// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.Build.Construction;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Construction;

namespace Microsoft.CodeAnalysis.MSBuild
{

    //internal 
    public sealed class NonReentrantLock
    {
        /// <summary>
        /// A synchronization object to protect access to the <see cref="_owningThreadId"/> field and to be pulsed
        /// when <see cref="Release"/> is called and during cancellation.
        /// </summary>
        private readonly object _syncLock;

        /// <summary>
        /// The <see cref="Environment.CurrentManagedThreadId" /> of the thread that holds the lock. Zero if no thread is holding
        /// the lock.
        /// </summary>
        private volatile int _owningThreadId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="useThisInstanceForSynchronization">If false (the default), then the class
        /// allocates an internal object to be used as a sync lock.
        /// If true, then the sync lock object will be the NonReentrantLock instance itself. This
        /// saves an allocation but a client may not safely further use this instance in a call to
        /// Monitor.Enter/Exit or in a "lock" statement.
        /// </param>
        public NonReentrantLock(bool useThisInstanceForSynchronization = false)
        {
            _syncLock = useThisInstanceForSynchronization ? this : new object();
        }

        /// <summary>
        /// Shared factory for use in lazy initialization.
        /// </summary>
        public static readonly Func<NonReentrantLock> Factory = () => new NonReentrantLock(useThisInstanceForSynchronization: true);

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="NonReentrantLock"/>, while observing a
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <remarks>
        /// Recursive locking is not supported. i.e. A thread may not call Wait successfully twice without an
        /// intervening <see cref="Release"/>.
        /// </remarks>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> token to
        /// observe.</param>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was
        /// canceled.</exception>
        /// <exception cref="LockRecursionException">The caller already holds the lock</exception>
        public void Wait(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.IsOwnedByMe)
            {
                throw new LockRecursionException();
            }

            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Fast path to try and avoid allocations in callback registration.
                lock (_syncLock)
                {
                    if (!this.IsLocked)
                    {
                        this.TakeOwnership();
                        return;
                    }
                }

                cancellationTokenRegistration = cancellationToken.Register(s_cancellationTokenCanceledEventHandler, _syncLock, useSynchronizationContext: false);
            }

            using (cancellationTokenRegistration)
            {
                // PERF: First spin wait for the lock to become available, but only up to the first planned yield.
                // This additional amount of spinwaiting was inherited from SemaphoreSlim's implementation where
                // it showed measurable perf gains in test scenarios.
                SpinWait spin = new SpinWait();
                while (this.IsLocked && !spin.NextSpinWillYield)
                {
                    spin.SpinOnce();
                }

                lock (_syncLock)
                {
                    while (this.IsLocked)
                    {
                        // If cancelled, we throw. Trying to wait could lead to deadlock.
                        cancellationToken.ThrowIfCancellationRequested();

                        //using (Logger.LogBlock(FunctionId.Misc_NonReentrantLock_BlockingWait, cancellationToken))
                        //{
                        // Another thread holds the lock. Wait until we get awoken either
                        // by some code calling "Release" or by cancellation.

                        Monitor.Wait(_syncLock);

                        //}
                    }

                    // We now hold the lock
                    this.TakeOwnership();
                }
            }
        }

        /// <summary>
        /// Exit the mutual exclusion.
        /// </summary>
        /// <remarks>
        /// The calling thread must currently hold the lock.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The lock is not currently held by the calling thread.</exception>
        public void Release()
        {
            AssertHasLock();

            lock (_syncLock)
            {
                this.ReleaseOwnership();

                // Release one waiter
                Monitor.Pulse(_syncLock);
            }
        }

        /// <summary>
        /// Determine if the lock is currently held by the calling thread.
        /// </summary>
        /// <returns>True if the lock is currently held by the calling thread.</returns>
        public bool LockHeldByMe()
        {
            return this.IsOwnedByMe;
        }

        /// <summary>
        /// Throw an exception if the lock is not held by the calling thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">The lock is not currently held by the calling thread.</exception>
        public void AssertHasLock()
        {

            //Contract.ThrowIfFalse(LockHeldByMe());
        }

        /// <summary>
        /// Checks if the lock is currently held.
        /// </summary>
        private bool IsLocked
        {
            get
            {
                return _owningThreadId != 0;
            }
        }

        /// <summary>
        /// Checks if the lock is currently held by the calling thread.
        /// </summary>
        private bool IsOwnedByMe
        {
            get
            {
                return _owningThreadId == Environment.CurrentManagedThreadId;
            }
        }

        /// <summary>
        /// Take ownership of the lock (by the calling thread). The lock may not already
        /// be held by any other code.
        /// </summary>
        private void TakeOwnership()
        {
            Contract.Assert(!this.IsLocked);
            _owningThreadId = Environment.CurrentManagedThreadId;
        }

        /// <summary>
        /// Release ownership of the lock. The lock must already be held by the calling thread.
        /// </summary>
        private void ReleaseOwnership()
        {
            Contract.Assert(this.IsOwnedByMe);
            _owningThreadId = 0;
        }

        /// <summary>
        /// Action object passed to a cancellation token registration.
        /// </summary>
        private static readonly Action<object> s_cancellationTokenCanceledEventHandler = CancellationTokenCanceledEventHandler;

        /// <summary>
        /// Callback executed when a cancellation token is canceled during a Wait.
        /// </summary>
        /// <param name="obj">The syncLock that protects a <see cref="NonReentrantLock"/> instance.</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            lock (obj)
            {
                // Release all waiters to check their cancellation tokens.
                Monitor.PulseAll(obj);
            }
        }

        public SemaphoreDisposer DisposableWait(CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Wait(cancellationToken);
            return new SemaphoreDisposer(this);
        }

        /// <summary>
        /// Since we want to avoid boxing the return from <see cref="NonReentrantLock.DisposableWait"/>, this type must be public.
        /// </summary>
        public struct SemaphoreDisposer : IDisposable
        {
            private readonly NonReentrantLock _semaphore;

            public SemaphoreDisposer(NonReentrantLock semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }


    public sealed class MSBuildWorkspace : Workspace
    {
        private readonly NonReentrantLock _serializationLock = new NonReentrantLock(false);
        private readonly NonReentrantLock _dataGuard = new NonReentrantLock(false);
        private readonly Dictionary<string, string> _extensionToLanguageMap =
            new Dictionary<string, string>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProjectId> _projectPathToProjectIdMap =
            new Dictionary<string, ProjectId>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IProjectFileLoader> _projectPathToLoaderMap =
            new Dictionary<string, IProjectFileLoader>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        private string _solutionFilePath;
        private ImmutableDictionary<string, string> _properties;
        private const string SolutionDirProperty = "SolutionDir";
        private static readonly char[] s_directorySplitChars;
        private IProjectFile _applyChangesProjectFile;

        private int _state = 0;

        public ImmutableDictionary<string, string> Properties
        {
            get
            {
                return this._properties;
            }
        }

        public bool LoadMetadataForReferencedProjects { get; set; }

        public bool SkipUnrecognizedProjects { get; set; }

        static MSBuildWorkspace()
        {
            char[] chArray = new char[2];
            int index1 = 0;
            int num1 = (int)System.IO.Path.DirectorySeparatorChar;
            chArray[index1] = (char)num1;
            int index2 = 1;
            int num2 = (int)System.IO.Path.AltDirectorySeparatorChar;
            chArray[index2] = (char)num2;
            MSBuildWorkspace.s_directorySplitChars = chArray;
        }

        private MSBuildWorkspace(HostServices hostServices, ImmutableDictionary<string, string> properties)
            : base(hostServices, "MSBuildWorkspace")
        {
            this._properties = properties ?? ImmutableDictionary<string, string>.Empty;
            this.SetSolutionProperties((string)null);
            this.LoadMetadataForReferencedProjects = false;
            this.SkipUnrecognizedProjects = true;
        }

        public static MSBuildWorkspace Create()
        {
            return MSBuildWorkspace.Create((IDictionary<string, string>)ImmutableDictionary<string, string>.Empty);
        }

        public static MSBuildWorkspace Create(IDictionary<string, string> properties)
        {
            return MSBuildWorkspace.Create(properties, (HostServices)DesktopMefHostServices.DefaultServices);
        }

        public static MSBuildWorkspace Create(IDictionary<string, string> properties, HostServices hostServices)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");
            if (hostServices == null)
                throw new ArgumentNullException("hostServices");
            else
                return new MSBuildWorkspace(hostServices, 
                    ImmutableDictionary.ToImmutableDictionary<string, string>((IEnumerable<KeyValuePair<string, string>>)properties));
        }

        public void AssociateFileExtensionWithLanguage(string projectFileExtension, string language)
        {
            if (language == null)
                throw new ArgumentNullException("language");
            if (projectFileExtension == null)
                throw new ArgumentNullException("projectFileExtension");
            using (this._dataGuard.DisposableWait(new CancellationToken()))
                this._extensionToLanguageMap[projectFileExtension] = language;
        }

        public void CloseSolution()
        {
            using (this._serializationLock.DisposableWait(new CancellationToken()))
                this.ClearSolution();
        }

        protected override void ClearSolutionData()
        {
            base.ClearSolutionData();
            using (this._dataGuard.DisposableWait(new CancellationToken()))
            {
                this.SetSolutionProperties((string)null);
                this._projectPathToProjectIdMap.Clear();
                this._projectPathToLoaderMap.Clear();
            }
        }

        private void SetSolutionProperties(string solutionFilePath)
        {
            this._solutionFilePath = solutionFilePath;
            if (string.IsNullOrEmpty(solutionFilePath) || string.IsNullOrEmpty(solutionFilePath))
                return;
            string path = System.IO.Path.GetDirectoryName(solutionFilePath);
            if (!path.EndsWith("\\", StringComparison.Ordinal))
                path = path + "\\";
            if (!System.IO.Directory.Exists(path))
                return;
            this._properties = this._properties.SetItem("SolutionDir", path);
        }

        private ProjectId GetProjectId(string fullProjectPath)
        {
            using (this._dataGuard.DisposableWait(new CancellationToken()))
            {
                ProjectId projectId;
                this._projectPathToProjectIdMap.TryGetValue(fullProjectPath, out projectId);
                return projectId;
            }
        }

        private ProjectId GetOrCreateProjectId(string fullProjectPath)
        {
            using (this._dataGuard.DisposableWait(new CancellationToken()))
            {
                ProjectId newId;
                if (!this._projectPathToProjectIdMap.TryGetValue(fullProjectPath, out newId))
                {
                    newId = ProjectId.CreateNewId(fullProjectPath);
                    this._projectPathToProjectIdMap.Add(fullProjectPath, newId);
                }
                return newId;
            }
        }

        private bool TryGetLoaderFromProjectPath(string projectFilePath, MSBuildWorkspace.ReportMode mode, out IProjectFileLoader loader)
        {
            using (this._dataGuard.DisposableWait(new CancellationToken()))
            {
                if (!this._projectPathToLoaderMap.TryGetValue(projectFilePath, out loader))
                {
                    string str1 = System.IO.Path.GetExtension(projectFilePath);
                    if (str1.Length > 0 && (int)str1[0] == 46)
                        str1 = str1.Substring(1);
                    string str2;
                    if (this._extensionToLanguageMap.TryGetValue(str1, out str2))
                    {
                        throw new NotImplementedException();
                        //if (Roslyn.Utilities.EnumerableExtensions.Contains(this.Services.SupportedLanguages, str2))
                        //{
                        //    loader = this.Services.GetLanguageServices(str2).GetService<IProjectFileLoader>();
                        //}
                        //else
                        //{
                        //    this.ReportFailure(mode, string.Format("CannotOpenProjectUnsupportedLanguage", (object)projectFilePath, (object)str2), (Func<string, Exception>)null);
                        //    return false;
                        //}
                    }
                    else
                    {
                        loader = ProjectFileLoader.GetLoaderForProjectFileExtension((Workspace)this, str1);
                        if (loader == null)
                        {
                            this.ReportFailure(mode, string.Format("CannotOpenProjectUnrecognizedFileExtension", (object)projectFilePath, (object)System.IO.Path.GetExtension(projectFilePath)), (Func<string, Exception>)null);
                            return false;
                        }
                    }
                    if (loader != null)
                        this._projectPathToLoaderMap[projectFilePath] = loader;
                }
                return loader != null;
            }
        }

        private bool TryGetAbsoluteProjectPath(string path, string baseDirectory, MSBuildWorkspace.ReportMode mode, out string absolutePath)
        {
            try
            {
                absolutePath = this.GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception ex)
            {
                this.ReportFailure(mode, string.Format("InvalidProjectFilePath", (object)path), (Func<string, Exception>)null);
                absolutePath = (string)null;
                return false;
            }
            if (System.IO.File.Exists(absolutePath))
                return true;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            this.ReportFailure(mode, string.Format("ProjectFileNotFound", (object)absolutePath));
            //MSBuildWorkspace.\u003C\u003Ec.\u003C\u003E9__29_0 ??
            //    (MSBuildWorkspace.\u003C\u003Ec.\u003C\u003E9__29_0 = new Func<string, Exception>(
            //        MSBuildWorkspace.\u003C\u003Ec.\u003C\u003E9.\u003CTryGetAbsoluteProjectPath\u003Eb__29_0)));
            return false;
        }

        private string GetAbsoluteSolutionPath(string path, string baseDirectory)
        {
            string absolutePath;
            try
            {
                absolutePath = this.GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("InvalidSolutionFilePath", (object)path));
            }
            if (!System.IO.File.Exists(absolutePath))
                throw new FileNotFoundException(string.Format("SolutionFileNotFound", (object)absolutePath));
            else
                return absolutePath;
        }

        private void ReportFailure(MSBuildWorkspace.ReportMode mode, string message, Func<string, Exception> createException = null)
        {
            switch (mode)
            {
                case MSBuildWorkspace.ReportMode.Throw:
                    if (createException != null)
                        throw createException(message);
                    else
                        throw new InvalidOperationException(message);
                case MSBuildWorkspace.ReportMode.Log:
                    this.OnWorkspaceFailed(new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, message));
                    break;
            }
        }

        private string GetAbsolutePath(string path, string baseDirectoryPath)
        {
            return System.IO.Path.GetFullPath(
                FileUtilities.ResolveRelativePath(path, baseDirectoryPath) ?? path);
        }

        public async Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken)
        {
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field
            int num = _state;
            List<ProjectInSolution> invalidProjects;
            MSBuildWorkspace.ReportMode reportMode;
            Dictionary<ProjectId, ProjectInfo> loadedProjects;
            string absoluteSolutionPath;
            VersionStamp version;
            IEnumerator<ProjectInSolution> enumerator;

            if (num != 0)
            {
                if (solutionFilePath == null)
                    throw new ArgumentNullException("solutionFilePath");
                this.ClearSolution();
                absoluteSolutionPath = this.GetAbsoluteSolutionPath(solutionFilePath, System.IO.Directory.GetCurrentDirectory());

                throw new NotImplementedException();
                NonReentrantLock.SemaphoreDisposer semaphoreDisposer1 = this._dataGuard.DisposableWait(cancellationToken);
                try
                {
                    this.SetSolutionProperties(absoluteSolutionPath);
                }
                finally
                {
                    if (num < 0)
                        semaphoreDisposer1.Dispose();
                }
                version = new VersionStamp();

                // throw new NotImplementedException();
                // var reader = System.IO.File.OpenText(absoluteSolutionPath);

                // SolutionFile solutionFile = SolutionFile.Parse(reader);
                Microsoft.Build.Construction.SolutionFile solutionFile = SolutionFile.Parse(absoluteSolutionPath);

                reportMode = this.SkipUnrecognizedProjects ? MSBuildWorkspace.ReportMode.Log : MSBuildWorkspace.ReportMode.Throw;
                invalidProjects = new List<ProjectInSolution>();
                NonReentrantLock.SemaphoreDisposer semaphoreDisposer2 = this._dataGuard.DisposableWait(cancellationToken);
                try
                {
                    // MsBuildWorkspace SolutionFile ProjectsInOrder
                    IEnumerator<ProjectInSolution> enumerator1 = solutionFile.ProjectsInOrder.GetEnumerator();

                    // IEnumerator<ProjectBlock> enumeratorBlock =  // .ProjectsInOrder().GetEnumerator();

                    try
                    {
                        while (enumerator1.MoveNext())
                        {
                            var block = enumerator1.Current;

                            ProjectInSolution current = new ProjectInSolution(solutionFile);

                            if (current.ProjectType != SolutionProjectType.SolutionFolder)
                            {
                                string absolutePath = this.TryGetAbsolutePath(current.AbsolutePath, reportMode);
                                if (absolutePath != null)
                                {
                                    string extension = System.IO.Path.GetExtension(absolutePath);
                                    if (extension.Length > 0 && (int)extension[0] == 46)
                                        extension = extension.Substring(1);
                                    
                                    IProjectFileLoader projectFileExtension = 
                                        ProjectFileLoader.GetLoaderForProjectFileExtension((Workspace)this, extension);
                                    if (projectFileExtension != null)
                                        this._projectPathToLoaderMap[absolutePath] = projectFileExtension;
                                }
                                else
                                    invalidProjects.Add(current);
                            }
                        }
                    }
                    finally
                    {
                        if (num < 0 && enumerator1 != null)
                            enumerator1.Dispose();
                    }
                }
                finally
                {
                    if (num < 0)
                        semaphoreDisposer2.Dispose();
                }
                loadedProjects = new Dictionary<ProjectId, ProjectInfo>();
                enumerator = solutionFile.ProjectsInOrder.GetEnumerator();
            }
            try
            {
                if (num == 0)
                {
                    //ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter
                    //    configuredTaskAwaiter = new ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter();
                    // ISSUE: explicit reference operation
                    // ISSUE: reference to a compiler-generated field
                    _state = num = -1;
                }
                else
                    goto label_32;
            label_31:

                string absolutePath;
                IProjectFileLoader loader;

                throw new NotImplementedException();
                
                //absolutePath = solutionFilePath;
                //loader = null;
                //loadedProjects = null;
                //ProjectId projectId = await this.GetOrLoadProjectAsync(absolutePath, loader, false,
                //    loadedProjects, cancellationToken).ConfigureAwait(false);

            label_32:

                throw new NotImplementedException();
                while (enumerator.MoveNext())
                {
                    ProjectInSolution current = enumerator.Current;

                    throw new NotImplementedException();
                    cancellationToken.ThrowIfCancellationRequested();
                    if (current.ProjectType != SolutionProjectType.SolutionFolder && !invalidProjects.Contains(current))
                    {
                        absolutePath = this.TryGetAbsolutePath(current.AbsolutePath, reportMode);
                        if (absolutePath != null && this.TryGetLoaderFromProjectPath(absolutePath, reportMode, out loader))
                            goto label_31;
                    }
                }
            }
            finally
            {
                throw new NotImplementedException();
                if (num < 0 && enumerator != null)
                    enumerator.Dispose();
            }
            enumerator = (IEnumerator<ProjectInSolution>)null;

            throw new NotImplementedException();
            //this.OnSolutionAdded(SolutionInfo.Create(SolutionId.CreateNewId(
            //    absoluteSolutionPath), version, absoluteSolutionPath,
            //    (IEnumerable<ProjectInfo>)loadedProjects.Values));

            this.UpdateReferencesAfterAdd();
            return this.CurrentSolution;
        }

        public async Task<Project> OpenProjectAsync(string projectFilePath, CancellationToken cancellationToken)
        {
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field

            int num = _state;
            Dictionary<ProjectId, ProjectInfo> loadedProjects;
            string absolutePath;
            IProjectFileLoader loader;
            Project project;

            if (num != 0)
            {
                if (projectFilePath == null)
                    throw new ArgumentNullException("projectFilePath");
                if (this.TryGetAbsoluteProjectPath(projectFilePath, System.IO.Directory.GetCurrentDirectory(),
                    MSBuildWorkspace.ReportMode.Throw, out absolutePath) && this.TryGetLoaderFromProjectPath(projectFilePath,
                    MSBuildWorkspace.ReportMode.Throw, out loader))
                {
                    loadedProjects = new Dictionary<ProjectId, ProjectInfo>();
                }
                else
                {
                    project = (Project)null;
                    goto label_15;
                }
            }
            else
            {
                //ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter configuredTaskAwaiter
                //    = new ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter();
                // ISSUE: explicit reference operation
                // ISSUE: reference to a compiler-generated field

                absolutePath = null;
                loader = null;
                _state = num = -1;
                throw new NotImplementedException();
            }

            ProjectId projectId = await this.GetOrLoadProjectAsync(
                absolutePath, loader, this.LoadMetadataForReferencedProjects, loadedProjects, cancellationToken).ConfigureAwait(false);

            Dictionary<ProjectId, ProjectInfo>.ValueCollection.Enumerator enumerator = loadedProjects.Values.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                    this.OnProjectAdded(enumerator.Current);
            }
            finally
            {
                if (num < 0)
                    enumerator.Dispose();
            }

            this.UpdateReferencesAfterAdd();
            project = this.CurrentSolution.GetProject(projectId);

        label_15:
            return project;
        }

        private string TryGetAbsolutePath(string path, MSBuildWorkspace.ReportMode mode)
        {
            try
            {
                path = System.IO.Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                this.ReportFailure(mode, string.Format("InvalidProjectFilePath", (object)path), (Func<string, Exception>)null);
                return (string)null;
            }
            if (System.IO.File.Exists(path))
                return path;
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated field
            // ISSUE: reference to a compiler-generated method
            throw new InvalidOperationException(string.Format("ProjectFileNotFound", (object)path));
            //this.ReportFailure(mode, string.Format("ProjectFileNotFound", (object) path), 
            //    MSBuildWorkspace.\u003C\u003Ec.\u003C\u003E9__36_0 ?? (MSBuildWorkspace.\u003C\u003Ec.\u003C\u003E9__36_0 = new Func<string, Exception>(MSBuildWorkspace.\u003C\u003Ec.\u003C\u003E9.\u003CTryGetAbsolutePath\u003Eb__36_0)));
            return (string)null;
        }

        private void UpdateReferencesAfterAdd()
        {
            using (this._serializationLock.DisposableWait(new CancellationToken()))
            {
                Solution currentSolution = this.CurrentSolution;
                Solution solution = this.UpdateReferencesAfterAdd(currentSolution);
                if (solution == currentSolution)
                    return;
                Solution newSolution = this.SetCurrentSolution(solution);
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, currentSolution,
                    newSolution, (ProjectId)null, (DocumentId)null);
            }
        }

        private Solution UpdateReferencesAfterAdd(Solution solution)
        {
            Dictionary<string, ProjectId> dictionary = new Dictionary<string, ProjectId>();
            foreach (Project project in solution.Projects)
            {
                if (!string.IsNullOrEmpty(project.OutputFilePath))
                    dictionary[project.OutputFilePath] = project.Id;
            }
            foreach (ProjectId projectId1 in (IEnumerable<ProjectId>)solution.ProjectIds)
            {
                Project project = solution.GetProject(projectId1);
                foreach (MetadataReference metadataReference in (IEnumerable<MetadataReference>)project.MetadataReferences)
                {
                    MetadataReference meta = metadataReference;
                    PortableExecutableReference executableReference = meta as PortableExecutableReference;
                    ProjectId projectId2;
                    if (executableReference != null && (!string.IsNullOrEmpty(executableReference.Display)
                        && dictionary.TryGetValue(executableReference.Display, out projectId2)
                        || !string.IsNullOrEmpty(executableReference.FilePath)
                        && dictionary.TryGetValue(executableReference.FilePath, out projectId2)))
                    {
                        ProjectReference projectReference = new ProjectReference(projectId2,
                            executableReference.Properties.Aliases, executableReference.Properties.EmbedInteropTypes);

                        throw new NotImplementedException();
                        //if (!Enumerable.Contains<ProjectReference>(project.ProjectReferences, projectReference))
                        //    project = project.WithProjectReferences(Roslyn.Utilities.EnumerableExtensions
                        //        .Concat<ProjectReference>(project.ProjectReferences, projectReference));

                        project = project.WithMetadataReferences(Enumerable.Where<MetadataReference>(
                            (IEnumerable<MetadataReference>)project.MetadataReferences, (Func<MetadataReference, bool>)(mr => mr != meta)));
                    }
                }
                solution = project.Solution;
            }
            return solution;
        }

        private async Task<ProjectId> GetOrLoadProjectAsync(string projectFilePath, IProjectFileLoader loader,
            bool preferMetadata, Dictionary<ProjectId, ProjectInfo> loadedProjects, CancellationToken cancellationToken)
        {
            ProjectId projectId;
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field
            if (_state != 0)
            {
                projectId = this.GetProjectId(projectFilePath);
                if (!(projectId == (ProjectId)null))
                    goto label_4;
            }
            else
            {
                //ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter 
                //    configuredTaskAwaiter = new ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter();
                int num = 0;
                // ISSUE: explicit reference operation
                // ISSUE: reference to a compiler-generated field
                _state = num = -1;
            }

            projectId = await this.LoadProjectAsync(projectFilePath, loader, preferMetadata,
                loadedProjects, cancellationToken).ConfigureAwait(false);
        label_4:
            return projectId;
        }

        private async Task<ProjectId> LoadProjectAsync(string projectFilePath, IProjectFileLoader loader, bool preferMetadata, Dictionary<ProjectId, ProjectInfo> loadedProjects, CancellationToken cancellationToken)
        {
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field

            //  (^this).
            int num1 = _state;
            ProjectId projectId;
            string projectName;
            switch (num1)
            {
                case 0:
                    ConfiguredTaskAwaitable<IProjectFile>.ConfiguredTaskAwaiter configuredTaskAwaiter1 = new ConfiguredTaskAwaitable<IProjectFile>.ConfiguredTaskAwaiter();
                    // ISSUE: explicit reference operation
                    // ISSUE: reference to a compiler-generated field
                    _state = num1 = -1;
                    break;
                case 1:

                    throw new NotImplementedException();
                    //ConfiguredTaskAwaitable<ProjectFileInfo>.ConfiguredTaskAwaiter configuredTaskAwaiter2 = new ConfiguredTaskAwaitable<ProjectFileInfo>.ConfiguredTaskAwaiter();
                    // ISSUE: explicit reference operation
                    // ISSUE: reference to a compiler-generated field
                    _state = num1 = -1;
                    goto label_5;
                case 2:
                    ConfiguredTaskAwaitable<MSBuildWorkspace.ResolvedReferences>.ConfiguredTaskAwaiter configuredTaskAwaiter3 = new ConfiguredTaskAwaitable<MSBuildWorkspace.ResolvedReferences>.ConfiguredTaskAwaiter();
                    int num2;
                    // ISSUE: explicit reference operation
                    // ISSUE: reference to a compiler-generated field
                    _state = num2 = -1;
                    goto label_16;
                default:
                    projectId = this.GetOrCreateProjectId(projectFilePath);
                    projectName = System.IO.Path.GetFileNameWithoutExtension(projectFilePath);
                    break;
            }
            IProjectFile projectFile = await loader.LoadProjectFileAsync(projectFilePath, (IDictionary<string, string>)this._properties, cancellationToken).ConfigureAwait(false);
        label_5:

            throw new NotImplementedException();
        //ProjectFileInfo projectFileInfo = await projectFile.GetProjectFileInfoAsync(cancellationToken).ConfigureAwait(false);
        //VersionStamp version = string.IsNullOrEmpty(projectFilePath) || !System.IO.File.Exists(projectFilePath) ? VersionStamp.Create() : VersionStamp.Create(System.IO.File.GetLastWriteTimeUtc(projectFilePath));
        //ImmutableArray<DocumentFileInfo> immutableArray = Roslyn.Utilities.ImmutableArrayExtensions.ToImmutableArrayOrEmpty<DocumentFileInfo>((IEnumerable<DocumentFileInfo>)projectFileInfo.Documents);
        //this.CheckDocuments((IEnumerable<DocumentFileInfo>)immutableArray, projectFilePath, projectId);
        //System.Text.Encoding defaultEncoding = MSBuildWorkspace.GetDefaultEncoding(projectFileInfo.CodePage);
        //List<DocumentInfo> docs = new List<DocumentInfo>();
        //foreach (DocumentFileInfo documentFileInfo in immutableArray)
        //{
        //    string name;
        //    ImmutableArray<string> folders;
        //    MSBuildWorkspace.GetDocumentNameAndFolders(documentFileInfo.LogicalPath, out name, out folders);
        //    docs.Add(DocumentInfo.Create(DocumentId.CreateNewId(projectId, documentFileInfo.FilePath), name, (IEnumerable<string>)folders, projectFile.GetSourceCodeKind(documentFileInfo.FilePath), (TextLoader)new FileTextLoader(documentFileInfo.FilePath, defaultEncoding), documentFileInfo.FilePath, documentFileInfo.IsGenerated));
        //}
        //List<DocumentInfo> additonalDocs = new List<DocumentInfo>();
        //IEnumerator<DocumentFileInfo> enumerator = projectFileInfo.AdditionalDocuments.GetEnumerator();
        //try
        //{
        //    while (enumerator.MoveNext())
        //    {
        //        DocumentFileInfo current = enumerator.Current;
        //        string name;
        //        ImmutableArray<string> folders;
        //        MSBuildWorkspace.GetDocumentNameAndFolders(current.LogicalPath, out name, out folders);
        //        additonalDocs.Add(DocumentInfo.Create(DocumentId.CreateNewId(projectId, current.FilePath), name, (IEnumerable<string>)folders, SourceCodeKind.Regular, (TextLoader)new FileTextLoader(current.FilePath, defaultEncoding), current.FilePath, current.IsGenerated));
        //    }
        //}
        //finally
        //{
        //    if (num1 < 0 && enumerator != null)
        //        enumerator.Dispose();
        //}

        label_16:
            throw new NotImplementedException();

            //MSBuildWorkspace.ResolvedReferences resolvedReferences = await this.ResolveProjectReferencesAsync(projectId, projectFilePath, projectFileInfo.ProjectReferences, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);
            //IEnumerable<MetadataReference> metadataReferences = Enumerable.Concat<MetadataReference>((IEnumerable<MetadataReference>)projectFileInfo.MetadataReferences, (IEnumerable<MetadataReference>)resolvedReferences.MetadataReferences);

            //string outputFilePath = projectFileInfo.OutputFilePath;
            //string assemblyName = projectFileInfo.AssemblyName;
            //if (string.IsNullOrWhiteSpace(assemblyName))
            //{
            //    assemblyName = System.IO.Path.GetFileNameWithoutExtension(projectFilePath);
            //    if (string.IsNullOrWhiteSpace(assemblyName))
            //        assemblyName = "assembly";
            //}

            //loadedProjects.Add(projectId, ProjectInfo.Create(projectId, version, projectName, assemblyName, loader.Language, projectFilePath, outputFilePath, projectFileInfo.CompilationOptions,
            //projectFileInfo.ParseOptions, (IEnumerable<DocumentInfo>)docs, (IEnumerable<ProjectReference>)resolvedReferences.ProjectReferences, metadataReferences, (IEnumerable<AnalyzerReference>)
            //    projectFileInfo.AnalyzerReferences, (IEnumerable<DocumentInfo>)additonalDocs, false, (Type)null));
            return projectId;
        }

        private static System.Text.Encoding GetDefaultEncoding(int codePage)
        {
            if (codePage == 0)
                return (System.Text.Encoding)null;
            try
            {
                return System.Text.Encoding.GetEncoding(codePage);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return (System.Text.Encoding)null;
            }
        }

        private static void GetDocumentNameAndFolders(string logicalPath, out string name, out ImmutableArray<string> folders)
        {
            string[] strArray1 = logicalPath.Split(MSBuildWorkspace.s_directorySplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (strArray1.Length != 0)
            {
                folders = strArray1.Length <= 1 ? ImmutableArray.Create<string>() :
                        ImmutableArray.ToImmutableArray<string>(Enumerable.Take<string>((IEnumerable<string>)strArray1, strArray1.Length - 1));

                // ISSUE: explicit reference operation
                // ISSUE: variable of a reference type

                throw new NotImplementedException();
                //string& local = @name;
                //string[] strArray2 = strArray1;
                //int index = strArray2.Length - 1;
                //string str = strArray2[index];
                //// ISSUE: explicit reference operation
                //// ^local = str;
                //name = str;
            }
            else
            {
                name = logicalPath;
                folders = ImmutableArray.Create<string>();
            }
        }

        private void CheckDocuments(IEnumerable<DocumentFileInfo> docs, string projectFilePath, ProjectId projectId)
        {
            HashSet<string> hashSet = new HashSet<string>();
            foreach (DocumentFileInfo documentFileInfo in docs)
            {
                if (hashSet.Contains(documentFileInfo.FilePath))
                    this.OnWorkspaceFailed((WorkspaceDiagnostic)
                        new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning,
                            string.Format("DuplicateSourceFileInProject", (object)documentFileInfo.FilePath, (object)projectFilePath), projectId));
                hashSet.Add(documentFileInfo.FilePath);
            }
        }

        private async Task<MSBuildWorkspace.ResolvedReferences> ResolveProjectReferencesAsync(ProjectId thisProjectId, string thisProjectPath, IReadOnlyList<ProjectFileReference> projectFileReferences, bool preferMetadata, Dictionary<ProjectId, ProjectInfo> loadedProjects, CancellationToken cancellationToken)
        {
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field
            int num = _state;
            MSBuildWorkspace.ReportMode reportMode;
            MSBuildWorkspace.ResolvedReferences resolvedReferences;
            IEnumerator<ProjectFileReference> enumerator = null; // System.Linq.Enumerable.Empty<ProjectFileReference>();

            switch (num)
            {
                case 0:
                case 1:
                case 2:
                    try
                    {
                        string fullPath;
                        ProjectFileReference projectFileReference;
                        ConfiguredTaskAwaitable<MetadataReference>.ConfiguredTaskAwaiter configuredTaskAwaiter1;
                        IProjectFileLoader loader;
                        ConfiguredTaskAwaitable<MetadataReference> configuredTaskAwaitable;
                        switch (num)
                        {
                            case 0:
                                configuredTaskAwaiter1 = new ConfiguredTaskAwaitable<MetadataReference>.ConfiguredTaskAwaiter();
                                // ISSUE: explicit reference operation
                                // ISSUE: reference to a compiler-generated field
                                _state = num = -1;
                                break;
                            case 1:
                                ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter configuredTaskAwaiter2 = new ConfiguredTaskAwaitable<ProjectId>.ConfiguredTaskAwaiter();
                                // ISSUE: explicit reference operation
                                // ISSUE: reference to a compiler-generated field
                                _state = num = -1;
                                goto label_14;
                            case 2:
                                configuredTaskAwaiter1 = new ConfiguredTaskAwaitable<MetadataReference>.ConfiguredTaskAwaiter();
                                // ISSUE: explicit reference operation
                                // ISSUE: reference to a compiler-generated field
                                _state = num = -1;
                                goto label_17;
                            default:

                                throw new NotImplementedException();
                                //while (enumerator.MoveNext())
                                //{
                                //    projectFileReference = enumerator.Current;
                                //    if (this.TryGetAbsoluteProjectPath(projectFileReference.Path, System.IO.Path.GetDirectoryName(thisProjectPath), reportMode, out fullPath))
                                //    {
                                //        ProjectId projectId1 = this.GetProjectId(fullPath);
                                //        if (projectId1 != (ProjectId)null)
                                //        {
                                //            resolvedReferences.ProjectReferences.Add(new ProjectReference(projectId1, projectFileReference.Aliases, false));
                                //        }
                                //        else
                                //        {
                                //            this.TryGetLoaderFromProjectPath(fullPath, MSBuildWorkspace.ReportMode.Ignore, out loader);
                                //            if (preferMetadata || loader == null)
                                //            {
                                //                configuredTaskAwaitable = this.GetProjectMetadata(fullPath, projectFileReference.Aliases, (IDictionary<string, string>)this._properties, cancellationToken).ConfigureAwait(false);
                                //                goto label_10;
                                //            }
                                //            else
                                //                goto label_12;
                                //        }
                                //    }
                                //    else
                                //    {
                                //        fullPath = projectFileReference.Path;
                                //        goto label_21;
                                //    }
                                //}
                                goto label_26;
                        }
                    //label_10:
                    //    MetadataReference metadataReference1 = await configuredTaskAwaitable;
                    //    if (metadataReference1 != null)
                    //    {
                    //        resolvedReferences.MetadataReferences.Add(metadataReference1);
                    //        goto default;
                    //    }
                    //label_12:
                    //    if (!this.TryGetLoaderFromProjectPath(fullPath, reportMode, out loader))
                    //        goto label_21;
                    label_14:

                        throw new NotImplementedException();
                    //ProjectId projectId = await this.GetOrLoadProjectAsync(fullPath, loader, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);
                    //if (this.ProjectAlreadyReferencesProject(loadedProjects, projectId, thisProjectId))
                    //{
                    //    configuredTaskAwaitable = this.GetProjectMetadata(fullPath, projectFileReference.Aliases, (IDictionary<string, string>)this._properties, cancellationToken).ConfigureAwait(false);
                    //}
                    //else
                    //{
                    //    resolvedReferences.ProjectReferences.Add(new ProjectReference(projectId, projectFileReference.Aliases, false));
                    //    goto default;
                    //}
                    label_17:

                        throw new NotImplementedException();
                    //MetadataReference metadataReference2 = await configuredTaskAwaitable;
                    //if (metadataReference2 != null)
                    //{
                    //    resolvedReferences.MetadataReferences.Add(metadataReference2);
                    //    goto default;
                    //}
                    //else
                    //    goto default;
                    label_21:
                        resolvedReferences.ProjectReferences.Add(new ProjectReference(this.GetOrCreateProjectId(fullPath), projectFileReference.Aliases, false));
                        fullPath = (string)null;
                        projectFileReference = (ProjectFileReference)null;
                        goto default;
                    }
                    finally
                    {
                        if (num < 0 && enumerator != null)
                            enumerator.Dispose();
                    }
                label_26:
                    enumerator = (IEnumerator<ProjectFileReference>)null;
                    return resolvedReferences;
                default:
                    resolvedReferences = new MSBuildWorkspace.ResolvedReferences();
                    reportMode = this.SkipUnrecognizedProjects ? MSBuildWorkspace.ReportMode.Log : MSBuildWorkspace.ReportMode.Throw;
                    enumerator = projectFileReferences.GetEnumerator();
                    goto case 0;
            }
        }

        private bool ProjectAlreadyReferencesProject(Dictionary<ProjectId, ProjectInfo> loadedProjects, ProjectId fromProject, ProjectId targetProject)
        {
            ProjectInfo projectInfo;
            if (loadedProjects.TryGetValue(fromProject, out projectInfo))
                return Enumerable.Any<ProjectReference>((IEnumerable<ProjectReference>)projectInfo.ProjectReferences, (Func<ProjectReference, bool>)(pr =>
                {
                    if (!(pr.ProjectId == targetProject))
                        return this.ProjectAlreadyReferencesProject(loadedProjects, pr.ProjectId, targetProject);
                    else
                        return true;
                }));
            else
                return false;
        }

        private async Task<MetadataReference> GetProjectMetadata(string projectFilePath, ImmutableArray<string> aliases,
            IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field
            int num1 = _state;
            string outputFilePath;
            if (num1 != 0)
                outputFilePath = (string)null;
            try
            {
                if (num1 == 0)
                {
                    ConfiguredTaskAwaitable<string>.ConfiguredTaskAwaiter configuredTaskAwaiter = new ConfiguredTaskAwaitable<string>.ConfiguredTaskAwaiter();
                    int num2;
                    // ISSUE: explicit reference operation
                    // ISSUE: reference to a compiler-generated field
                    _state = num2 = -1;
                }
                outputFilePath = await ProjectFileLoader.GetOutputFilePathAsync(projectFilePath, globalProperties, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.OnWorkspaceFailed(new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message));
            }
            throw new NotImplementedException();
            //return outputFilePath == null || !System.IO.File.Exists(outputFilePath) ? (MetadataReference)null : (!Workspace.TestHookStandaloneProjectsDoNotHoldReferences ? (MetadataReference)this.Services.GetService<IMetadataService>().GetReference(outputFilePath, new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, false)) : (MetadataReference)AssemblyMetadata.CreateFromImage((IEnumerable<byte>)System.IO.File.ReadAllBytes(outputFilePath)).GetReference(this.Services.GetService<
            //    IDocumentationProviderService>().GetDocumentationProvider(outputFilePath), aliases, false, (string)null, outputFilePath));
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.AddProjectReference:
                case ApplyChangesKind.RemoveProjectReference:
                case ApplyChangesKind.AddMetadataReference:
                case ApplyChangesKind.RemoveMetadataReference:
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.AddAnalyzerReference:
                case ApplyChangesKind.RemoveAnalyzerReference:
                    return true;
                default:
                    return false;
            }
        }

        private bool HasProjectFileChanges(ProjectChanges changes)
        {
            if (!Enumerable.Any<DocumentId>(changes.GetAddedDocuments())
                && !Enumerable.Any<DocumentId>(changes.GetRemovedDocuments())
                && (!Enumerable.Any<MetadataReference>(changes.GetAddedMetadataReferences())
                && !Enumerable.Any<MetadataReference>(changes.GetRemovedMetadataReferences()))
                && (!Enumerable.Any<ProjectReference>(changes.GetAddedProjectReferences())
                && !Enumerable.Any<ProjectReference>(changes.GetRemovedProjectReferences()) && !Enumerable.Any<AnalyzerReference>(changes.GetAddedAnalyzerReferences())))
                return Enumerable.Any<AnalyzerReference>(changes.GetRemovedAnalyzerReferences());
            else
                return true;
        }

        public override bool TryApplyChanges(Solution newSolution)
        {
            using (this._serializationLock.DisposableWait(new CancellationToken()))
                return base.TryApplyChanges(newSolution);
        }

        protected override void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            Project project = projectChanges.OldProject ?? projectChanges.NewProject;
            try
            {
                if (this.HasProjectFileChanges(projectChanges))
                {
                    string filePath = project.FilePath;
                    IProjectFileLoader loader;
                    if (this.TryGetLoaderFromProjectPath(filePath, MSBuildWorkspace.ReportMode.Ignore, out loader))
                    {
                        try
                        {
                            this._applyChangesProjectFile = loader.LoadProjectFileAsync(filePath, (IDictionary<string, string>)this._properties, CancellationToken.None).Result;
                        }
                        catch (IOException ex)
                        {
                            this.OnWorkspaceFailed((WorkspaceDiagnostic)new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, projectChanges.ProjectId));
                        }
                    }
                }
                base.ApplyProjectChanges(projectChanges);
                if (this._applyChangesProjectFile == null)
                    return;
                try
                {
                    this._applyChangesProjectFile.Save();
                }
                catch (IOException ex)
                {
                    this.OnWorkspaceFailed((WorkspaceDiagnostic)new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, projectChanges.ProjectId));
                }
            }
            finally
            {
                this._applyChangesProjectFile = (IProjectFile)null;
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText text)
        {
            Document document = this.CurrentSolution.GetDocument(documentId);
            if (document == null)
                return;
            System.Text.Encoding encoding = MSBuildWorkspace.DetermineEncoding(text, document);
            this.SaveDocumentText(documentId, document.FilePath, text, encoding ?? (System.Text.Encoding)new UTF8Encoding(false));
            this.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
        }

        private static System.Text.Encoding DetermineEncoding(SourceText text, Document document)
        {
            //if (text.Encoding != null)
            //    return (System.Text.Encoding) (object)text.Encoding;

            throw new NotImplementedException();
            //try
            //{
            //    using (ExceptionHelpers.SuppressFailFast())
            //    {
            //        using (System.IO.FileStream fileStream = new System.IO.FileStream(document.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            //            return EncodedStringText.Create((Stream)fileStream, (System.Text.Encoding)null, SourceHashAlgorithm.Sha1).Encoding;
            //    }
            //}
            //catch (IOException ex)
            //{
            //}
            //catch (InvalidDataException ex)
            //{
            //}

            return (System.Text.Encoding)null;
        }

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            Project project = this.CurrentSolution.GetProject(info.Id.ProjectId);
            IProjectFileLoader loader;
            if (!this.TryGetLoaderFromProjectPath(project.FilePath, MSBuildWorkspace.ReportMode.Ignore, out loader))
                return;
            string documentExtension = this._applyChangesProjectFile.GetDocumentExtension(info.SourceCodeKind);
            string str1 = System.IO.Path.ChangeExtension(info.Name, documentExtension);

            throw new NotImplementedException();

            //string str2 = info.Folders == null || info.Folders.Count <= 0 ? str1 : System.IO.Path.Combine(System.IO.Path.Combine(Enumerable.ToArray<string>((IEnumerable<string>)info.Folders)), str1);
            //string absolutePath = this.GetAbsolutePath(str2, System.IO.Path.GetDirectoryName(project.FilePath));
            //DocumentInfo documentInfo = info.WithName(str1).WithFilePath(absolutePath).WithTextLoader((TextLoader)new FileTextLoader(absolutePath, text.Encoding));
            //this._applyChangesProjectFile.AddDocument(str2, (string)null);
            //this.OnDocumentAdded(documentInfo);
            //if (text == null)
            //    return;
            //DocumentId id = info.Id;
            //string fullPath = absolutePath;
            //SourceText newText = text;
            //System.Text.Encoding encoding = newText.Encoding ?? System.Text.Encoding.UTF8;
            //this.SaveDocumentText(id, fullPath, newText, encoding);
        }

        private void SaveDocumentText(DocumentId id, string fullPath, SourceText newText, System.Text.Encoding encoding)
        {
            throw new NotImplementedException();
            //try
            //{
            //    using (ExceptionHelpers.SuppressFailFast())
            //    {
            //        string directoryName = System.IO.Path.GetDirectoryName(fullPath);
            //        if (!System.IO.Directory.Exists(directoryName))
            //            System.IO.Directory.CreateDirectory(directoryName);
            //        using (StreamWriter streamWriter = new StreamWriter(fullPath, false, encoding))
            //            newText.Write((TextWriter)streamWriter, new CancellationToken());
            //    }
            //}
            //catch (IOException ex)
            //{
            //    this.OnWorkspaceFailed((WorkspaceDiagnostic)new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, id));
            //}
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            Document document = this.CurrentSolution.GetDocument(documentId);
            if (document == null)
                return;
            this._applyChangesProjectFile.RemoveDocument(document.FilePath);
            this.DeleteDocumentFile(document.Id, document.FilePath);
            this.OnDocumentRemoved(documentId);
        }

        private void DeleteDocumentFile(DocumentId documentId, string fullPath)
        {
            try
            {
                if (!System.IO.File.Exists(fullPath))
                    return;
                System.IO.File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                this.OnWorkspaceFailed((WorkspaceDiagnostic)new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, documentId));
            }
            catch (NotSupportedException ex)
            {
                this.OnWorkspaceFailed((WorkspaceDiagnostic)new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, documentId));
            }
            catch (UnauthorizedAccessException ex)
            {
                this.OnWorkspaceFailed((WorkspaceDiagnostic)new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, documentId));
            }
        }

        protected override void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            AssemblyIdentity assemblyIdentity = this.GetAssemblyIdentity(projectId, metadataReference);
            this._applyChangesProjectFile.AddMetadataReference(metadataReference, assemblyIdentity);
            this.OnMetadataReferenceAdded(projectId, metadataReference);
        }

        protected override void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            AssemblyIdentity assemblyIdentity = this.GetAssemblyIdentity(projectId, metadataReference);
            this._applyChangesProjectFile.RemoveMetadataReference(metadataReference, assemblyIdentity);
            this.OnMetadataReferenceRemoved(projectId, metadataReference);
        }

        private AssemblyIdentity GetAssemblyIdentity(ProjectId projectId, MetadataReference metadataReference)
        {
            Project project = this.CurrentSolution.GetProject(projectId);
            if (!Enumerable.Contains<MetadataReference>((IEnumerable<MetadataReference>)project.MetadataReferences, metadataReference))
                project = project.AddMetadataReference(metadataReference);

            throw new NotImplementedException();
            //IAssemblySymbol assemblySymbol = Roslyn.Utilities.TaskExtensions.WaitAndGetResult<Compilation>(
            //    project.GetCompilationAsync(CancellationToken.None), CancellationToken.None)
            //    .GetAssemblyOrModuleSymbol(metadataReference) as IAssemblySymbol;
            //if (assemblySymbol == null)
            //    return (AssemblyIdentity)null;
            //else
            //    return assemblySymbol.Identity;
        }

        protected override void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            Project project = this.CurrentSolution.GetProject(projectReference.ProjectId);
            if (project != null)
                this._applyChangesProjectFile.AddProjectReference(project.Name, new ProjectFileReference(project.FilePath, projectReference.Aliases));
            this.OnProjectReferenceAdded(projectId, projectReference);
        }

        protected override void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            Project project = this.CurrentSolution.GetProject(projectReference.ProjectId);
            if (project != null)
                this._applyChangesProjectFile.RemoveProjectReference(project.Name, project.FilePath);
            this.OnProjectReferenceRemoved(projectId, projectReference);
        }

        protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            //this._applyChangesProjectFile.AddAnalyzerReference(analyzerReference);
            this.OnAnalyzerReferenceAdded(projectId, analyzerReference);
        }

        protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            //this._applyChangesProjectFile.RemoveAnalyzerReference(analyzerReference);
            this.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
        }

        private enum ReportMode
        {
            Throw,
            Log,
            Ignore,
        }

        private class ResolvedReferences
        {
            public readonly List<ProjectReference> ProjectReferences = new List<ProjectReference>();
            public readonly List<MetadataReference> MetadataReferences = new List<MetadataReference>();
        }
    }
}


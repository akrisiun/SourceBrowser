// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.ProjectFile
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location:  SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
//using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild
{
    // internal 
    public abstract class ProjectFile : IProjectFile
    {
        private static readonly SemaphoreSlim s_buildManagerLock = new SemaphoreSlim(1);
        private readonly ProjectFileLoader _loader;
        private readonly Microsoft.Build.Evaluation.Project _loadedProject;
        private IDictionary<string, ProjectItem> _documents;
        private int _state;

        public virtual string FilePath
        {
            get
            {
                return this._loadedProject.FullPath;
            }
        }

        public ProjectFile(ProjectFileLoader loader, Microsoft.Build.Evaluation.Project loadedProject)
        {
            this._loader = loader;
            this._loadedProject = loadedProject;
        }

        ~ProjectFile()
        {
            try
            {
                this._loadedProject.ProjectCollection.UnloadAllProjects();
            }
            catch
            {
            }
            finally
            {
                // ISSUE: explicit finalizer call
                // ISSUE: explicit non-virtual call
                //__nonvirtual (((object) this).Finalize());

                //((object)this).Finalize();
            }
        }

        public string GetPropertyValue(string name)
        {
            return this._loadedProject.GetPropertyValue(name);
        }

        public abstract SourceCodeKind GetSourceCodeKind(string documentFileName);

        public abstract string GetDocumentExtension(SourceCodeKind kind);

        // TODO
        //public abstract Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken);

        protected async Task<ProjectInstance> BuildAsync(string taskName, ITaskHost taskHost, CancellationToken cancellationToken)
        {
            ProjectInstance executedProject;
            
            //BuildTargets buildTargets;

            ProjectInstance projectInstance;
            HostServices hostServices;
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field
            if (_state != 0)
            {
                Microsoft.Build.Evaluation.Project project = this._loadedProject;
                string[] strArray = new string[1];
                int index = 0;
                string str = "Compile";
                strArray[index] = str;
                //buildTargets = new BuildTargets(project, strArray);

                //buildTargets.RemoveAfter("CoreCompile", false);

                executedProject = this._loadedProject.CreateProjectInstance();
                if (!executedProject.Targets.ContainsKey("Compile"))
                {
                    projectInstance = executedProject;
                    goto label_8;
                }
                else
                {
                    hostServices = new HostServices();
                    hostServices.RegisterHostObject(this._loadedProject.FullPath, "CoreCompile", taskName, taskHost);
                }
            }
            else
            {
                ConfiguredTaskAwaitable<BuildResult>.ConfiguredTaskAwaiter configuredTaskAwaiter = new ConfiguredTaskAwaitable<BuildResult>.ConfiguredTaskAwaiter();
                int num;
                // ISSUE: explicit reference operation
                // ISSUE: reference to a compiler-generated field
                _state = num = -1;
            }

            throw new NotImplementedException();
            //BuildResult buildResult = await this.BuildAsync(new BuildParameters(this._loadedProject.ProjectCollection), new BuildRequestData(executedProject, buildTargets.Targets, hostServices), cancellationToken).ConfigureAwait(false);
            //if (buildResult.Exception != null)
            //    throw buildResult.Exception;
            projectInstance = executedProject;
        label_8:
            return projectInstance;
        }

        private async Task<BuildResult> BuildAsync(BuildParameters parameters, BuildRequestData requestData, CancellationToken cancellationToken)
        {
            // ISSUE: explicit reference operation
            // ISSUE: reference to a compiler-generated field
            int num = _state;

            //SemaphoreSlimExtensions.SemaphoreDisposer semaphoreDisposer;
            switch (num)
            {
                case 0:
                    //ConfiguredTaskAwaitable<SemaphoreSlimExtensions.SemaphoreDisposer>.ConfiguredTaskAwaiter configuredTaskAwaiter1
                    //    = new ConfiguredTaskAwaitable<SemaphoreSlimExtensions.SemaphoreDisposer>.ConfiguredTaskAwaiter();

                    // ISSUE: explicit reference operation
                    // ISSUE: reference to a compiler-generated field
                    _state = num = -1;
                    break;
                case 1:
                    BuildResult buildResult;
                    try
                    {
                        //if (num == 1)
                        //{
                        //    ConfiguredTaskAwaitable<BuildResult>.ConfiguredTaskAwaiter configuredTaskAwaiter2
                        //        = new ConfiguredTaskAwaitable<BuildResult>.ConfiguredTaskAwaiter();
                        //    // ISSUE: explicit reference operation
                        //    // ISSUE: reference to a compiler-generated field
                        //    _state = num = -1;
                        //}
                        buildResult = await ProjectFile.BuildAsync(BuildManager.DefaultBuildManager, parameters, requestData,
                            cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        //if (num < 0 && semaphoreDisposer is IDisposable)
                        //    //semaphoreDisposer.Dispose();
                        //    (semaphoreDisposer as IDisposable).Dispose();
                    }
                    return buildResult;
            }
            //semaphoreDisposer = await SemaphoreSlimExtensions.DisposableWaitAsync(ProjectFile.s_buildManagerLock,
            //    cancellationToken).ConfigureAwait(false);
            
            //goto case 1;
            throw new NotImplementedException();
        }

        private static Task<BuildResult> BuildAsync(BuildManager buildManager, BuildParameters parameters,
            BuildRequestData requestData, CancellationToken cancellationToken)
        {
            TaskCompletionSource<BuildResult> taskSource = new TaskCompletionSource<BuildResult>();
            buildManager.BeginBuild(parameters);
            CancellationTokenRegistration registration = new CancellationTokenRegistration();
            if (cancellationToken.CanBeCanceled)
                registration = cancellationToken.Register((Action)(() =>
                {
                    try
                    {
                        buildManager.CancelAllSubmissions();
                        buildManager.EndBuild();
                        registration.Dispose();
                    }
                    finally
                    {
                        taskSource.TrySetCanceled();
                    }
                }));
            try
            {
                buildManager.PendBuildRequest(requestData).ExecuteAsync((BuildSubmissionCompleteCallback)(sub =>
                {
                    try
                    {
                        BuildResult buildResult = sub.BuildResult;
                        buildManager.EndBuild();
                        registration.Dispose();
                        taskSource.TrySetResult(buildResult);
                    }
                    catch (Exception ex)
                    {
                        taskSource.TrySetException(ex);
                    }
                }), (object)null);
            }
            catch (Exception ex)
            {
                taskSource.SetException(ex);
            }
            return taskSource.Task;
        }

        protected virtual string GetOutputDirectory()
        {
            string path = this._loadedProject.GetPropertyValue("TargetPath");
            if (string.IsNullOrEmpty(path))
                path = this._loadedProject.DirectoryPath;
            return System.IO.Path.GetDirectoryName(this.GetAbsolutePath(path));
        }

        protected virtual string GetAssemblyName()
        {
            string path = this._loadedProject.GetPropertyValue("AssemblyName");
            if (string.IsNullOrEmpty(path))
                path = System.IO.Path.GetFileNameWithoutExtension(this._loadedProject.FullPath);
            return PathUtilities.GetFileName(path);
        }

        protected bool IsProjectReferenceOutputAssembly(ITaskItem item)
        {
            return item.GetMetadata("ReferenceOutputAssembly") == "true";
        }

        //protected IEnumerable<ProjectFileReference> GetProjectReferences(ProjectInstance executedProject)
        //{
        //  // ISSUE: reference to a compiler-generated field
        //  // ISSUE: reference to a compiler-generated field
        //  // ISSUE: reference to a compiler-generated field
        //  // ISSUE: reference to a compiler-generated method
        //  return Enumerable.Select<ProjectItemInstance, ProjectFileReference>(Enumerable.Where<ProjectItemInstance>((IEnumerable<ProjectItemInstance>) executedProject.GetItems("ProjectReference"), 
        //      ProjectFile.\u003C\u003Ec.\u003C\u003E9__17_0 ?? (ProjectFile.\u003C\u003Ec.\u003C\u003E9__17_0 
        //          = new Func<ProjectItemInstance, bool>(ProjectFile.\u003C\u003Ec.\u003C\u003E9.\u003CGetProjectReferences\u003Eb__17_0))), new Func<ProjectItemInstance, ProjectFileReference>(this.CreateProjectFileReference));
        //}

        protected virtual ProjectFileReference CreateProjectFileReference(ProjectItemInstance reference)
        {
            return new ProjectFileReference(reference.EvaluatedInclude, ImmutableArray<string>.Empty);
        }

        protected virtual IEnumerable<ITaskItem> GetDocumentsFromModel(ProjectInstance executedProject)
        {
            return (IEnumerable<ITaskItem>)executedProject.GetItems("Compile");
        }

        protected virtual IEnumerable<ITaskItem> GetMetadataReferencesFromModel(ProjectInstance executedProject)
        {
            return (IEnumerable<ITaskItem>)executedProject.GetItems("ReferencePath");
        }

        protected virtual IEnumerable<ITaskItem> GetAnalyzerReferencesFromModel(ProjectInstance executedProject)
        {
            return (IEnumerable<ITaskItem>)executedProject.GetItems("Analyzer");
        }

        protected virtual IEnumerable<ITaskItem> GetAdditionalFilesFromModel(ProjectInstance executedProject)
        {
            return (IEnumerable<ITaskItem>)executedProject.GetItems("AdditionalFiles");
        }

        public ProjectProperty GetProperty(string name)
        {
            return this._loadedProject.GetProperty(name);
        }

        protected IEnumerable<ITaskItem> GetTaskItems(ProjectInstance executedProject, string itemType)
        {
            return (IEnumerable<ITaskItem>)executedProject.GetItems(itemType);
        }

        protected string GetItemString(ProjectInstance executedProject, string itemType)
        {
            string str = "";
            foreach (ProjectItemInstance projectItemInstance in (IEnumerable<ProjectItemInstance>)executedProject.GetItems(itemType))
            {
                if (str.Length > 0)
                    str = str + " ";
                str = str + projectItemInstance.EvaluatedInclude;
            }
            return str;
        }

        protected string ReadPropertyString(ProjectInstance executedProject, string propertyName)
        {
            ProjectInstance executedProject1 = executedProject;
            string str = propertyName;
            return this.ReadPropertyString(executedProject1, str, str);
        }

        protected string ReadPropertyString(ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            ProjectPropertyInstance property1 = executedProject.GetProperty(executedPropertyName);
            if (property1 != null)
                return property1.EvaluatedValue;
            ProjectProperty property2 = this._loadedProject.GetProperty(evaluatedPropertyName);
            if (property2 != null)
                return property2.EvaluatedValue;
            else
                return (string)null;
        }

        protected bool ReadPropertyBool(ProjectInstance executedProject, string propertyName)
        {
            return ProjectFile.ConvertToBool(this.ReadPropertyString(executedProject, propertyName));
        }

        protected bool ReadPropertyBool(ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            return ProjectFile.ConvertToBool(this.ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static bool ConvertToBool(string value)
        {
            if (value == null)
                return false;
            if (!string.Equals("true", value, StringComparison.OrdinalIgnoreCase))
                return string.Equals("On", value, StringComparison.OrdinalIgnoreCase);
            else
                return true;
        }

        protected int ReadPropertyInt(ProjectInstance executedProject, string propertyName)
        {
            return ProjectFile.ConvertToInt(this.ReadPropertyString(executedProject, propertyName));
        }

        protected int ReadPropertyInt(ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            return ProjectFile.ConvertToInt(this.ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static int ConvertToInt(string value)
        {
            if (value == null)
                return 0;
            int result;
            int.TryParse(value, out result);
            return result;
        }

        protected ulong ReadPropertyULong(ProjectInstance executedProject, string propertyName)
        {
            return ProjectFile.ConvertToULong(this.ReadPropertyString(executedProject, propertyName));
        }

        protected ulong ReadPropertyULong(ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName)
        {
            return ProjectFile.ConvertToULong(this.ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static ulong ConvertToULong(string value)
        {
            if (value == null)
                return 0UL;
            ulong result;
            ulong.TryParse(value, out result);
            return result;
        }

        protected TEnum? ReadPropertyEnum<TEnum>(ProjectInstance executedProject, string propertyName) where TEnum : struct
        {
            return ProjectFile.ConvertToEnum<TEnum>(this.ReadPropertyString(executedProject, propertyName));
        }

        protected TEnum? ReadPropertyEnum<TEnum>(ProjectInstance executedProject, string executedPropertyName, string evaluatedPropertyName) where TEnum : struct
        {
            return ProjectFile.ConvertToEnum<TEnum>(this.ReadPropertyString(executedProject, executedPropertyName, evaluatedPropertyName));
        }

        private static TEnum? ConvertToEnum<TEnum>(string value) where TEnum : struct
        {
            if (value == null)
                return new TEnum?();
            TEnum result;
            if (Enum.TryParse<TEnum>(value, out result))
                return new TEnum?(result);
            else
                return new TEnum?();
        }

        protected string GetAbsolutePath(string path)
        {
            return System.IO.Path.GetFullPath(FileUtilities.ResolveRelativePath(path, this._loadedProject.DirectoryPath) ?? path);
        }

        protected string GetDocumentFilePath(ITaskItem documentItem)
        {
            return this.GetAbsolutePath(documentItem.ItemSpec);
        }

        protected static bool IsDocumentLinked(ITaskItem documentItem)
        {
            return !string.IsNullOrEmpty(documentItem.GetMetadata("Link"));
        }

        protected bool IsDocumentGenerated(ITaskItem documentItem)
        {
            if (this._documents == null)
            {
                this._documents = (IDictionary<string, ProjectItem>)new Dictionary<string, ProjectItem>();
                foreach (ProjectItem projectItem in (IEnumerable<ProjectItem>)this._loadedProject.GetItems("compile"))
                    this._documents[this.GetAbsolutePath(projectItem.EvaluatedInclude)] = projectItem;
            }
            return !this._documents.ContainsKey(this.GetAbsolutePath(documentItem.ItemSpec));
        }

        protected static string GetDocumentLogicalPath(ITaskItem documentItem, string projectDirectory)
        {
            string metadata = documentItem.GetMetadata("Link");
            if (!string.IsNullOrEmpty(metadata))
                return metadata;
            string path = documentItem.ItemSpec;
            if (System.IO.Path.IsPathRooted(path))
            {
                string fullPath = System.IO.Path.GetFullPath(path);
                if (!fullPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                    return System.IO.Path.GetFileName(fullPath);
                path = fullPath.Substring(projectDirectory.Length);
            }
            return path;
        }

        protected string GetReferenceFilePath(ProjectItemInstance projectItem)
        {
            return this.GetAbsolutePath(projectItem.EvaluatedInclude);
        }

        public void AddDocument(string filePath, string logicalPath = null)
        {
            string unevaluatedInclude = FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, filePath);
            Dictionary<string, string> dictionary = (Dictionary<string, string>)null;
            if (logicalPath != null && unevaluatedInclude != logicalPath)
            {
                dictionary = new Dictionary<string, string>();
                dictionary.Add("link", logicalPath);
                unevaluatedInclude = filePath;
            }
            this._loadedProject.AddItem("Compile", unevaluatedInclude, (IEnumerable<KeyValuePair<string, string>>)dictionary);
        }

        public void RemoveDocument(string filePath)
        {
            string relativePath = FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, filePath);
            ProjectItem projectItem = Enumerable.FirstOrDefault<ProjectItem>((IEnumerable<ProjectItem>)this._loadedProject.GetItems("Compile"), (Func<ProjectItem, bool>)(it =>
            {
                if (!FilePathUtilities.PathsEqual(it.EvaluatedInclude, relativePath))
                    return FilePathUtilities.PathsEqual(it.EvaluatedInclude, filePath);
                else
                    return true;
            }));
            if (projectItem == null)
                return;
            this._loadedProject.RemoveItem(projectItem);
        }

        public void AddMetadataReference(Microsoft.CodeAnalysis.MetadataReference reference, AssemblyIdentity identity)
        {
            PortableExecutableReference executableReference = reference as PortableExecutableReference;
            if (executableReference == null || executableReference.FilePath == null)
                return;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            if (!executableReference.Properties.Aliases.IsEmpty)
                dictionary.Add("Aliases", string.Join(",", (IEnumerable<string>)executableReference.Properties.Aliases));
            if (this.IsInGAC(executableReference.FilePath) && identity != (AssemblyIdentity)null)
                this._loadedProject.AddItem("Reference", identity.GetDisplayName(false), (IEnumerable<KeyValuePair<string, string>>)dictionary);
            else
                this._loadedProject.AddItem("Reference", FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, executableReference.FilePath), (IEnumerable<KeyValuePair<string, string>>)dictionary);
        }

        private bool IsInGAC(string filePath)
        {
            return filePath.Contains("\\GAC_MSIL\\");
        }

        public void RemoveMetadataReference(Microsoft.CodeAnalysis.MetadataReference reference, AssemblyIdentity identity)
        {
            PortableExecutableReference executableReference = reference as PortableExecutableReference;
            if (executableReference == null || executableReference.FilePath == null)
                return;
            ProjectItem referenceItem = this.FindReferenceItem(identity, executableReference.FilePath);
            if (referenceItem == null)
                return;
            this._loadedProject.RemoveItem(referenceItem);
        }

        private ProjectItem FindReferenceItem(AssemblyIdentity identity, string filePath)
        {
            ICollection<ProjectItem> items = this._loadedProject.GetItems("Reference");
            ProjectItem projectItem = (ProjectItem)null;
            if (identity != (AssemblyIdentity)null)
            {
                string shortAssemblyName = identity.Name;
                throw new NotImplementedException();
                //string fullAssemblyName = AssemblyIdentityExtensions.ToAssemblyName(identity).FullName;
                //projectItem = Enumerable.FirstOrDefault<ProjectItem>((IEnumerable<ProjectItem>)items, (Func<ProjectItem, bool>)(it => string.Compare(it.EvaluatedInclude, shortAssemblyName, StringComparison.OrdinalIgnoreCase) == 0)) ?? Enumerable.FirstOrDefault<ProjectItem>((IEnumerable<ProjectItem>)items, (Func<ProjectItem, bool>)(it => string.Compare(it.EvaluatedInclude, fullAssemblyName, StringComparison.OrdinalIgnoreCase) == 0));
            }
            if (projectItem == null)
            {
                string relativePath = FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, filePath);
                projectItem = Enumerable.FirstOrDefault<ProjectItem>((IEnumerable<ProjectItem>)items, (Func<ProjectItem, bool>)(it =>
                {
                    if (!FilePathUtilities.PathsEqual(it.EvaluatedInclude, filePath))
                        return FilePathUtilities.PathsEqual(it.EvaluatedInclude, relativePath);
                    else
                        return true;
                }));
            }
            if (projectItem == null && identity != (AssemblyIdentity)null)
            {
                string partialName = identity.Name + ",";
                List<ProjectItem> list = Enumerable.ToList<ProjectItem>(Enumerable.Where<ProjectItem>((IEnumerable<ProjectItem>)items, (Func<ProjectItem, bool>)(it => it.EvaluatedInclude.StartsWith(partialName, StringComparison.OrdinalIgnoreCase))));
                if (list.Count == 1)
                    projectItem = list[0];
            }
            return projectItem;
        }

        public void AddProjectReference(string projectName, ProjectFileReference reference)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("Name", projectName);
            if (!reference.Aliases.IsEmpty)
                dictionary.Add("Aliases", string.Join(",", (IEnumerable<string>)reference.Aliases));
            this._loadedProject.AddItem("ProjectReference", FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, reference.Path), (IEnumerable<KeyValuePair<string, string>>)dictionary);
        }

        public void RemoveProjectReference(string projectName, string projectFilePath)
        {
            FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, projectFilePath);
            ProjectItem projectReferenceItem = this.FindProjectReferenceItem(projectName, projectFilePath);
            if (projectReferenceItem == null)
                return;
            this._loadedProject.RemoveItem(projectReferenceItem);
        }

        private ProjectItem FindProjectReferenceItem(string projectName, string projectFilePath)
        {
            ICollection<ProjectItem> items = this._loadedProject.GetItems("ProjectReference");
            string relativePath = FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, projectFilePath);
            return Enumerable.First<ProjectItem>((IEnumerable<ProjectItem>)items, (Func<ProjectItem, bool>)(it =>
            {
                if (!FilePathUtilities.PathsEqual(it.EvaluatedInclude, relativePath))
                    return FilePathUtilities.PathsEqual(it.EvaluatedInclude, projectFilePath);
                else
                    return true;
            })) ?? Enumerable.First<ProjectItem>((IEnumerable<ProjectItem>)items, (Func<ProjectItem, bool>)(it => string.Compare(projectName, it.GetMetadataValue("Name"), StringComparison.OrdinalIgnoreCase) == 0));
        }

        public void AddAnalyzerReference(AnalyzerReference reference)
        {
            AnalyzerFileReference analyzerFileReference = reference as AnalyzerFileReference;
            if (analyzerFileReference == null)
                return;
            this._loadedProject.AddItem("Analyzer", FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, analyzerFileReference.FullPath));
        }

        public void RemoveAnalyzerReference(AnalyzerReference reference)
        {
            AnalyzerFileReference fileRef = reference as AnalyzerFileReference;
            if (fileRef == null)
                return;
            string relativePath = FilePathUtilities.GetRelativePath(this._loadedProject.DirectoryPath, fileRef.FullPath);
            ProjectItem projectItem = Enumerable.FirstOrDefault<ProjectItem>((IEnumerable<ProjectItem>)this._loadedProject.GetItems("Analyzer"), (Func<ProjectItem, bool>)(it =>
            {
                if (!FilePathUtilities.PathsEqual(it.EvaluatedInclude, relativePath))
                    return FilePathUtilities.PathsEqual(it.EvaluatedInclude, fileRef.FullPath);
                else
                    return true;
            }));
            if (projectItem == null)
                return;
            this._loadedProject.RemoveItem(projectItem);
        }

        public void Save()
        {
            this._loadedProject.Save();
        }

        internal static bool TryGetOutputKind(string outputKind, out OutputKind kind)
        {
            if (string.Equals(outputKind, "Library", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.DynamicallyLinkedLibrary;
                return true;
            }
            else if (string.Equals(outputKind, "Exe", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.ConsoleApplication;
                return true;
            }
            else if (string.Equals(outputKind, "WinExe", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.WindowsApplication;
                return true;
            }
            else if (string.Equals(outputKind, "Module", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.NetModule;
                return true;
            }
            else if (string.Equals(outputKind, "WinMDObj", StringComparison.OrdinalIgnoreCase))
            {
                kind = OutputKind.WindowsRuntimeMetadata;
                return true;
            }
            else
            {
                kind = OutputKind.DynamicallyLinkedLibrary;
                return false;
            }
        }
    }

    public sealed class ProjectFileReference
    {
        public string Path
        {
            get;
            set;
        }

        public ImmutableArray<string> Aliases
        {
            get;
            set;
        }

        public ProjectFileReference(string path, ImmutableArray<string> aliases)
        {
            // ISSUE: reference to a compiler-generated field
            this.Path = path;
            this.Aliases = aliases;
        }
    }

}

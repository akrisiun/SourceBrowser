using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.SourceBrowser.Common;

namespace Microsoft.CodeAnalysis.MSBuild
{
    using Microsoft.SourceBrowser.HtmlGenerator;
    using Microsoft.CodeAnalysis.Host;
    using Microsoft.Build.Construction;
    // using System.Collections.Generic;
    using System.Collections;

    internal enum ReportMode
    {
        Throw,
        Log,
        Ignore
    }

    internal interface IProjectFileLoader // : ILanguageService
    {
        string Language { get; }

        Task<IProjectFile> LoadProjectFileAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken);
    }

     internal interface IProjectFile
    {
        string FilePath { get; }
        string ErrorMessage { get; }
    }

    internal class LoadState
    {
            private Dictionary<ProjectId, ProjectInfo> _projectIdToProjectInfoMap
                = new Dictionary<ProjectId, ProjectInfo>();
 
            /// <summary>
            /// Used to memoize results of <see cref="ProjectAlreadyReferencesProject"/> calls.
            /// Reset any time internal state is changed.
            /// </summary>
            private Dictionary<ProjectId, Dictionary<ProjectId, bool>> _projectAlreadyReferencesProjectResultCache
                = new Dictionary<ProjectId, Dictionary<ProjectId, bool>>();
            private readonly  Dictionary<string, ProjectId> _projectPathToProjectIdMap
                 = new Dictionary<string, ProjectId>(PathUtilities.Comparer);
 
            public LoadState(IReadOnlyDictionary<string, ProjectId> projectPathToProjectIdMap)
            {
                if (projectPathToProjectIdMap != null)
                {
                    foreach(var item in projectPathToProjectIdMap)
                    {
                        ProjectInfo info  =  AdhocProject(item.Value, item.Key, "C#");
                        Add(info);
                    }
                }
            }

            public ProjectInfo AdhocProject(ProjectId id, string name, string language)
            {
                // CreateFromSerialized(Guid id
                // Guid idGuid = id != null ? System.Guid.Parse(id) : default(Guid);

                var id2 = id ?? Microsoft.CodeAnalysis.ProjectId.CreateNewId();
                var info = ProjectInfo.Create(id2, VersionStamp.Create(), name, name, language);
                return info;
            }
 
            public void Add(ProjectInfo info)
            {
                _projectIdToProjectInfoMap.Add(info.Id, info);
                //Memoized results of ProjectAlreadyReferencesProject may no longer be correct;
                //reset the cache.
                _projectAlreadyReferencesProjectResultCache.Clear();
            }

            public ProjectId GetOrCreateProjectId(string fullProjectPath)
            {
                if (!_projectPathToProjectIdMap.TryGetValue(fullProjectPath, out var id))
                {
                    id = ProjectId.CreateNewId(debugName: fullProjectPath);
                    _projectPathToProjectIdMap.Add(fullProjectPath, id);
                }
 
                return id;
            }

            public static SemaphoreDisposer DisposableWait(object @this, CancellationToken cancellationToken = default(CancellationToken))
            {
                // this.Wait(cancellationToken);
                return new SemaphoreDisposer(@this);
            }

            /// <summary>
            /// Since we want to avoid boxing the return from <see cref="NonReentrantLock.DisposableWait"/>, this type must be public.
            /// </summary>
            public struct SemaphoreDisposer : IDisposable
            {
                private readonly object _semaphore; // NonReentrantLock _semaphore;

                /* TODO: public static CancellationTokenRegistration Register(Action<Object> callback, Object state, bool useSynchronizationContext)
                {
                    return Register(
                        callback,
                        state,
                        useSynchronizationContext,
                        true   // useExecutionContext=true
                    );
                }
                */

                public SemaphoreDisposer(object target) // , NonReentrantLock semaphore)
                {
                     _semaphore = null; // semaphore;
                }

                public void Dispose()
                {
                    // _semaphore.Release();
                }
            }
            public static bool  TryGetLoaderFromProjectPath(MSBuildWorkspace _workspace, string projectFilePath, ReportMode mode, 
                   out ILanguageService loader)
            {
                using (DisposableWait(_workspace))
                {
                    // otherwise try to figure it out from extension
                    var extension = Path.GetExtension(projectFilePath);
                    if (extension.Length > 0 && extension[0] == '.')
                    {
                        extension = extension.Substring(1);
                    }
    
                    /* if (_extensionToLanguageMap.TryGetValue(extension, out var language))
                            loader = _workspace.Services.GetLanguageServices(language).GetService<IProjectFileLoader>();
                            // if this.ReportFailure(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projectFilePath, language));
                    }  else */
                    {
                        loader = GetLoaderForProjectFileExtension(_workspace, extension);
    
                        if (loader == null)
                        {
                            // this.ReportFailure(mode, string.Format(Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, projectFilePath, Path.GetExtension(projectFilePath)));
                            Console.WriteLine($"Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language : {projectFilePath} {Path.GetExtension(projectFilePath)}");
                            // return false;
                        }
                    }

                    if (true) 
                    {
                       var csproj = projectFilePath;
 
                        var loaderWS = new MSBuildProjectLoader(_workspace);
                        var infos = loaderWS.LoadProjectInfoAsync(csproj).Result;
                        ProjectInfo info = infos.FirstOrDefault();

                        // loader = infos as ILanguageService ?? loader;
                    }
    
                    // since we have both C# and VB loaders in this same library, it no longer indicates whether we have full language support available.
                    if (true) // loader != null)
                    {
                        var language = "C#"; // loader.Language;
    
                        // check for command line parser existing... if not then error.
                        var commandLineParser = _workspace.Services.GetLanguageServices(language).GetService<ICommandLineParserService>();
                        if (commandLineParser == null)
                        {
                            // loader = null;
                            // this.ReportFailure(mode, $"Cannot_open_project_0_because_the_language_1_is_not_supported : {projectFilePath} {language})");
                            Console.WriteLine($"Cannot_open_project_0_because_the_language_1_is_not_supported : {projectFilePath} {language})"); // .Red
                            // return false;
                        }
                    }
    
                    return loader != null;
                }
            }
        
            public static ILanguageService GetLoaderForProjectFileExtension(Workspace workspace, string extension)
            {
                // (workspace.Services as Microsoft.CodeAnalysis.Host.HostWorkspaceServices).HostServices
                // WorkspaceHacks.Pack as HostServices

                var languages = 
                   (workspace.Services as Microsoft.CodeAnalysis.Host.HostWorkspaceServices).GetLanguageServices("C#")
                   as HostLanguageServices;
                var list = languages.WorkspaceServices.GetLanguageServices("C#");

                // return workspace.Services.FindLanguageServices<IProjectFileLoader>(
                var loader = workspace.Services.FindLanguageServices<ILanguageService>(
                    d => Any(d))
                    .FirstOrDefault();
                
                if (loader == null)
                {
                    // second chance
                    var loadern2 = workspace.Services.FindLanguageServices<HostLanguageServices>(dd => true);
                    var nn2 = loadern2.GetEnumerator();
                    nn2.MoveNext();
                    var loader2 = nn2.Current as ILanguageService;
                }

                return loader as ILanguageService;
            }

            public static bool Any(object when)
            {
                var extension = "csproj";
                var d = when as IReadOnlyDictionary<string, object>; // MetadataFilter;
                var any = d.GetEnumerableMetadata<string>
                        ("ProjectFileExtension").Any(e => string.Equals(e,
                             extension, StringComparison.OrdinalIgnoreCase));
                return any;
            }

            public static ProjectId GetProjectId(LoadState loadedProjects, string fullProjectPath)
            {
                loadedProjects._projectPathToProjectIdMap.TryGetValue(fullProjectPath, out var id);
                return id;
            }
    
            //  var tmp = await GetOrLoadProjectAsync(projectAbsolutePath,
            public static ProjectId // async Task<ProjectId> 
                GetOrLoadProjectAsync(MSBuildWorkspace workspace,
                string projectFilePath, ILanguageService loader, // IProjectFileLoader loader,
                bool preferMetadata, LoadState loadedProjects, CancellationToken cancellationToken)
            {
                var projectId = GetProjectId(loadedProjects, projectFilePath);
                if (projectId == null)
                {
                    // await
                    projectId = LoadProjectAsync(workspace, projectFilePath,
                        loader, preferMetadata, loadedProjects, cancellationToken);
                        // .ConfigureAwait(false);
                }
    
                return projectId;
            }

            // LoadProjectAsync
            public static ProjectId // async Task<ProjectId> 
                LoadProjectAsync(MSBuildWorkspace workspace, string projectFilePath,
                ILanguageService loader, // IProjectFileLoader loader, 
                bool preferMetadata, 
                LoadState loadedProjects, CancellationToken cancellationToken)
            {
                // Debug.Assert(projectFilePath != null); loader != null);
    
                var projectId = loadedProjects.GetOrCreateProjectId(projectFilePath);
                var projectName = Path.GetFileNameWithoutExtension(projectFilePath);
                var _properties = workspace.Properties;

                var typ = loader.GetType();
                var met = typ.GetMethods();
                var metCall = met.FirstOrDefault(m => m.Name.StartsWith("LoadProjectFileAsync"));

                //  dynamic loaderX =  loader; // as IProjectFile;
                /* var projectFile = await loaderX.LoadProjectFileAsync(projectFilePath, _properties, cancellationToken).ConfigureAwait(false);
                if (projectFile.ErrorMessage != null)
                {
                    // GetMsbuildFailedMessage(projectFilePath, projectFile.ErrorMessage));
                    return null;
                }  */

                var name = Path.GetFileNameWithoutExtension(projectFilePath);
                ProjectId projectFileInfo = CreateProjectFileInfo(name); // await GetProjectFileInfoAsync(cancellationToken).ConfigureAwait(false);
    
                return projectFileInfo;
            }

            static ProjectId CreateProjectFileInfo(string assemblyName) // CSharpCompilerInputs compilerInputs, BuildInfo buildInfo)
            {
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "bin"); // this.GetOutputDirectory() , compilerInputs.OutputFileName);
                // var assemblyName = this.GetAssemblyName();
                ProjectId id = ProjectId.CreateNewId(assemblyName);
 
                /* ProjectFileInfo project = null; // buildInfo.Project;
                if (project == null)
                {
                    return new ProjectFileInfo(
                        outputPath,
                        assemblyName,
                        commandLineArgs: Enumerable.Empty<string>(),
                        documents: Enumerable.Empty<DocumentFileInfo>(),
                        additionalDocuments: Enumerable.Empty<DocumentFileInfo>(),
                        projectReferences: Enumerable.Empty<ProjectFileReference>(),
                        errorMessage: ""); // buildInfo.ErrorMessage);
                }
                */
                return id;
            }
    }

    #region Internal

    internal static class IReadOnlyDictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
 
            return default(TValue);
        }
 
        public static IEnumerable<T> GetEnumerableMetadata<T>(this IReadOnlyDictionary<string, object> metadata, string name)
        {
            switch (metadata.GetValueOrDefault(name))
            {
                case IEnumerable<T> enumerable: return enumerable;
                case T s: return SingletonEnumerable(s);
                default: return  Enumerable.Empty<T>();
                    // SpecializedCollections.EmptyEnumerable<T>();
            }
        }

        public static IEnumerable<T> SingletonEnumerable<T>(T value)
        {
            return new Singleton.List<T>(value);
        }
    }

    internal static partial class Singleton
    {
        internal sealed class List<T> : IList<T>, IReadOnlyCollection<T>
        {
            private readonly T _loneValue;

            public List(T value)
            {
                _loneValue = value;
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return EqualityComparer<T>.Default.Equals(_loneValue, item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                array[arrayIndex] = _loneValue;
            }

            public int Count => 1;

            public bool IsReadOnly => true;

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator<T>(_loneValue);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public T this[int index]
            {
                get
                {
                    if (index != 0)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return _loneValue;
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public int IndexOf(T item)
            {
                if (Equals(_loneValue, item))
                {
                    return 0;
                }

                return -1;
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
        }

        internal class Enumerator<T> : IEnumerator<T>
        {
            private readonly T _loneValue;
            private bool _moveNextCalled;

            public Enumerator(T value)
            {
                _loneValue = value;
                _moveNextCalled = false;
            }

            public T Current => _loneValue;

            object IEnumerator.Current => _loneValue;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (!_moveNextCalled)
                {
                    _moveNextCalled = true;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _moveNextCalled = false;
            }
        }

    }


    public class PathUtilities
    {
            public static readonly IEqualityComparer<string> Comparer = new PathComparer();
    
            private class PathComparer : IEqualityComparer<string>
            {
                public bool Equals(string x, string y)
                {
                    if (x == null && y == null)
                    {
                        return true;
                    }
    
                    if (x == null || y == null)
                    {
                        return false;
                    }
    
                    return PathsEqual(x, y);
                }
    
                public int GetHashCode(string s)
                {
                    return PathHashCode(s);
                }
            }

            public static bool PathsEqual(string path1, string path2)
            {
                return PathsEqual(path1, path2, Math.Max(path1.Length, path2.Length));
            }

                private static bool PathsEqual(string path1, string path2, int length)
            {
                if (path1.Length < length || path2.Length < length)
                {
                    return false;
                }
    
                for (int i = 0; i < length; i++)
                {
                    if (!PathCharEqual(path1[i], path2[i]))
                    {
                        return false;
                    }
                }
    
                return true;
            }

                private static bool PathCharEqual(char x, char y)
            {
                if ( Path.DirectorySeparatorChar.Equals(x)
                    && Path.DirectorySeparatorChar.Equals(y)) // IsDirectorySeparator(x) && IsDirectorySeparator(y))
                {
                    return true;
                }
    
                return IsUnixLikePlatform 
                    ? x == y 
                    : char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
            }

            internal static bool IsUnixLikePlatform => PlatformInformation.IsUnix;

        private static int PathHashCode(string path)
                {
                    int hc = 0;
        
                    if (path != null)
                    {
                        foreach (var ch in path)
                        {
                            if (!Path.DirectorySeparatorChar.Equals(ch)) // IsDirectorySeparator(ch))
                            {
                                hc = Hash.Combine((int)char.ToUpperInvariant(ch), hc);
                            }
                        }
                    }
        
                    return hc;
                }
    }

    internal static class PlatformInformation
    {
        public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        public static bool IsUnix => Path.DirectorySeparatorChar == '/';
    }

    internal static class Hash
    {
        /// <summary>
        /// This is how VB Anonymous Types combine hash values for fields.
        /// </summary>
        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }
 
        internal static int Combine(bool newKeyPart, int currentKey)
        {
            return Combine(currentKey, newKeyPart ? 1 : 0);
        }
 
        /// <summary>
        /// This is how VB Anonymous Types combine hash values for fields.
        /// PERF: Do not use with enum types because that involves multiple
        /// unnecessary boxing operations.  Unfortunately, we can't constrain
        /// T to "non-enum", so we'll use a more restrictive constraint.
        /// </summary>
        internal static int Combine<T>(T newKeyPart, int currentKey) where T : class
        {
            int hash = unchecked(currentKey * (int)0xA5555529);
 
            if (newKeyPart != null)
            {
                return unchecked(hash + newKeyPart.GetHashCode());
            }
 
            return hash;
        }
 
        internal static int CombineValues<T>(IEnumerable<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }
 
            var hashCode = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }
 
                // Should end up with a constrained virtual call to object.GetHashCode (i.e. avoid boxing where possible).
                if (value != null)
                {
                    hashCode = Hash.Combine(value.GetHashCode(), hashCode);
                }
            }
 
            return hashCode;
        }
 
        internal static int CombineValues<T>(T[] values, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }
 
            var maxSize = Math.Min(maxItemsToHash, values.Length);
            var hashCode = 0;
 
            for (int i = 0; i < maxSize; i++)
            {
                T value = values[i];
 
                // Should end up with a constrained virtual call to object.GetHashCode (i.e. avoid boxing where possible).
                if (value != null)
                {
                    hashCode = Hash.Combine(value.GetHashCode(), hashCode);
                }
            }
 
            return hashCode;
        }
 
        internal static int CombineValues<T>(ImmutableArray<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values.IsDefaultOrEmpty)
            {
                return 0;
            }
 
            var hashCode = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }
 
                // Should end up with a constrained virtual call to object.GetHashCode (i.e. avoid boxing where possible).
                if (value != null)
                {
                    hashCode = Hash.Combine(value.GetHashCode(), hashCode);
                }
            }
 
            return hashCode;
        }
 
        internal static int CombineValues(IEnumerable<string> values, StringComparer stringComparer, int maxItemsToHash = int.MaxValue)
        {
            if (values == null)
            {
                return 0;
            }
 
            var hashCode = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }
 
                if (value != null)
                {
                    hashCode = Hash.Combine(stringComparer.GetHashCode(value), hashCode);
                }
            }
 
            return hashCode;
        }
 
        /// <summary>
        /// The offset bias value used in the FNV-1a algorithm
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        internal const int FnvOffsetBias = unchecked((int)2166136261);
        internal const int FnvPrime = 16777619;
 
  
        /// <summary>
        /// Compute the FNV-1a hash of a sequence of bytes
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// </summary>
        /// <param name="data">The sequence of bytes</param>
        /// <returns>The FNV-1a hash of <paramref name="data"/></returns>
        internal static int GetFNVHashCode(ImmutableArray<byte> data)
        {
            int hashCode = Hash.FnvOffsetBias;
 
            for (int i = 0; i < data.Length; i++)
            {
                hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
            }
 
            return hashCode;
        }
 
        /// <summary>
        /// Compute the hashcode of a sub-string using FNV-1a
        /// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        /// Note: FNV-1a was developed and tuned for 8-bit sequences. We're using it here
        /// for 16-bit Unicode chars on the understanding that the majority of chars will
        /// fit into 8-bits and, therefore, the algorithm will retain its desirable traits
        /// for generating hash codes.
        /// </summary>
        /// <param name="text">The input string</param>
        /// <param name="start">The start index of the first character to hash</param>
        /// <param name="length">The number of characters, beginning with <paramref name="start"/> to hash</param>
        /// <returns>The FNV-1a hash code of the substring beginning at <paramref name="start"/> and ending after <paramref name="length"/> characters.</returns>
        internal static int GetFNVHashCode(string text, int start, int length)
        {
            int hashCode = Hash.FnvOffsetBias;
            int end = start + length;
 
            for (int i = start; i < end; i++)
            {
                hashCode = unchecked((hashCode ^ text[i]) * Hash.FnvPrime);
            }
 
            return hashCode;
        }
 
        internal static int GetCaseInsensitiveFNVHashCode(string text)
        {
            return GetCaseInsensitiveFNVHashCode(text, 0, text.Length);
        }
 
        internal static int GetCaseInsensitiveFNVHashCode(string text, int start, int length)
        {
            int hashCode = Hash.FnvOffsetBias;
            int end = start + length;
 
            for (int i = start; i < end; i++)
            {
                hashCode = unchecked((hashCode ^ CaseInsensitiveComparison.ToLower(text[i])) * Hash.FnvPrime);
            }
 
            return hashCode;
        }
  
        internal static int CombineFNVHash(int hashCode, string text)
        {
            foreach (char ch in text)
            {
                hashCode = unchecked((hashCode ^ ch) * Hash.FnvPrime);
            }
 
            return hashCode;
        }
 
        // Combine a char with an existing FNV-1a hash code
        // See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
        // <returns>The result of combining <paramref name="hashCode"/> with <paramref name="ch"/> using the FNV-1a algorithm</returns>
        internal static int CombineFNVHash(int hashCode, char ch)
        {
            return unchecked((hashCode ^ ch) * Hash.FnvPrime);
        }
    }

    #endregion
}

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ICommandLineParserService : ILanguageService
    {
        CommandLineArguments Parse(IEnumerable<string> arguments, string baseDirectory, bool isInteractive, string sdkDirectory);
    }
}

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public partial class SolutionGenerator : IDisposable
    {
        public string SolutionSourceFolder { get; private set; }
        public string SolutionDestinationFolder { get; private set; }
        public string ProjectFilePath { get; private set; }
        public string ServerPath { get; set; }
        public IReadOnlyDictionary<string, string> ServerPathMappings { get; }
        public string NetworkShare { get; private set; }
        private Federation Federation { get; set; }
        public IEnumerable<string> PluginBlacklist { get; private set; }
        private readonly HashSet<string> typeScriptFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public MEF.PluginAggregator PluginAggregator;

        public static Exception Errors { get; set; }

        /// <summary>
        /// List of all assembly names included in the index, from all solutions
        /// </summary>
        public HashSet<string> GlobalAssemblyList { get; set; }

        private Solution solution;
        private Workspace workspace;
        private MSBuildWorkspace MSBWorkspace => workspace as MSBuildWorkspace;

        public SolutionGenerator(
            string solutionFilePath,
            string solutionDestinationFolder,
            string serverPath = null,
            ImmutableDictionary<string, string> properties = null,
            Federation federation = null,
            IReadOnlyDictionary<string, string> serverPathMappings = null,
            IEnumerable<string> pluginBlacklist = null)
        {
            Errors = null;

            this.SolutionSourceFolder = Path.GetDirectoryName(solutionFilePath);
            this.SolutionDestinationFolder = solutionDestinationFolder;
            this.ProjectFilePath = solutionFilePath;
            this.ServerPath = serverPath;
            ServerPathMappings = serverPathMappings;

            this.solution = CreateSolution(solutionFilePath, properties);
            this.Federation = federation ?? new Federation();
            this.PluginBlacklist = pluginBlacklist ?? Enumerable.Empty<string>();

            if (LoadPlugins)
            {
                SetupPluginAggregator();
            }
        }

        public static bool LoadPlugins { get; set; } = true;

        private void SetupPluginAggregator()
        {
            var settings = System.Configuration.ConfigurationManager.AppSettings;
            var configs = settings
                .AllKeys
                .Where(k => k.Contains(':'))                            //Ignore keys that don't have a colon to indicate which plugin they go to
                .Select(k => Tuple.Create(k.Split(':'), settings[k]))   //Get the data -- split the key to get the plugin name and setting name, look up the key to get the value
                .GroupBy(t => t.Item1[0])                               //Group the settings based on which plugin they're for
                .ToDictionary(
                    group => group.Key,                                 //Index the outer dictionary based on plugin
                    group => group.ToDictionary(
                        t => t.Item1[1],                                //Index the inner dictionary based on setting name
                        t => t.Item2                                    //The actual value of the setting
                    )
                );

            PluginAggregator = new MEF.PluginAggregator(configs, new Utilities.PluginLogger(), PluginBlacklist);

            try
            {
                PluginAggregator.Wrap();

                if (PluginAggregator.Any())
                    FirstChanceExceptionHandler.IgnoreModules(PluginAggregator.Select(p => p.PluginModule));

                PluginAggregator.Init();
            }
            catch (Exception ex) { PluginAggregator.LoadErrors = ex; }
        }

        public SolutionGenerator(
            string projectFilePath,
            string commandLineArguments,
            string outputAssemblyPath,
            string solutionSourceFolder,
            string solutionDestinationFolder,
            string serverPath,
            string networkShare)
        {
            this.ProjectFilePath = projectFilePath;
            string projectName = Path.GetFileNameWithoutExtension(projectFilePath);
            string language = projectFilePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) ?
                LanguageNames.VisualBasic : LanguageNames.CSharp;
            this.SolutionSourceFolder = solutionSourceFolder;
            this.SolutionDestinationFolder = solutionDestinationFolder;
            this.ServerPath = serverPath;
            this.NetworkShare = networkShare;
            string projectSourceFolder = Path.GetDirectoryName(projectFilePath);
            SetupPluginAggregator();

            MSBuildWorkspace msb = null;
            this.solution = CreateSolution(
                commandLineArguments,
                projectName,
                language,
                projectSourceFolder,
                outputAssemblyPath, out msb);

            this.workspace = msb;
        }

        public IEnumerable<string> GetAssemblyNames()
        {
            if (solution != null)
            {
                return solution.Projects.Select(p => p.AssemblyName);
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }

        public static MSBuildWorkspace CreateWorkspace(ImmutableDictionary<string, string> propertiesOpt = null)
        {
            propertiesOpt = propertiesOpt ?? ImmutableDictionary<string, string>.Empty;

            // Explicitly add "CheckForSystemRuntimeDependency = true" property to correctly resolve facade references.
            // See https://github.com/dotnet/roslyn/issues/560
            propertiesOpt = propertiesOpt.Add("CheckForSystemRuntimeDependency", "true");
            propertiesOpt = propertiesOpt.Add("VisualStudioVersion", "15.0");

            var w = MSBuildWorkspace.Create(properties: propertiesOpt, hostServices: WorkspaceHacks.Pack);
            w.LoadMetadataForReferencedProjects = true;
            return w;
        }

        public static Solution CreateSolution(
            string commandLineArguments,
            string projectName,
            string language,
            string projectSourceFolder,
            string outputAssemblyPath,
            out MSBuildWorkspace msb)
        {
            var workspace = CreateWorkspace();
            msb = workspace;

            var projectInfo = CommandLineProject.CreateProjectInfo(
                projectName,
                language,
                commandLineArguments,
                projectSourceFolder,
                workspace);
            var solution = workspace.CurrentSolution.AddProject(projectInfo);

            solution = RemoveNonExistingFiles(solution);
            solution = AddAssemblyAttributesFile(language, outputAssemblyPath, solution);
            solution = DisambiguateSameNameLinkedFiles(solution);

            solution.Workspace.WorkspaceFailed += WorkspaceFailed;

            return solution;
        }

        private static Solution DisambiguateSameNameLinkedFiles(Solution solution)
        {
            foreach (var projectId in solution.ProjectIds.ToArray())
            {
                var project = solution.GetProject(projectId);
                solution = DisambiguateSameNameLinkedFiles(project);
            }

            return solution;
        }

        /// <summary>
        /// If there are two linked files both outside the project cone, and they have same names,
        /// they will logically appear as the same file in the project root. To disambiguate, we
        /// remove both files from the project's root and re-add them each into a folder chain that
        /// is formed from the full path of each document.
        /// </summary>
        private static Solution DisambiguateSameNameLinkedFiles(Project project)
        {
            var nameMap = project.Documents.Where(d => !d.Folders.Any()).ToLookup(d => d.Name);
            foreach (var conflictedItemGroup in nameMap.Where(g => g.Count() > 1))
            {
                foreach (var conflictedDocument in conflictedItemGroup)
                {
                    project = project.RemoveDocument(conflictedDocument.Id);
                    string filePath = conflictedDocument.FilePath;
                    DocumentId newId = DocumentId.CreateNewId(project.Id, filePath);
                    var folders = filePath.Split('\\').Select(p => p.TrimEnd(':'));
                    project = project.Solution.AddDocument(
                        newId,
                        conflictedDocument.Name,
                        conflictedDocument.GetTextAsync().Result,
                        folders,
                        filePath).GetProject(project.Id);
                }
            }

            return project.Solution;
        }

        private static Solution RemoveNonExistingFiles(Solution solution)
        {
            foreach (var projectId in solution.ProjectIds.ToArray())
            {
                var project = solution.GetProject(projectId);
                solution = RemoveNonExistingDocuments(project);

                project = solution.GetProject(projectId);
                solution = RemoveNonExistingReferences(project);
            }

            return solution;
        }

        private static Solution RemoveNonExistingDocuments(Project project)
        {
            foreach (var documentId in project.DocumentIds.ToArray())
            {
                var document = project.GetDocument(documentId);
                if (!File.Exists(document.FilePath))
                {
                    Log.Message("Document doesn't exist on disk: " + document.FilePath);
                    project = project.RemoveDocument(documentId);
                }
            }

            return project.Solution;
        }

        private static Solution RemoveNonExistingReferences(Project project)
        {
            foreach (var metadataReference in project.MetadataReferences.ToArray())
            {
                if (!File.Exists(metadataReference.Display))
                {
                    Log.Message("Reference assembly doesn't exist on disk: " + metadataReference.Display);
                    project = project.RemoveMetadataReference(metadataReference);
                }
            }

            return project.Solution;
        }

        private static Solution AddAssemblyAttributesFile(string language, string outputAssemblyPath, Solution solution)
        {
            if (!File.Exists(outputAssemblyPath))
            {
                Log.Exception("AddAssemblyAttributesFile: assembly doesn't exist: " + outputAssemblyPath);
                return solution;
            }

            var assemblyAttributesFileText = MetadataReading.GetAssemblyAttributesFileText(
                assemblyFilePath: outputAssemblyPath,
                language: language);
            if (assemblyAttributesFileText != null)
            {
                var extension = language == LanguageNames.CSharp ? ".cs" : ".vb";
                var newAssemblyAttributesDocumentName = MetadataAsSource.GeneratedAssemblyAttributesFileName + extension;
                var existingAssemblyAttributesFileName = "AssemblyAttributes" + extension;

                var project = solution.Projects.First();
                if (project.Documents.All(d => d.Name != existingAssemblyAttributesFileName || d.Folders.Count != 0))
                {
                    var document = project.AddDocument(
                        newAssemblyAttributesDocumentName,
                        assemblyAttributesFileText,
                        filePath: newAssemblyAttributesDocumentName);
                    solution = document.Project.Solution;
                }
            }

            return solution;
        }

        public static string CurrentAssemblyName = null;

        public IEnumerable<Project> Projects => solution?.Projects;
        
        public IDictionary<string, ProjectData> MSBProjects { get; set; }
        public CodeAnalysis.Host.HostServices Host {get; set;}

        public Project GetProject(Solution solution, ProjectId id) 
        {
            return solution.GetProject(id);
        }

        /// <returns>true if only part of the solution was processed and the method needs to be called again, false if all done</returns>
        public bool Generate(
            HashSet<string> processedAssemblyList = null, Folder<Project> solutionExplorerRoot = null, 
            IEnumerable<Project> slnProjects = null)
        {
            if (solution == null)
            {
                // we failed to open the solution earlier; just return
                Log.Message("Solution is null: " + this.ProjectFilePath);
                return false;
            }

            var allProjects = (slnProjects ?? this.Projects).ToArray();
            if (allProjects.Length == 0)
            {
                // Second chance:
                Log.Exception("Solution " + this.ProjectFilePath + " has 0 projects - this is suspicious");

                var projects = new List<Project>();
                Parse(this.SolutionFilePath, null);
                // parser.GetAwaiter();

                var host = Host;
                var adhoc = new AdhocWorkspace(Host);

                foreach(var item in MSBProjects)
                {
                    var csproj = item.Key;
                    // new ProjectData { Info = info, Id = projectId.Id, ProjectId = ProjectId }
                    ProjectData data = item.Value;
                    ProjectId id = data.ProjectId;
                    var info = data.Info;

                    var project = solution.GetProject(id);
                    if (project == null)
                    {
                        var name = Path.GetFileName(csproj);
                        project = adhoc.AddProject(csproj, "C#");
                    }
                    if (project != null)
                        projects.Add(project);
                }
                
                allProjects = projects.ToArray();
            }

            var projectsToProcess = allProjects
                .Where(p => processedAssemblyList == null || !processedAssemblyList.Contains(p.AssemblyName))
                .ToArray();

            var currentBatch = projectsToProcess
                .ToArray();
            foreach (var project in currentBatch)
            {
                try
                {
                    CurrentAssemblyName = project.AssemblyName;

                    var generator = new ProjectGenerator(this, project);
                    generator.Generate().GetAwaiter().GetResult();

                    File.AppendAllText(Paths.ProcessedAssemblies, project.AssemblyName + Environment.NewLine, Encoding.UTF8);
                    if (processedAssemblyList != null)
                    {
                        processedAssemblyList.Add(project.AssemblyName);
                    }
                }
                finally
                {
                    CurrentAssemblyName = null;
                }
            }

            new TypeScriptSupport().Generate(typeScriptFiles, SolutionDestinationFolder);

            if (currentBatch.Length > 1)
            {
                AddProjectsToSolutionExplorer(
                    solutionExplorerRoot,
                    currentBatch);
            }

            return currentBatch.Length < projectsToProcess.Length;
        }

        private void SetFieldValue(object instance, string fieldName, object value)
        {
            var type = instance.GetType();
            var fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(instance, null);
        }

        public void GenerateExternalReferences(HashSet<string> assemblyList)
        {
            var externalReferences = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var project in solution.Projects)
            {
                var references = project.MetadataReferences
                    .OfType<PortableExecutableReference>()
                    .Where(m => File.Exists(m.FilePath))
                    .Where(m => !assemblyList.Contains(Path.GetFileNameWithoutExtension(m.FilePath)))
                    .Where(m => !IsPartOfSolution(Path.GetFileNameWithoutExtension(m.FilePath)))
                    .Where(m => GetExternalAssemblyIndex(Path.GetFileNameWithoutExtension(m.FilePath)) == -1)
                    .Select(m => Path.GetFullPath(m.FilePath));
                foreach (var reference in references)
                {
                    externalReferences[Path.GetFileNameWithoutExtension(reference)] = reference;
                }
            }

            foreach (var externalReference in externalReferences)
            {
                Log.Write(externalReference.Key, ConsoleColor.Magenta);
                var solutionGenerator = new SolutionGenerator(
                    externalReference.Value,
                    Paths.SolutionDestinationFolder,
                    pluginBlacklist: PluginBlacklist);
                solutionGenerator.Generate(assemblyList);
            }
        }

        public bool IsPartOfSolution(string assemblyName)
        {
            if (GlobalAssemblyList == null)
            {
                // if for some reason we don't know a global list, assume everything is in the solution
                // this is better than the alternative
                return true;
            }

            return GlobalAssemblyList.Contains(assemblyName);
        }

        public int GetExternalAssemblyIndex(string assemblyName)
        {
            if (Federation == null)
            {
                return -1;
            }

            return Federation.GetExternalAssemblyIndex(assemblyName);
        }

        public string SolutionFilePath {get; set;}

        private Solution CreateSolution(string solutionFilePath, ImmutableDictionary<string, string> properties = null)
        {
            try
            {
                Solution solution = null;
                if (solutionFilePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    properties = AddSolutionProperties(properties, solutionFilePath);
                    var workspace = CreateWorkspace(properties);
                    workspace.SkipUnrecognizedProjects = true;

                    workspace.WorkspaceFailed += WorkspaceFailed;
                    SolutionFilePath = solutionFilePath;
                    
                    try {
                        //  public Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken = default(CancellationToken));
                        solution = workspace.OpenSolutionAsync(solutionFilePath)
                            .GetAwaiter().GetResult();

                        Parse(solutionFilePath, workspace);

                    } catch (Exception ex) 
                    {
                        Console.WriteLine($"MS CodeAnalysis Workspace.Desktop failed {ex}");
                        Console.WriteLine(Environment.StackTrace);
                    }

                    this.workspace = workspace ?? this.workspace;
                }
                else if (
                    solutionFilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    solutionFilePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                {
                    var workspace = CreateWorkspace(properties);
                    workspace.WorkspaceFailed += WorkspaceFailed;
                    solution = workspace.OpenProjectAsync(solutionFilePath).GetAwaiter().GetResult().Solution;
                    this.workspace = workspace;
                }
                else if (
                    solutionFilePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    solutionFilePath.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase) ||
                    solutionFilePath.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase))
                {
                    solution = MetadataAsSource.LoadMetadataAsSourceSolution(solutionFilePath);
                    if (solution != null)
                    {
                        solution.Workspace.WorkspaceFailed += WorkspaceFailed;
                        workspace = solution.Workspace;
                    }
                }

                if (solution == null)
                {
                    return null;
                }

                SolutionFilePath = solutionFilePath;

                return solution;
            }
            catch (Exception ex)
            {
                Errors = ex.InnerException ?? ex;
                Log.Exception(ex, "Failed to open solution: " + solutionFilePath);
                return null;
            }
        }

        // http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces.Desktop/Workspace/MSBuild/MSBuildProjectLoader.cs,bb1d492b86f517b4,references
        // using MSB = Microsoft.Build;

        // public IReadOnlyList<ProjectInSolution> ProjectsInOrder { get; }
        public void // async Task<object> 
            Parse(string sln, MSBuildWorkspace workspace)
        {
            sln = sln ?? SolutionFilePath;
            workspace = workspace ?? this.MSBWorkspace;

            Host = WorkspaceHacks.Pack as CodeAnalysis.Host.HostServices;

            MSBProjects  = new Dictionary<string, ProjectData>();

            var absoluteSolutionPath = Path.GetFullPath(sln);
            var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(absoluteSolutionPath)
                as Microsoft.Build.Construction.SolutionFile;
            //  var reportMode = ReportMode.Log; // this.SkipUnrecognizedProjects ? ReportMode.Log : ReportMode.Throw;
            var cancellationToken = CancellationToken.None;
 
            // a list to accumulate all the loaded projects
            var loadedProjects = new LoadState(null);
            
            // load all the projects
            foreach (var project in solutionFile.ProjectsInOrder)
            {
                // cancellationToken.ThrowIfCancellationRequested();
 
                if (project.ProjectType != Microsoft.Build.Construction.SolutionProjectType.SolutionFolder)
                {
                    var projectAbsolutePath = project.AbsolutePath; //TryGetAbsolutePath(project.AbsolutePath, reportMode);
                    if (PlatformInformation.IsUnix)
                        projectAbsolutePath = projectAbsolutePath.Replace('\\', Path.DirectorySeparatorChar);

                    if (projectAbsolutePath != null)
                    {
                        /*
                        if (LoadState.TryGetLoaderFromProjectPath(workspace,  projectAbsolutePath, reportMode, out var loader))
                        {
                            // projects get added to 'loadedProjects' as side-effect
                            // never prefer metadata when loading solution, all projects get loaded if they can.
                            var projectFilePath = projectAbsolutePath;

                            // GetOrLoadProjectAsync
                            var projectId = LoadState.GetProjectId(loadedProjects, projectFilePath);
    
                            var tmp = // await 
                                LoadState.GetOrLoadProjectAsync(workspace, 
                                    projectAbsolutePath,
                                    loader, preferMetadata: false, loadedProjects: loadedProjects, 
                                    cancellationToken: cancellationToken); // .ConfigureAwait(false);

                            this.MSBProjects.Add(projectAbsolutePath, projectId);
                        }
                        */

                        var csproj = projectAbsolutePath;
 
                        var loaderWS = new MSBuildProjectLoader(workspace);
                        var infos = loaderWS.LoadProjectInfoAsync(csproj).Result;

                        ProjectInfo info = infos.FirstOrDefault();
                        var projectId = info.Id; //  LoadState.GetProjectId(loadedProjects, projectAbsolutePath);
                        this.MSBProjects.Add(projectAbsolutePath,
                            new ProjectData {
                                Name = Path.GetFileNameWithoutExtension(csproj),
                                Info = info, Id = projectId.Id.ToString(), ProjectId = projectId }
                         );
                    }
                }
            }
        }

        public class ProjectData
        {
            public string Name {get;set;}
            public string Id {get; set;}
            public ProjectId ProjectId {get; set;}
            public ProjectInfo Info {get; set;}
        }

        private ImmutableDictionary<string, string> AddSolutionProperties(ImmutableDictionary<string, string> properties, string solutionFilePath)
        {
            // http://referencesource.microsoft.com/#MSBuildFiles/C/ProgramFiles(x86)/MSBuild/14.0/bin_/amd64/Microsoft.Common.CurrentVersion.targets,296
            properties = properties ?? ImmutableDictionary<string, string>.Empty;
            properties = properties.Add("SolutionName", Path.GetFileNameWithoutExtension(solutionFilePath));
            properties = properties.Add("SolutionFileName", Path.GetFileName(solutionFilePath));
            properties = properties.Add("SolutionPath", solutionFilePath);
            properties = properties.Add("SolutionDir", Path.GetDirectoryName(solutionFilePath));
            properties = properties.Add("SolutionExt", Path.GetExtension(solutionFilePath));
            return properties;
        }

        private static void WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            var message = e.Diagnostic.Message;
            if (message.StartsWith("Could not find file") || message.StartsWith("Could not find a part of the path"))
            {
                return;
            }

            if (message.StartsWith("The imported project "))
            {
                return;
            }

            if (message.Contains("because the file extension '.shproj'"))
            {
                return;
            }

            var project = ((Workspace)sender).CurrentSolution.Projects.FirstOrDefault();
            if (project != null)
            {
                message = message + " Project: " + project.Name;
            }

            Log.Exception("Workspace failed: " + message);
            Log.Write(Environment.StackTrace, ConsoleColor.Red);

            Log.Write(message, ConsoleColor.Red);
        }

        public void AddTypeScriptFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            filePath = Path.GetFullPath(filePath);
            this.typeScriptFiles.Add(filePath);
        }

        public void Dispose()
        {
            if (workspace != null)
            {
                workspace.Dispose();
                workspace = null;
            }
        }
    }
}
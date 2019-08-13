using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.SourceBrowser.Common;
using Roslyn.Utilities;

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

        /// <summary>
        /// List of all assembly names included in the index, from all solutions
        /// </summary>
        public HashSet<string> GlobalAssemblyList { get; set; }

        private Solution solution;
        private Workspace workspace;

        public SolutionGenerator(
            string solutionFilePath,
            string solutionDestinationFolder,
            string serverPath = null,
            ImmutableDictionary<string, string> properties = null,
            Federation federation = null,
            IReadOnlyDictionary<string, string> serverPathMappings = null,
            IEnumerable<string> pluginBlacklist = null,
            bool doNotIncludeReferencedProjects = false)
        {
            this.SolutionSourceFolder = Path.GetDirectoryName(solutionFilePath);
            this.SolutionDestinationFolder = solutionDestinationFolder;
            this.ProjectFilePath = solutionFilePath;
            this.ServerPath = serverPath;
            ServerPathMappings = serverPathMappings;
            this.solution = CreateSolution(solutionFilePath, properties, doNotIncludeReferencedProjects);
            this.Federation = federation ?? new Federation();
            this.PluginBlacklist = pluginBlacklist ?? Enumerable.Empty<string>();

            if (LoadPlugins) {
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
            try {
                PluginAggregator = new MEF.PluginAggregator(configs, new Utilities.PluginLogger(), PluginBlacklist);
                if (PluginAggregator != null)
                    FirstChanceExceptionHandler.IgnoreModules(PluginAggregator.Select(p => p.PluginModule));
                PluginAggregator?.Init();
            }
            catch (Exception ex) { 
                LastError = ex; // no fatal error
            }
        }

        public static Exception LastError { get; set; }
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

            this.solution = CreateSolution(
                commandLineArguments,
                projectName,
                language,
                projectSourceFolder,
                outputAssemblyPath);
        }

        public IEnumerable<string> GetAssemblyNames()
        {
            if (solution != null) {
                return solution.Projects.Select(p => p.AssemblyName);
            } else {
                return Enumerable.Empty<string>();
            }
        }

        private static MSBuildWorkspace CreateWorkspace(ImmutableDictionary<string, string> propertiesOpt = null)
        {
            propertiesOpt = propertiesOpt ?? ImmutableDictionary<string, string>.Empty;

            // Explicitly add "CheckForSystemRuntimeDependency = true" property to correctly resolve facade references.
            // See https://github.com/dotnet/roslyn/issues/560
            propertiesOpt = propertiesOpt.Add("CheckForSystemRuntimeDependency", "true");
            propertiesOpt = propertiesOpt.Add("VisualStudioVersion", "15.0");
            propertiesOpt = propertiesOpt.Add("AlwaysCompileMarkupFilesInSeparateDomain", "false");

            var w = MSBuildWorkspace.Create(properties: propertiesOpt);
            w.LoadMetadataForReferencedProjects = true;
            w.AssociateFileExtensionWithLanguage("depproj", LanguageNames.CSharp);
            return w;
        }

        private static Solution CreateSolution(
            string commandLineArguments,
            string projectName,
            string language,
            string projectSourceFolder,
            string outputAssemblyPath)
        {
            var workspace = CreateWorkspace();

            // microsoft.codeanalysis.workspaces.common\3.0.0
            Microsoft.CodeAnalysis.ProjectInfo projectInfo = null;

            projectInfo = CommandLineProject.CreateProjectInfo(
                projectName,
                language,
                commandLineArguments,
                projectSourceFolder,
                workspace);

            var solution = workspace.CurrentSolution.AddProject(projectInfo);

            solution = RemoveNonExistingFiles(solution);
            solution = AddAssemblyAttributesFile(language, outputAssemblyPath, solution);
            solution = DisambiguateSameNameLinkedFiles(solution);
            solution = DeduplicateProjectReferences(solution);

            solution.Workspace.WorkspaceFailed += WorkspaceFailed;

            return solution;
        }

        private static Solution DisambiguateSameNameLinkedFiles(Solution solution)
        {
            foreach (var projectId in solution.ProjectIds.ToArray()) {
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
            foreach (var conflictedItemGroup in nameMap.Where(g => g.Count() > 1)) {
                foreach (var conflictedDocument in conflictedItemGroup) {
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
            foreach (var projectId in solution.ProjectIds.ToArray()) {
                var project = solution.GetProject(projectId);
                solution = RemoveNonExistingDocuments(project);

                project = solution.GetProject(projectId);
                solution = RemoveNonExistingReferences(project);
            }

            return solution;
        }

        private static Solution RemoveNonExistingDocuments(Project project)
        {
            foreach (var documentId in project.DocumentIds.ToArray()) {
                var document = project.GetDocument(documentId);
                if (!File.Exists(document.FilePath)) {
                    Log.Message("Document doesn't exist on disk: " + document.FilePath);
                    project = project.RemoveDocument(documentId);
                }
            }

            return project.Solution;
        }

        private static Solution RemoveNonExistingReferences(Project project)
        {
            foreach (var metadataReference in project.MetadataReferences.ToArray()) {
                if (!File.Exists(metadataReference.Display)) {
                    Log.Message("Reference assembly doesn't exist on disk: " + metadataReference.Display);
                    project = project.RemoveMetadataReference(metadataReference);
                }
            }

            return project.Solution;
        }

        private static Solution AddAssemblyAttributesFile(string language, string outputAssemblyPath, Solution solution)
        {
            if (!File.Exists(outputAssemblyPath)) {
                Log.Exception("AddAssemblyAttributesFile: assembly doesn't exist: " + outputAssemblyPath);
                return solution;
            }

            var assemblyAttributesFileText = MetadataReading.GetAssemblyAttributesFileText(
                assemblyFilePath: outputAssemblyPath,
                language: language);
            if (assemblyAttributesFileText != null) {
                var extension = language == LanguageNames.CSharp ? ".cs" : ".vb";
                var newAssemblyAttributesDocumentName = MetadataAsSource.GeneratedAssemblyAttributesFileName + extension;
                var existingAssemblyAttributesFileName = "AssemblyAttributes" + extension;

                var project = solution.Projects.First();
                if (project.Documents.All(d => d.Name != existingAssemblyAttributesFileName || d.Folders.Count != 0)) {
                    var document = project.AddDocument(
                        newAssemblyAttributesDocumentName,
                        assemblyAttributesFileText,
                        filePath: newAssemblyAttributesDocumentName);
                    solution = document.Project.Solution;
                }
            }

            return solution;
        }

        private static Solution DeduplicateProjectReferences(Solution solution)
        {
            foreach (var projectId in solution.ProjectIds.ToArray()) {
                var project = solution.GetProject(projectId);

                var distinctProjectReferences = project.AllProjectReferences.Distinct().ToArray();
                if (distinctProjectReferences.Length < project.AllProjectReferences.Count) {
                    var duplicates = project.AllProjectReferences.GroupBy(p => p).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
                    foreach (var duplicate in duplicates) {
                        Log.Write($"Duplicate project reference to {duplicate.ProjectId.ToString()} in project: {project.Name}", ConsoleColor.Yellow);
                    }

                    var newProject = project.WithProjectReferences(distinctProjectReferences);
                    solution = newProject.Solution;
                }
            }

            return solution;
        }

        public static string CurrentAssemblyName = null;

        /// <returns>true if only part of the solution was processed and the method needs to be called again, false if all done</returns>
        public bool Generate(HashSet<string> processedAssemblyList = null, Folder<ProjectSkeleton> solutionExplorerRoot = null)
        {
            if (solution == null) {
                // we failed to open the solution earlier; just return
                Log.Message("Solution is null: " + this.ProjectFilePath);
                return false;
            }

            var allProjects = solution.Projects.ToArray();
            if (allProjects.Length == 0) {
                Log.Exception("Solution " + this.ProjectFilePath + " has 0 projects - this is suspicious");
            }

            var projectsToProcess = allProjects
                .Where(p => processedAssemblyList == null || processedAssemblyList.Add(p.AssemblyName))
                .ToArray();
            var currentBatch = projectsToProcess
                .ToArray();
            foreach (var project in currentBatch) {
                try {
                    CurrentAssemblyName = project.AssemblyName;

                    var generator = new ProjectGenerator(this, project);
                    generator.Generate().GetAwaiter().GetResult();

                    File.AppendAllText(Paths.ProcessedAssemblies, project.AssemblyName + Environment.NewLine, Encoding.UTF8);
                }
                finally {
                    CurrentAssemblyName = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }

            new TypeScriptSupport().Generate(typeScriptFiles, SolutionDestinationFolder);

            AddProjectsToSolutionExplorer(
                solutionExplorerRoot,
                currentBatch);

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

            foreach (var project in solution.Projects) {
                var references = project.MetadataReferences
                    .OfType<PortableExecutableReference>()
                    .Where(m => File.Exists(m.FilePath) &&
                                !assemblyList.Contains(Path.GetFileNameWithoutExtension(m.FilePath)) &&
                                !IsPartOfSolution(Path.GetFileNameWithoutExtension(m.FilePath)) &&
                                GetExternalAssemblyIndex(Path.GetFileNameWithoutExtension(m.FilePath)) == -1
                    )
                    .Select(m => Path.GetFullPath(m.FilePath));
                foreach (var reference in references) {
                    externalReferences[Path.GetFileNameWithoutExtension(reference)] = reference;
                }
            }

            foreach (var externalReference in externalReferences) {
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
            if (GlobalAssemblyList == null) {
                // if for some reason we don't know a global list, assume everything is in the solution
                // this is better than the alternative
                return true;
            }

            return GlobalAssemblyList.Contains(assemblyName);
        }

        public int GetExternalAssemblyIndex(string assemblyName)
        {
            if (Federation == null) {
                return -1;
            }

            return Federation.GetExternalAssemblyIndex(assemblyName);
        }

        private Solution CreateSolution(string solutionFilePath, ImmutableDictionary<string, string> properties = null, bool doNotIncludeReferencedProjects = false)
        {
            try {
                Solution solution = null;
                if (solutionFilePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)) {
                    properties = AddSolutionProperties(properties, solutionFilePath);
                    var workspace = CreateWorkspace(properties);
                    workspace.SkipUnrecognizedProjects = true;
                    workspace.WorkspaceFailed += WorkspaceFailed;
                    solution = workspace.OpenSolutionAsync(solutionFilePath).GetAwaiter().GetResult();
                    solution = DeduplicateProjectReferences(solution);
                    this.workspace = workspace;
                } else if (
                      solutionFilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                      solutionFilePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase)) {
                    var workspace = CreateWorkspace(properties);
                    workspace.WorkspaceFailed += WorkspaceFailed;
                    solution = workspace.OpenProjectAsync(solutionFilePath).GetAwaiter().GetResult().Solution;
                    solution = DeduplicateProjectReferences(solution);
                    if (doNotIncludeReferencedProjects) {
                        var keepPrimaryProject = solution.Projects.First(p => string.Equals(p.FilePath, solutionFilePath, StringComparison.OrdinalIgnoreCase));
                        foreach (var projectIdToRemove in solution.ProjectIds.Where(id => id != keepPrimaryProject.Id).ToArray()) {
                            solution = solution.RemoveProject(projectIdToRemove);
                        }
                    }

                    this.workspace = workspace;
                } else if (
                      solutionFilePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                      solutionFilePath.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase) ||
                      solutionFilePath.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase)) {
                    solution = MetadataAsSource.LoadMetadataAsSourceSolution(solutionFilePath);
                    if (solution != null) {
                        solution.Workspace.WorkspaceFailed += WorkspaceFailed;
                        workspace = solution.Workspace;
                    }
                }

                return solution;
            }
            catch (Exception ex) {
                Log.Exception(ex, "Failed to open solution: " + solutionFilePath);
                return null;
            }
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
            if (message.StartsWith("Could not find file") || message.StartsWith("Could not find a part of the path")) {
                return;
            }

            if (message.StartsWith("The imported project ")) {
                return;
            }

            if (message.Contains("because the file extension '.shproj'")) {
                return;
            }

            var project = ((Workspace)sender).CurrentSolution.Projects.FirstOrDefault();
            if (project != null) {
                message = message + " Project: " + project.Name;
            }

            Log.Exception("Workspace failed: " + message);
            Log.Write(message, ConsoleColor.Red);
        }

        public void AddTypeScriptFile(string filePath)
        {
            if (!File.Exists(filePath)) {
                return;
            }

            filePath = Path.GetFullPath(filePath);
            this.typeScriptFiles.Add(filePath);
        }

        public void Dispose()
        {
            if (workspace != null) {
                workspace.Dispose();
                workspace = null;
            }
        }
    }

}

namespace Microsoft.CodeAnalysis
{
    public class CommandLineProject
    {

        public static ProjectInfo CreateProjectInfo(string projectName, string language,
            // IEnumerable<string> commandLine
            string Args,
            string projectDirectory, Workspace workspace = null)
        {
            IEnumerable<string> commandLineArgs = Args.Split(new char[] { ' ' });

            // TODO (tomat): the method may throw all sorts of exceptions.
            var tmpWorkspace = workspace ?? new AdhocWorkspace(DesktopMefHostServices.DefaultServices);
            var languageServices = tmpWorkspace.Services.GetLanguageServices(language);
            if (languageServices == null) {
                throw new ArgumentException("Unrecognized_language_name");
            }

            var commandLineParser = languageServices.GetRequiredService<ICommandLineParserService>();
            var commandLineArguments = commandLineParser.Parse(commandLineArgs, projectDirectory,
                isInteractive: false, sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

            //var metadataService = tmpWorkspace.Services.GetRequiredService<IMetadataService>();

            //// we only support file paths in /r command line arguments
            //var relativePathResolver =
            //    new RelativePathResolver(commandLineArguments.ReferencePaths, commandLineArguments.BaseDirectory);
            //var commandLineMetadataReferenceResolver = new WorkspaceMetadataFileReferenceResolver(
            //    metadataService, relativePathResolver);

            var analyzerLoader = tmpWorkspace.Services.GetRequiredService<IAnalyzerService>().GetLoader();
            //var xmlFileResolver = new XmlFileResolver(commandLineArguments.BaseDirectory);
            //var strongNameProvider = new DesktopStrongNameProvider(commandLineArguments.KeyFileSearchPaths);

            //// resolve all metadata references.
            //var boundMetadataReferences = commandLineArguments.ResolveMetadataReferences(commandLineMetadataReferenceResolver);
            //var unresolvedMetadataReferences = boundMetadataReferences.FirstOrDefault(r => r is UnresolvedMetadataReference);
            //if (unresolvedMetadataReferences != null) {
            //    throw new ArgumentException(string.Format(
            //        "Can_t_resolve_metadata_reference_colon_0", ((UnresolvedMetadataReference)unresolvedMetadataReferences).Reference));
            //}

            // resolve all analyzer references.
            //foreach (var path in commandLineArguments.AnalyzerReferences.Select(r => r.FilePath)) {
            //    analyzerLoader.AddDependencyLocation(relativePathResolver.ResolvePath(path, baseFilePath: null));
            //}

            //var boundAnalyzerReferences = commandLineArguments.ResolveAnalyzerReferences(analyzerLoader);
            //var unresolvedAnalyzerReferences = boundAnalyzerReferences.FirstOrDefault(r => r is UnresolvedAnalyzerReference);
            //if (unresolvedAnalyzerReferences != null) {
            //    throw new ArgumentException(string.Format(
            //        "Can_t_resolve_analyzer_reference_colon_0", ((UnresolvedAnalyzerReference)unresolvedAnalyzerReferences).Display));
            //}

            AssemblyIdentityComparer assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            //if (commandLineArguments.AppConfigPath != null) {
            //    try {
            //        using (var appConfigStream = new FileStream(commandLineArguments.AppConfigPath, FileMode.Open, FileAccess.Read)) {
            //            assemblyIdentityComparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfigStream);
            //        }
            //    }
            //    catch (Exception e) {
            //        throw new ArgumentException(string.Format(
            //            "An_error_occurred_while_reading_the_specified_configuration_file_colon_0", e.Message));
            //    }
            //} else {
            //    assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            //}

            var projectId = ProjectId.CreateNewId(debugName: projectName);

            // construct file infos
            var docs = new List<DocumentInfo>();

            //foreach (var fileArg in commandLineArguments.SourceFiles) 
            //{
            //    var absolutePath = Path.IsPathRooted(fileArg.Path) || string.IsNullOrEmpty(projectDirectory)
            //        ? Path.GetFullPath(fileArg.Path)
            //        : Path.GetFullPath(Path.Combine(projectDirectory, fileArg.Path));

            //    var relativePath = PathUtilities.GetRelativePath(projectDirectory, absolutePath);
            //    var isWithinProject = PathUtilities.IsChildPath(projectDirectory, absolutePath);

            //    var folderRoot = isWithinProject ? Path.GetDirectoryName(relativePath) : "";
            //    var folders = isWithinProject ? GetFolders(relativePath) : null;
            //    var name = Path.GetFileName(relativePath);
            //    var id = DocumentId.CreateNewId(projectId, absolutePath);

            //    var doc = DocumentInfo.Create(
            //       id: id,
            //       name: name,
            //       folders: folders,
            //       sourceCodeKind: fileArg.IsScript ? SourceCodeKind.Script : SourceCodeKind.Regular,
            //       loader: new FileTextLoader(absolutePath, commandLineArguments.Encoding),
            //       filePath: absolutePath);

            //    docs.Add(doc);
            //}

            // construct file infos for additional files.
            var additionalDocs = new List<DocumentInfo>();
            //foreach (var fileArg in commandLineArguments.AdditionalFiles) {
            //    var absolutePath = Path.IsPathRooted(fileArg.Path) || string.IsNullOrEmpty(projectDirectory)
            //            ? Path.GetFullPath(fileArg.Path)
            //            : Path.GetFullPath(Path.Combine(projectDirectory, fileArg.Path));

            //    var relativePath = PathUtilities.GetRelativePath(projectDirectory, absolutePath);
            //    var isWithinProject = PathUtilities.IsChildPath(projectDirectory, absolutePath);

            //    var folderRoot = isWithinProject ? Path.GetDirectoryName(relativePath) : "";
            //    var folders = isWithinProject ? GetFolders(relativePath) : null;
            //    var name = Path.GetFileName(relativePath);
            //    var id = DocumentId.CreateNewId(projectId, absolutePath);

            //    var doc = DocumentInfo.Create(
            //       id: id,
            //       name: name,
            //       folders: folders,
            //       sourceCodeKind: SourceCodeKind.Regular,
            //       loader: new FileTextLoader(absolutePath, commandLineArguments.Encoding),
            //       filePath: absolutePath);

            //    additionalDocs.Add(doc);
            //}

            // If /out is not specified and the project is a console app the csc.exe finds out the Main method
            // and names the compilation after the file that contains it. We don't want to create a compilation, 
            // bind Mains etc. here. Besides the msbuild always includes /out in the command line it produces.
            // So if we don't have the /out argument we name the compilation "<anonymous>".

            //string assemblyName = (commandLineArguments.OutputFileName != null) ?
            //    Path.GetFileNameWithoutExtension(commandLineArguments.OutputFileName) : "<anonymous>";
            string assemblyName = "<anonymous>";

            // TODO (tomat): what should be the assemblyName when compiling a netmodule? Should it be /moduleassemblyname

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                projectName,
                assemblyName,
                language: language,
                compilationOptions: commandLineArguments.CompilationOptions
                    //.WithXmlReferenceResolver(xmlFileResolver)
                    //.WithAssemblyIdentityComparer(assemblyIdentityComparer)
                    //.WithStrongNameProvider(strongNameProvider)

                    //// TODO (https://github.com/dotnet/roslyn/issues/4967): 
                    //.WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, 
                    //new RelativePathResolver(ImmutableArray<string>.Empty, projectDirectory)))
                    ,

                parseOptions: commandLineArguments.ParseOptions,
                documents: docs,
                additionalDocuments: additionalDocs,
                metadataReferences: null, // boundMetadataReferences,
                analyzerReferences: null); //  boundAnalyzerReferences);

            return projectInfo;
        }

        private static readonly char[] s_folderSplitters = new char[] { Path.DirectorySeparatorChar };

        private static IList<string> GetFolders(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) {
                return ImmutableArray.Create<string>();
            } else {
                return directory.Split(s_folderSplitters, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            }
        }

    }

}


namespace Microsoft.CodeAnalysis.Host.Mef
{
    public static class DesktopMefHostServices
    {
        private static MefHostServices s_defaultServices;
        public static MefHostServices DefaultServices {
            get {
                if (s_defaultServices == null) {
                    Interlocked.CompareExchange(ref s_defaultServices, MefHostServices.Create(DefaultAssemblies), null);
                }

                return s_defaultServices;
            }
        }

        internal static void ResetHostServicesTestOnly()
        {
            s_defaultServices = null;
        }

        public static ImmutableArray<Assembly> DefaultAssemblies => MefHostServices.DefaultAssemblies;
    }

}


namespace Microsoft.CodeAnalysis.Host
{
    internal interface IMetadataService : IWorkspaceService
    {
        PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties);
    }

    internal sealed class WorkspaceMetadataFileReferenceResolver : MetadataReferenceResolver, IEquatable<WorkspaceMetadataFileReferenceResolver>
    {
        private readonly IMetadataService _metadataService;
        internal readonly RelativePathResolver PathResolver;

        public WorkspaceMetadataFileReferenceResolver(IMetadataService metadataService, RelativePathResolver pathResolver)
        {
            Debug.Assert(metadataService != null);
            Debug.Assert(pathResolver != null);

            _metadataService = metadataService;
            PathResolver = pathResolver;
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            string path = PathResolver.ResolvePath(reference, baseFilePath);
            //if (path == null) {
                return ImmutableArray<PortableExecutableReference>.Empty;
            //}

            //return ImmutableArray.Create(_metadataService.GetReference(path, properties));
        }

        public bool Equals(WorkspaceMetadataFileReferenceResolver other)
        {
            return other != null
                && _metadataService == other._metadataService
                && PathResolver.Equals(other.PathResolver);
        }

        public override int GetHashCode()
        {
            // Hash.Combine
            return HashX.Combine(_metadataService, HashX.Combine(PathResolver, 0));
        }


        public override bool Equals(object other) => Equals(other as WorkspaceMetadataFileReferenceResolver);
    }


}



namespace Microsoft.CodeAnalysis.CSharp
{
    internal interface ICommandLineParserService : ILanguageService
    {
        CommandLineArguments Parse(IEnumerable<string> arguments, string baseDirectory, bool isInteractive, string sdkDirectory);
    }

    [ExportLanguageService(typeof(ICommandLineParserService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpCommandLineParserService : ICommandLineParserService
    {
        public CommandLineArguments Parse(IEnumerable<string> arguments, string baseDirectory, bool isInteractive, string sdkDirectory)
        {
#if SCRIPTING
            var parser = isInteractive ? CSharpCommandLineParser.Interactive : CSharpCommandLineParser.Default;
#else
            var parser = CSharpCommandLineParser.Default;
#endif
            return parser.Parse(arguments, baseDirectory, sdkDirectory);
        }
    }
}



namespace Microsoft.CodeAnalysis.Interop
{
    using System.Security;
    

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891"),
        System.Security.SuppressUnmanagedCodeSecurity]
    internal interface IClrRuntimeInfo
    {
        [PreserveSig]
        int GetVersionString(
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        [PreserveSig]
        int GetRuntimeDirectory(
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsLoaded(
            [In] IntPtr processHandle);

        [PreserveSig]
        int LoadErrorString(
            [In, MarshalAs(UnmanagedType.U4)] int resourceId,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        IntPtr LoadLibrary(
            [In, MarshalAs(UnmanagedType.LPWStr)] string dllName);

        IntPtr GetProcAddress(
            [In, MarshalAs(UnmanagedType.LPStr)] string procName);

        [return: MarshalAs(UnmanagedType.Interface)]
        object GetInterface(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid coClassId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsLoadable();

        void SetDefaultStartupFlags(
            [In, MarshalAs(UnmanagedType.U4)] int startupFlags,
            [In, MarshalAs(UnmanagedType.LPStr)] string hostConfigFile);

        [PreserveSig]
        int GetDefaultStartupFlags(
            [Out, MarshalAs(UnmanagedType.U4)] out int startupFlags,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 2)] StringBuilder hostConfigFile,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int hostConfigFileLength);

        void BindAsLegacyV2Runtime();

        void IsStarted(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool started,
            [Out, MarshalAs(UnmanagedType.U4)] out int startupFlags);
    }


    
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("9FD93CCF-3280-4391-B3A9-96E1CDE77C8D"),
        SuppressUnmanagedCodeSecurity]
    internal interface IClrStrongName
    {
        void GetHashFromAssemblyFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromAssemblyFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromBlob(
            [In] IntPtr pbBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cchBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromFile(
            [In, MarshalAs(UnmanagedType.LPStr)] string pszFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromFileW(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        void GetHashFromHandle(
            [In] IntPtr hFile,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int piHashAlg,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] pbHash,
            [In, MarshalAs(UnmanagedType.U4)] int cchHash,
            [MarshalAs(UnmanagedType.U4)] out int pchHash);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameCompareAssemblies(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly1,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzAssembly2);

        void StrongNameFreeBuffer(
            [In] IntPtr pbMemory);

        void StrongNameGetBlob(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        void StrongNameGetBlobFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbBlob,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int pcbBlob);

        void StrongNameGetPublicKey(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameHashSize(
            [In, MarshalAs(UnmanagedType.U4)] int ulHashAlg);

        void StrongNameKeyDelete(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer);

        void StrongNameKeyGen(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        void StrongNameKeyGenEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags,
            [In, MarshalAs(UnmanagedType.U4)] int dwKeySize,
            out IntPtr ppbKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbKeyBlob);

        void StrongNameKeyInstall(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob);

        void StrongNameSignatureGeneration(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)]byte[] ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob);

        void StrongNameSignatureGenerationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszFilePath,
            [In, MarshalAs(UnmanagedType.LPWStr)] string wszKeyContainer,
            [In] IntPtr pbKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbKeyBlob,
            out IntPtr ppbSignatureBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSignatureBlob,
            [In, MarshalAs(UnmanagedType.U4)] int dwFlags);

        void StrongNameSignatureSize(
            [In] IntPtr pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbSize);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameSignatureVerification(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool StrongNameSignatureVerificationEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            [In, MarshalAs(UnmanagedType.Bool)] bool fForceVerification,
            out IntPtr ptr);

        [return: MarshalAs(UnmanagedType.U4)]
        int StrongNameSignatureVerificationFromImage(
            [In] IntPtr pbBase,
            [In, MarshalAs(UnmanagedType.U4)] int dwLength,
            [In, MarshalAs(UnmanagedType.U4)] int dwInFlags);

        void StrongNameTokenFromAssembly(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);

        void StrongNameTokenFromAssemblyEx(
            [In, MarshalAs(UnmanagedType.LPWStr)] string pwzFilePath,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken,
            out IntPtr ppbPublicKeyBlob,
            [MarshalAs(UnmanagedType.U4)] out int pcbPublicKeyBlob);

        void StrongNameTokenFromPublicKey(
            [In] IntPtr pbPublicKeyBlob,
            [In, MarshalAs(UnmanagedType.U4)] int cbPublicKeyBlob,
            out IntPtr ppbStrongNameToken,
            [MarshalAs(UnmanagedType.U4)] out int pcbStrongNameToken);
    }

    
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D332DB9E-B9B3-4125-8207-A14884F53216"), SuppressUnmanagedCodeSecurity]
    internal interface IClrMetaHost
    {
        [return: MarshalAs(UnmanagedType.Interface)]
        object GetRuntime(
            [In, MarshalAs(UnmanagedType.LPWStr)] string version,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId);

        [PreserveSig]
        int GetVersionFromFile(
            [In, MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer,
            [In, Out, MarshalAs(UnmanagedType.U4)] ref int bufferLength);

        [return: MarshalAs(UnmanagedType.Interface)]
        object EnumerateInstalledRuntimes();

        [return: MarshalAs(UnmanagedType.Interface)]
        object EnumerateLoadedRuntimes(
            [In] IntPtr processHandle);

        // Placeholder for RequestRuntimeLoadedNotification
        [PreserveSig]
        int Reserved01(
            [In] IntPtr reserved1);

        [return: MarshalAs(UnmanagedType.Interface)]
        object QueryLegacyV2RuntimeBinding(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId);

        void ExitProcess(
            [In, MarshalAs(UnmanagedType.U4)] int exitCode);
    }

    internal static class ClrStrongName
    {
        [DllImport("mscoree.dll", PreserveSig = false, EntryPoint = "CLRCreateInstance")]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object nCreateInterface(
                [MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
                [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        internal static IClrStrongName GetInstance()
        {
            var metaHostClsid = new Guid(unchecked((int)0x9280188D), 0xE8E, 0x4867, 0xB3, 0xC, 0x7F, 0xA8, 0x38, 0x84, 0xE8, 0xDE);
            var metaHostGuid = new Guid(unchecked((int)0xD332DB9E), unchecked((short)0xB9B3), 0x4125, 0x82, 0x07, 0xA1, 0x48, 0x84, 0xF5, 0x32, 0x16);
            var clrStrongNameClsid = new Guid(unchecked((int)0xB79B0ACD), unchecked((short)0xF5CD), 0x409b, 0xB5, 0xA5, 0xA1, 0x62, 0x44, 0x61, 0x0B, 0x92);
            var clrRuntimeInfoGuid = new Guid(unchecked((int)0xBD39D1D2), unchecked((short)0xBA2F), 0x486A, 0x89, 0xB0, 0xB4, 0xB0, 0xCB, 0x46, 0x68, 0x91);
            var clrStrongNameGuid = new Guid(unchecked((int)0x9FD93CCF), 0x3280, 0x4391, 0xB3, 0xA9, 0x96, 0xE1, 0xCD, 0xE7, 0x7C, 0x8D);

            var metaHost = (IClrMetaHost)nCreateInterface(metaHostClsid, metaHostGuid);
            var runtime = (IClrRuntimeInfo)metaHost.GetRuntime(GetRuntimeVersion(), clrRuntimeInfoGuid);
            return (IClrStrongName)runtime.GetInterface(clrStrongNameClsid, clrStrongNameGuid);
        }

        internal static string GetRuntimeVersion()
        {
            // When running in a complus environment we must respect the specified CLR version.  This 
            // important to keeping internal builds running. 
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPLUS_InstallRoot")))
            {
                var version = Environment.GetEnvironmentVariable("COMPLUS_Version");
                if (!string.IsNullOrEmpty(version))
                {
                    return version;
                }
            }

            return "v4.0.30319";
        }
    }
}


namespace Microsoft.CodeAnalysis
{
    internal class RelativePathResolver : IEquatable<RelativePathResolver>
    {
        public ImmutableArray<string> SearchPaths { get; }
        public string BaseDirectory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativePathResolver"/> class.
        /// </summary>
        /// <param name="searchPaths">An ordered set of fully qualified 
        /// paths which are searched when resolving assembly names.</param>
        /// <param name="baseDirectory">Directory used when resolving relative paths.</param>
        public RelativePathResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            Debug.Assert(searchPaths.All(PathUtilities.IsAbsolute));
            Debug.Assert(baseDirectory == null || PathUtilities.GetPathKind(baseDirectory) == Roslyn.Utilities.PathKind.Absolute);

            SearchPaths = searchPaths;
            BaseDirectory = baseDirectory;
        }

        public string ResolvePath(string reference, string baseFilePath)
        {
            string resolvedPath = FileUtilities.ResolveRelativePath(reference, baseFilePath, BaseDirectory, SearchPaths, FileExists);
            if (resolvedPath == null)
            {
                return null;
            }

            return FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
        }

        protected virtual bool FileExists(string fullPath)
        {
            Debug.Assert(fullPath != null);
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            return File.Exists(fullPath);
        }

        public RelativePathResolver WithSearchPaths(ImmutableArray<string> searchPaths) =>
            new RelativePathResolver(searchPaths, BaseDirectory);

        public RelativePathResolver WithBaseDirectory(string baseDirectory) =>
            new RelativePathResolver(SearchPaths, baseDirectory);

        public bool Equals(RelativePathResolver other) =>
            BaseDirectory == other.BaseDirectory && SearchPaths.SequenceEqual(other.SearchPaths);

        public override int GetHashCode() =>
            HashX.Combine(BaseDirectory, HashX.CombineValues(SearchPaths));

        public override bool Equals(object obj) => Equals(obj as RelativePathResolver);
    }

    internal class HashX
    {

        internal static int Combine<T>(T newKeyPart, int currentKey) where T : class
        {
            int hash = unchecked(currentKey * (int)0xA5555529);

            if (newKeyPart != null) {
                return unchecked(hash + newKeyPart.GetHashCode());
            }

            return hash;
        }

        internal static int CombineValues<T>(IEnumerable<T> values, 
            int maxItemsToHash = int.MaxValue)
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
                    hashCode = HashX.Combine(value.GetHashCode(), hashCode);
                }
            }

            return hashCode;
        }

        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }
    }
}


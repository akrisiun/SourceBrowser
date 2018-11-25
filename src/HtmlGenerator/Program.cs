using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Reflection;
using Console = SourceBrowser.DebugConsole;

// here non ms hacks
namespace SourceBrowser
{
    public class DebugConsole {

        private const int MY_CODE_PAGE = 437;
        public static void Redirect()
        {
            var outStream = new MemoryStream();
            var encoding = System.Text.Encoding.GetEncoding(MY_CODE_PAGE);
            StreamWriter standardOutput = new StreamWriter(outStream, encoding);

            standardOutput.AutoFlush = true;
            global::System.Console.SetOut(standardOutput);
        }

        public static void WriteLine(string str, params object[] args) {

            global::System.Console.WriteLine(str);
            if (ProgramLoad.isDebug) {
                Debugger.Log(0, "", str);
            }
        }

        public static ConsoleKeyInfo ReadKey()
        {
            if (ProgramLoad.isDebug) {
                return new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false);
            }
            return global::System.Console.ReadKey();
        }
    }

    public class ProgramLoad
    {
        public static string BasePath { get; set; }
        public static List<Assembly> AsmList { get; set; }

        [STAThread]
        public static void Main(string[] args)
        {
            AsmList = new List<Assembly>();
            if (args.Length == 0) {
                PrintUsage();
                return;
            }

            var argList = String.Join(" ", args);
            if (argList.Contains("-debug") || Debugger.IsAttached) {
                isDebug = true;
            }

            // .dll's
            BasePath = BasePath ?? AppDomain.CurrentDomain.BaseDirectory;

            // System.AppContext, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            // System.Runtime, Version=4.1.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            // System.Collections.Immutable, Version=1.2.3.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            LoadDll("System.AppContext");
            LoadDll("System.Runtime"); // , Version=4.2.1.0,
            LoadDll("System.Collections.Immutable"); // , Version=1.5.0

            // InvalidOperationException: Microsoft.Build.Locator.MSBuildLocator.RegisterInstance was called, but MSBuild assemblies were already loaded.
            // LoadDll("Microsoft.Build");
            LoadDll("MEF");
            LoadDll("Microsoft.SourceBrowser.Common");
            LoadDll("Microsoft.SourceBrowser.BuildLogParser");

            Run(args);
        }

        public static void LoadDll(string name)
        {
            var dll = Path.Combine(BasePath, name + ".dll");
            if (!File.Exists(dll)) {
                Console.WriteLine($"no dll {dll}");
            }
            Assembly asm = null;
            try {
                asm = Assembly.LoadFile(dll);
            }
            catch (Exception ex) {
                Console.WriteLine($"Fails {dll} : {ex.InnerException ?? ex}");
            }
            if (asm != null) {
                if (isDebug)
                    Console.WriteLine($"asm: {asm}");

                AsmList.Add(asm);
            }
        }

        public static void Run(string[] args)
        {
            Microsoft.SourceBrowser.HtmlGenerator.Program.Run(args);
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: HtmlGenerator "
                + "[/out:<outputdirectory>] "
                + "[/force] "
                + "[/noplugins] "
                + "[/noplugin:Git] "
                + "<pathtosolution1.csproj|vbproj|sln> [more solutions/projects..] "
                + "[/in:<filecontaingprojectlist>] "
                + "[/nobuiltinfederations] "
                + "[/offlinefederation:server=assemblyListFile] "
                + "[/assemblylist]");
        }

        public static bool isDebug { get; set; }

    }
}

// Origin Microsoft source here
namespace Microsoft.SourceBrowser.HtmlGenerator
{
    using SourceBrowser;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis;
    using Microsoft.SourceBrowser.Common;
    using global::SourceBrowser;

    public class Program
    {
        public static void Run(string[] args)
        {
            var projects = new List<string>();
            var properties = new Dictionary<string, string>();
            var emitAssemblyList = false;
            var force = false;
            var noBuiltInFederations = false;
            var offlineFederations = new Dictionary<string, string>();
            var federations = new HashSet<string>();
            var serverPathMappings = new Dictionary<string, string>();
            var pluginBlacklist = new List<string>();
            var isDebug = ProgramLoad.isDebug;
            var argBefore = "";

            #region Parse args

            foreach (var arg in args) {
                if (arg.StartsWith("/out:")) {
                    Paths.SolutionDestinationFolder = Path.GetFullPath(arg.Substring("/out:".Length).StripQuotes());
                    continue;
                }
                if (arg.StartsWith("-o")) {
                    argBefore = arg;
                    continue;
                }
                if (argBefore.StartsWith("-o")) {
                    Paths.SolutionDestinationFolder = Path.GetFullPath(arg).StripQuotes();
                    continue;
                }
                argBefore = arg;
                if (arg.StartsWith("/debug") || arg.StartsWith("-debug")) {
                    isDebug = true;
                    continue;
                }

                if (arg.StartsWith("/serverPath:")) {
                    var mapping = arg.Substring("/serverPath:".Length).StripQuotes();
                    var parts = mapping.Split('=');
                    if (parts.Length != 2) {
                        Log.Write($"Invalid Server Path: '{mapping}'", ConsoleColor.Red);
                        continue;
                    }
                    serverPathMappings.Add(Path.GetFullPath(parts[0]), parts[1]);
                    continue;
                }

                if (arg == "/force") {
                    force = true;
                    continue;
                }

                if (arg.StartsWith("/in:")) {
                    string inputPath = arg.Substring("/in:".Length).StripQuotes();
                    try {
                        if (!File.Exists(inputPath)) {
                            continue;
                        }

                        string[] paths = File.ReadAllLines(inputPath);
                        foreach (string path in paths) {
                            AddProject(projects, path);
                        }
                    }
                    catch {
                        Log.Write("Invalid argument: " + arg, ConsoleColor.Red);
                    }

                    continue;
                }

                if (arg.StartsWith("/p:")) {
                    var match = Regex.Match(arg, "/p:(?<name>[^=]+)=(?<value>.+)");
                    if (match.Success) {
                        var propertyName = match.Groups["name"].Value;
                        var propertyValue = match.Groups["value"].Value;
                        properties.Add(propertyName, propertyValue);
                        continue;
                    }
                }

                if (arg == "/assemblylist") {
                    emitAssemblyList = true;
                    continue;
                }

                if (arg == "/nobuiltinfederations") {
                    noBuiltInFederations = true;
                    Log.Message("Disabling built-in federations.");
                    continue;
                }

                if (arg.StartsWith("/federation:")) {
                    string server = arg.Substring("/federation:".Length);
                    Log.Message($"Adding federation '{server}'.");
                    federations.Add(server);
                    continue;
                }

                if (arg.StartsWith("/offlinefederation:")) {
                    var match = Regex.Match(arg, "/offlinefederation:(?<server>[^=]+)=(?<file>.+)");
                    if (match.Success) {
                        var server = match.Groups["server"].Value;
                        var assemblyListFileName = match.Groups["file"].Value;
                        offlineFederations[server] = assemblyListFileName;
                        Log.Message($"Adding federation '{server}' (offline from '{assemblyListFileName}').");
                        continue;
                    }
                    continue;
                }

                if (string.Equals(arg, "/noplugins", StringComparison.OrdinalIgnoreCase)) {
                    SolutionGenerator.LoadPlugins = false;
                    continue;
                }

                if (arg.StartsWith("/noplugin:")) {
                    pluginBlacklist.Add(arg.Substring("/noplugin:".Length));
                    continue;
                }

                try {
                    AddProject(projects, arg);
                }
                catch (Exception ex) {
                    Log.Write("Exception: " + ex.ToString(), ConsoleColor.Red);
                }
            }

            #endregion

            if (isDebug) {
                ProgramLoad.isDebug = true;
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.KeyChar == '\n') { // Enter
                    if (!Debugger.IsAttached)
                        Debugger.Launch();
                    else
                        Debugger.Break();
                }

                Console.WriteLine($"Debuging {Environment.CurrentDirectory}");
            }

            if (projects.Count == 0) {
                ProgramLoad.PrintUsage();
                return;
            }

            // AssertTraceListener.Register();
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler.HandleFirstChanceException;

            // This loads the real MSBuild from the toolset so that all targets and SDKs can be found
            // as if a real build is happening
            MSBuildLocator.RegisterDefaults();

            if (Paths.SolutionDestinationFolder == null) {
                Paths.SolutionDestinationFolder = Path.Combine(Microsoft.SourceBrowser.Common.Paths.BaseAppFolder, "Index");
            }

            var websiteDestination = Paths.SolutionDestinationFolder;

            // Warning, this will delete and recreate your destination folder
            Paths.PrepareDestinationFolder(force);

            Paths.SolutionDestinationFolder = Path.Combine(Paths.SolutionDestinationFolder, "index"); //The actual index files need to be written to the "index" subdirectory

            Directory.CreateDirectory(Paths.SolutionDestinationFolder);

            Log.ErrorLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.ErrorLogFile);
            Log.MessageLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.MessageLogFile);

            using (Disposable.Timing("Generating website")) {
                var federation = new Federation();

                if (!noBuiltInFederations) {
                    federation.AddFederations(Federation.DefaultFederatedIndexUrls);
                }

                federation.AddFederations(federations);

                foreach (var entry in offlineFederations) {
                    federation.AddFederation(entry.Key, entry.Value);
                }

                IndexSolutions(projects, properties, federation, serverPathMappings, pluginBlacklist);
                FinalizeProjects(emitAssemblyList, federation);
                WebsiteFinalizer.Finalize(websiteDestination, emitAssemblyList, federation);
            }
        }

        public static void AddProject(List<string> projects, string path)
        {
            var project = Path.GetFullPath(path);
            if (IsSupportedProject(project)) {
                projects.Add(project);
            } else {
                Log.Exception("Project not found or not supported: " + path, isSevere: false);
            }
        }

        private static bool IsSupportedProject(string filePath)
        {
            if (!File.Exists(filePath)) {
                return false;
            }

            return filePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);
        }

        
        private static readonly Folder<Project> mergedSolutionExplorerRoot = new Folder<Project>();

        public static void IndexSolutions(IEnumerable<string> solutionFilePaths, Dictionary<string, string> properties, Federation federation, Dictionary<string, string> serverPathMappings, IEnumerable<string> pluginBlacklist)
        {
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in solutionFilePaths) {
                using (Disposable.Timing("Reading assembly names from " + path)) {
                    foreach (var assemblyName in AssemblyNameExtractor.GetAssemblyNames(path)) {
                        assemblyNames.Add(assemblyName);
                    }
                }
            }

            var processedAssemblyList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in solutionFilePaths) {
                using (Disposable.Timing("Generating " + path)) {
                    using (var solutionGenerator = new SolutionGenerator(
                        path,
                        Paths.SolutionDestinationFolder,
                        properties: properties.ToImmutableDictionary(),
                        federation: federation,
                        serverPathMappings: serverPathMappings,
                        pluginBlacklist: pluginBlacklist)) {
                        solutionGenerator.GlobalAssemblyList = assemblyNames;
                        solutionGenerator.Generate(processedAssemblyList, mergedSolutionExplorerRoot);
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public static void FinalizeProjects(bool emitAssemblyList, Federation federation)
        {
            GenerateLooseFilesProject(Constants.MSBuildFiles, Paths.SolutionDestinationFolder);
            GenerateLooseFilesProject(Constants.TypeScriptFiles, Paths.SolutionDestinationFolder);
            using (Disposable.Timing("Finalizing references")) {
                try {
                    var solutionFinalizer = new SolutionFinalizer(Paths.SolutionDestinationFolder);
                    solutionFinalizer.FinalizeProjects(emitAssemblyList, federation, mergedSolutionExplorerRoot);
                }
                catch (Exception ex) {
                    Log.Exception(ex, "Failure while finalizing projects");
                }
            }
        }

        private static void GenerateLooseFilesProject(string projectName, string solutionDestinationPath)
        {
            var projectGenerator = new ProjectGenerator(projectName, solutionDestinationPath);
            projectGenerator.GenerateNonProjectFolder();
        }
    }

    internal static class WebsiteFinalizer
    {
        public static void Finalize(string destinationFolder, bool emitAssemblyList, Federation federation)
        {
            string sourcePath = AppDomain.CurrentDomain.BaseDirectory;
            try {
                sourcePath = Assembly.GetEntryAssembly().Location;
                // getEntry...
            }
            catch (Exception ex) {
                Console.WriteLine($"Entry failure {ex}");
            }

            sourcePath = Path.GetDirectoryName(sourcePath);
            string basePath = sourcePath;

            sourcePath = Path.Combine(sourcePath, "Web");
            Console.WriteLine($"\nWeb: {sourcePath}");
            if (!Directory.Exists(sourcePath)) {
                sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web");
                if (!Directory.Exists(sourcePath)) 
                    return;
            }

            sourcePath = Path.GetFullPath(sourcePath);
            FileUtilities.CopyDirectory(sourcePath, destinationFolder);

            StampOverviewHtmlWithDate(destinationFolder);

            if (emitAssemblyList) {
                ToggleSolutionExplorerOff(destinationFolder);
            }

            SetExternalUrlMap(destinationFolder, federation);
        }

        private static void StampOverviewHtmlWithDate(string destinationFolder)
        {
            var source = Path.Combine(destinationFolder, "wwwroot", "overview.html");
            var dst = Path.Combine(destinationFolder, "index", "overview.html");
            if (File.Exists(source)) {
                var text = File.ReadAllText(source);
                text = StampOverviewHtmlText(text);
                File.WriteAllText(dst, text);
            }
        }

        private static string StampOverviewHtmlText(string text)
        {
            return text.Replace("$(Date)", DateTime.Today.ToString("MMMM d", CultureInfo.InvariantCulture));
        }

        private static void ToggleSolutionExplorerOff(string destinationFolder)
        {
            var source = Path.Combine(destinationFolder, "wwwroot/scripts.js");
            var dst = Path.Combine(destinationFolder, "index/scripts.js");
            if (File.Exists(source)) {
                var text = File.ReadAllText(source);
                text = text.Replace("/*USE_SOLUTION_EXPLORER*/true/*USE_SOLUTION_EXPLORER*/", "false");
                File.WriteAllText(dst, text);
            }
        }

        private static void SetExternalUrlMap(string destinationFolder, Federation federation)
        {
            var source = Path.Combine(destinationFolder, "wwwroot/scripts.js");
            var dst = Path.Combine(destinationFolder, "index/scripts.js");
            if (File.Exists(source)) {
                var sb = new StringBuilder();
                foreach (var server in federation.GetServers()) {
                    if (sb.Length > 0) {
                        sb.Append(",");
                    }

                    sb.Append("\"");
                    sb.Append(server);
                    sb.Append("\"");
                }

                if (sb.Length > 0) {
                    var text = File.ReadAllText(source);
                    text = Regex.Replace(text, @"/\*EXTERNAL_URL_MAP\*/.*/\*EXTERNAL_URL_MAP\*/", sb.ToString());
                    File.WriteAllText(dst, text);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.SourceBrowser.Common;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            Program p = ParseArgs(args);
            p?.Run();
        }

        public static Program ParseArgs(string[] args)
        {
            var p = new Program();

            foreach (var arg in args)
            {
                if (arg.StartsWith("/out:"))
                {
                    Paths.SolutionDestinationFolder = Path.GetFullPath(arg.Substring("/out:".Length).StripQuotes());
                    continue;
                }

                if (arg.StartsWith("/serverPath:"))
                {
                    var mapping = arg.Substring("/serverPath:".Length).StripQuotes();
                    var parts = mapping.Split('=');
                    if (parts.Length != 2)
                    {
                        Log.Write($"Invalid Server Path: '{mapping}'", ConsoleColor.Red);
                        continue;
                    }
                    p.serverPathMappings.Add(Path.GetFullPath(parts[0]), parts[1]);
                    continue;
                }

                if (arg == "/force")
                {
                    Force = true;
                    continue;
                }

                if (arg.StartsWith("/in:"))
                {
                    string inputPath = arg.Substring("/in:".Length).StripQuotes();
                    try
                    {
                        if (!File.Exists(inputPath))
                        {
                            continue;
                        }

                        string[] paths = File.ReadAllLines(inputPath);
                        foreach (string path in paths)
                        {
                            AddProject(p.projects, path);
                        }
                    }
                    catch
                    {
                        Log.Write("Invalid argument: " + arg, ConsoleColor.Red);
                    }

                    continue;
                }

                if (arg.StartsWith("/p:"))
                {
                    var match = Regex.Match(arg, "/p:(?<name>[^=]+)=(?<value>.+)");
                    if (match.Success)
                    {
                        var propertyName = match.Groups["name"].Value;
                        var propertyValue = match.Groups["value"].Value;

                        p.properties.Add(propertyName, propertyValue);
                        continue;
                    }
                }

                if (arg == "/assemblylist")
                {
                    emitAssemblyList = true;
                    continue;
                }

                if (arg == "/nobuiltinfederations")
                {
                    noBuiltInFederations = true;
                    Log.Message("Disabling built-in federations.");
                    continue;
                }

                if (arg.StartsWith("/federation:"))
                {
                    string server = arg.Substring("/federation:".Length);
                    Log.Message($"Adding federation '{server}'.");
                    p.federations.Add(server);
                    continue;
                }

                if (arg.StartsWith("/offlinefederation:"))
                {
                    var match = Regex.Match(arg, "/offlinefederation:(?<server>[^=]+)=(?<file>.+)");
                    if (match.Success)
                    {
                        var server = match.Groups["server"].Value;
                        var assemblyListFileName = match.Groups["file"].Value;

                        p.offlineFederations[server] = assemblyListFileName;
                        Log.Message($"Adding federation '{server}' (offline from '{assemblyListFileName}').");
                        continue;
                    }
                    continue;
                }

                if (string.Equals(arg, "/noplugins", StringComparison.OrdinalIgnoreCase))
                {
                    SolutionGenerator.LoadPlugins = false;
                    continue;
                }

                if (arg.StartsWith("/noplugin:"))
                {
                    p.pluginBlacklist.Add(arg.Substring("/noplugin:".Length));
                    continue;
                }

                try
                {
                    AddProject(p.projects, arg);
                }
                catch (Exception ex)
                {
                    Log.Write("Exception: " + ex.ToString(), ConsoleColor.Red);
                }
            }

            if (p.projects.Count == 0)
            {
                PrintUsage();
                return null;
            }

            return p;
        }

        private Program()
        {
            projects = new List<string>();
            properties = new Dictionary<string, string>();
            offlineFederations = new Dictionary<string, string>();
            federations = new HashSet<string>();

            serverPathMappings = new Dictionary<string, string>();
            pluginBlacklist = new List<string>();
        }

        List<string> projects;
        Dictionary<string, string> properties;
        HashSet<string> federations;
        Dictionary<string, string> offlineFederations;
        Dictionary<string, string> serverPathMappings;
        List<string> pluginBlacklist;

        public static bool Force = false;
        public static bool Assert = true;

        public static bool emitAssemblyList = false;
        public static bool noBuiltInFederations = false;

        public void Prepare(bool force = false)
        {
            if (Assert)
                AssertTraceListener.Register();

            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler.HandleFirstChanceException;

            HelpMSBuildFindToolset();

            if (Paths.SolutionDestinationFolder == null)
            {
                Paths.SolutionDestinationFolder = Path.Combine(Microsoft.SourceBrowser.Common.Paths.BaseAppFolder, "Index");
            }

            var websiteDestination = Paths.SolutionDestinationFolder;

            // Warning, this will delete and recreate your destination folder
            Paths.PrepareDestinationFolder(force);

            Paths.SolutionDestinationFolder = Path.Combine(Paths.SolutionDestinationFolder, "index"); //The actual index files need to be written to the "index" subdirectory

            Directory.CreateDirectory(Paths.SolutionDestinationFolder);

            Log.ErrorLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.ErrorLogFile);
            Log.MessageLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.MessageLogFile);
        }

        public void Run(bool prepared = false)
        {
            if (!prepared)
                Prepare(Force);

            var websiteDestination = Paths.SolutionDestinationFolder;

            using (Disposable.Timing("Generating website"))
            {
                var federation = GetFederations();

                IndexSolutions(projects, properties, federation, serverPathMappings, pluginBlacklist);
                FinalizeProjects(emitAssemblyList, federation);
                WebsiteFinalizer.Finalize(websiteDestination, emitAssemblyList, federation);
            }
        }

        public Federation GetFederations()
        { 
            var federation = new Federation();

            if (!noBuiltInFederations)
            {
            federation.AddFederations(Federation.DefaultFederatedIndexUrls);
            }

            federation.AddFederations(federations);

            foreach (var entry in offlineFederations)
            {
                federation.AddFederation(entry.Key, entry.Value);
            }

            return federation;
        }


        /// <summary>
        /// Workaround for a bug in MSBuild where it couldn't find the SdkResolver if run not in VS command prompt:
        /// https://github.com/Microsoft/msbuild/issues/2369
        /// Pretend we're in the VS Command Prompt to fix this.
        /// </summary>
        private static void HelpMSBuildFindToolset()
        {
            if (Environment.GetEnvironmentVariable("VSINSTALLDIR") == null)
            {
                var root = @"C:\Program Files (x86)\Microsoft Visual Studio\2017";
                if (Directory.Exists(root))
                {
                    foreach (var sku in Directory.GetDirectories(root))
                    {
                        var msbuildExe = Path.Combine(sku, "MSBuild", "15.0", "Bin", "MSBuild.exe");
                        if (File.Exists(msbuildExe))
                        {
                            Environment.SetEnvironmentVariable("VSINSTALLDIR", sku);
                            Environment.SetEnvironmentVariable("VisualStudioVersion", @"15.0");
                            break;
                        }
                    }
                }
            }
        }

        public static void AddProject(List<string> projects, string path)
        {
            var project = Path.GetFullPath(path);
            if (IsSupportedProject(project))
            {
                projects.Add(project);
            }
            else if (!path.EndsWith(".exe"))
            {
                Log.Exception("Project not found or not supported: " + path, isSevere: false);
            }
        }

        private static bool IsSupportedProject(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            return filePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage: HtmlGenerator "
                + @"[/out:<outputdirectory>] "
                + @"[/force] "
                + @"[/noplugins] "
                + @"[/noplugin:Git] "
                + @"<pathtosolution1.csproj|vbproj|sln> [more solutions/projects..] "
                + @"[/in:<filecontaingprojectlist>] "
                + @"[/nobuiltinfederations] "
                + @"[/offlinefederation:server=assemblyListFile] "
                + @"[/assemblylist]");
        }

        private static readonly Folder<Project> mergedSolutionExplorerRoot = new Folder<Project>();

        public static void IndexSolutions(IEnumerable<string> solutionFilePaths, Dictionary<string, string> properties, Federation federation, Dictionary<string, string> serverPathMappings, IEnumerable<string> pluginBlacklist)
        {
            var assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in solutionFilePaths)
            {
                using (Disposable.Timing("Reading assembly names from " + path))
                {
                    foreach (var assemblyName in AssemblyNameExtractor.GetAssemblyNames(path))
                    {
                        assemblyNames.Add(assemblyName);
                    }
                }
            }

            foreach (var path in solutionFilePaths)
            {
                using (Disposable.Timing("Generating " + path))
                {
                    Exception error = null;

                    using (var solutionGenerator = new SolutionGenerator(
                        path,
                        Paths.SolutionDestinationFolder,
                        properties: properties.ToImmutableDictionary(),
                        federation: federation,
                        serverPathMappings: serverPathMappings,
                        pluginBlacklist: pluginBlacklist))
                    {
                        solutionGenerator.GlobalAssemblyList = assemblyNames;
                        solutionGenerator.Generate(solutionExplorerRoot: mergedSolutionExplorerRoot);
                    }

                    error = SolutionGenerator.Errors;
                    if (error != null)
                    {
                        var type = error.GetType().FullName; // as System.Reflection.???
                        // loadersErrors = 
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private static void FinalizeProjects(bool emitAssemblyList, Federation federation)
        {
            GenerateLooseFilesProject(Constants.MSBuildFiles, Paths.SolutionDestinationFolder);
            GenerateLooseFilesProject(Constants.TypeScriptFiles, Paths.SolutionDestinationFolder);
            using (Disposable.Timing("Finalizing references"))
            {
                try
                {
                    var solutionFinalizer = new SolutionFinalizer(Paths.SolutionDestinationFolder);
                    solutionFinalizer.FinalizeProjects(emitAssemblyList, federation, mergedSolutionExplorerRoot);
                }
                catch (Exception ex)
                {
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

    public static class WebsiteFinalizer
    {
        public static void Finalize(string destinationFolder, bool emitAssemblyList, Federation federation)
        {
            string sourcePath = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                // if not test unit
                sourcePath = Assembly.GetEntryAssembly().Location;
            }
            catch { }

            sourcePath = Path.GetDirectoryName(sourcePath);
            string basePath = sourcePath;
            sourcePath = Path.Combine(sourcePath, @"Web");
            if (!Directory.Exists(sourcePath))
            {
                return;
            }

            sourcePath = Path.GetFullPath(sourcePath);
            FileUtilities.CopyDirectory(sourcePath, destinationFolder);

            StampOverviewHtmlWithDate(destinationFolder);

            if (emitAssemblyList)
            {
                ToggleSolutionExplorerOff(destinationFolder);
            }

            SetExternalUrlMap(destinationFolder, federation);
        }

        private static void StampOverviewHtmlWithDate(string destinationFolder)
        {
            var source = Path.Combine(destinationFolder, "wwwroot/overview.html");
            var dst = Path.Combine(destinationFolder, "index/overview.html");
            if (File.Exists(source))
            {
                var text = File.ReadAllText(source);
                text = StampOverviewHtmlText(text);
                File.WriteAllText(dst, text);
            }
        }

        private static string StampOverviewHtmlText(string text)
        {
            text = text.Replace("$(Date)", DateTime.Today.ToString("MMMM d", CultureInfo.InvariantCulture));
            return text;
        }

        private static void ToggleSolutionExplorerOff(string destinationFolder)
        {
            var source = Path.Combine(destinationFolder, "wwwroot/scripts.js");
            var dst = Path.Combine(destinationFolder, "index/scripts.js");
            if (File.Exists(source))
            {
                var text = File.ReadAllText(source);
                text = text.Replace("/*USE_SOLUTION_EXPLORER*/true/*USE_SOLUTION_EXPLORER*/", "false");
                File.WriteAllText(dst, text);
            }
        }

        private static void SetExternalUrlMap(string destinationFolder, Federation federation)
        {
            var source = Path.Combine(destinationFolder, "wwwroot/scripts.js");
            var dst = Path.Combine(destinationFolder, "index/scripts.js");
            if (File.Exists(source))
            {
                var sb = new StringBuilder();
                foreach (var server in federation.GetServers())
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append("\"");
                    sb.Append(server);
                    sb.Append("\"");
                }

                if (sb.Length > 0)
                {
                    var text = File.ReadAllText(source);
                    text = Regex.Replace(text, @"/\*EXTERNAL_URL_MAP\*/.*/\*EXTERNAL_URL_MAP\*/", sb.ToString());
                    File.WriteAllText(dst, text);
                }
            }
        }
    }
}

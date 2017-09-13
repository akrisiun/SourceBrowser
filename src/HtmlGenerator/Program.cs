using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;  
  
namespace Microsoft
{
    using Microsoft.SourceBrowser.Common;
    using Microsoft.SourceBrowser.HtmlGenerator;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Reflection;

    public class ProgramLoader
    {
        public static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            dynamic argParam = MainLoad(args);
            if (argParam == null)
                return;

            new SourceBrowser.HtmlGenerator.Program()
            .Run(args, argParam);
        }

        public static dynamic MainLoad(string[] args)
        {
            dynamic argParam = new ExpandoObject();

            var lite3 = AppDomain.CurrentDomain.BaseDirectory + @"\x64\E_SQLITE3.dll";
            if (File.Exists(lite3))
            {
                var res = Win32.LoadLibrary(lite3);
            }

            argParam.projects = new List<string>();
            argParam.properties = new Dictionary<string, string>();

            argParam.offlineFederations = new Dictionary<string, string>();
            argParam.federations = new HashSet<string>();
            argParam.serverPathMappings = new Dictionary<string, string>();
            argParam.pluginBlacklist = new List<string>();
            argParam.LoadPlugins = true;

            argParam.emitAssemblyList = false;
            argParam.force = false;
            argParam.noBuiltInFederations = false;
            argParam.SolutionDestinationFolder = "";
            var projects = argParam.projects;
            var SolutionDestinationFolder = argParam.SolutionDestinationFolder;
            var properties = argParam.properties;
            bool debug = false;

            foreach (var arg in args)
            {
                if (arg.StartsWith("/debug") || arg.StartsWith("-debug"))
                {
                    debug = true;
                    Console.Write("Debug?");
                    Console.ReadLine();
                    continue;
                }

                if (arg.StartsWith("/out:"))
                {
                    argParam.SolutionDestinationFolder = Path.GetFullPath(arg.Substring("/out:".Length).StripQuotes());
                    SolutionDestinationFolder = argParam.SolutionDestinationFolder;
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
                    argParam.serverPathMappings.Add(Path.GetFullPath(parts[0]), parts[1]);
                    continue;
                }

                if (arg == "/force" || arg == "-f" || arg == "-y")
                {
                    argParam.force = true;
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
                            AddProject(projects, path);
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
                        properties.Add(propertyName, propertyValue);
                        continue;
                    }
                }

                if (arg == "/assemblylist")
                {
                    argParam.emitAssemblyList = true;
                    continue;
                }

                if (arg == "/nobuiltinfederations")
                {
                    argParam.noBuiltInFederations = true;
                    Log.Message("Disabling built-in federations.");
                    continue;
                }

                if (arg.StartsWith("/federation:"))
                {
                    string server = arg.Substring("/federation:".Length);
                    Log.Message($"Adding federation '{server}'.");

                    argParam.federations.Add(server);
                    continue;
                }

                if (arg.StartsWith("/offlinefederation:"))
                {
                    var match = Regex.Match(arg, "/offlinefederation:(?<server>[^=]+)=(?<file>.+)");
                    if (match.Success)
                    {
                        var server = match.Groups["server"].Value;
                        var assemblyListFileName = match.Groups["file"].Value;

                        argParam.offlineFederations[server] = assemblyListFileName;
                        Log.Message($"Adding federation '{server}' (offline from '{assemblyListFileName}').");
                        continue;
                    }
                    continue;
                }

                if (string.Equals(arg, "/noplugins", StringComparison.OrdinalIgnoreCase))
                {
                    argParam.LoadPlugins = false;
                    // SolutionGenerator.LoadPlugins = false;
                    continue;
                }

                if (arg.StartsWith("/noplugin:"))
                {
                    argParam.pluginBlacklist.Add(arg.Substring("/noplugin:".Length));
                    continue;
                }

                try
                {
                    AddProject(projects, arg);
                }
                catch (Exception ex)
                {
                    Log.Write("Exception: " + ex.ToString(), ConsoleColor.Red);
                }
            }

            if (debug && Debugger.IsAttached)
            {
                Debugger.Break();
            }

            if (projects.Count == 0)
            {
                PrintUsage();
                return null;
            }

            //  LoadAssemblies();
            return argParam;
        }

        public class Win32
        {
            [DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]

            public static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        }

        private static void AddProject(List<string> projects, string path)
        {
            var project = Path.GetFullPath(path);
            if (IsSupportedProject(project))
            {
                projects.Add(project);
            }
            else
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

    }
}

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    using System.Collections.Immutable;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.SourceBrowser.Common;
    using System.Diagnostics;

    public class Program
    {
        public void Run(string[] args, dynamic argParam)
        {
            var projects = argParam.projects;
            if (argParam.SolutionDestinationFolder.Length > 0)
                Paths.SolutionDestinationFolder = argParam.SolutionDestinationFolder;

            SolutionGenerator.LoadPlugins = argParam.LoadPlugins;
            var force = argParam.force;
            var properties = argParam.properties;
            var emitAssemblyList = argParam.emitAssemblyList;

            Prepare(argParam.force);

            using (Disposable.Timing("Generating website"))
            {
                Federation federation = FederateIndex(projects, properties, argParam);

                FinalizeProjects(emitAssemblyList, federation);

                var websiteDestination = Paths.SolutionDestinationFolder;
                WebsiteFinalizer.Finalize(websiteDestination, emitAssemblyList, federation);
            }
        }

        public static Federation Create(bool noBuiltInFederations = false)
        {
            var federation = new Federation();

            if (!noBuiltInFederations)
            {
                foreach (string url in Federation.FederatedIndexUrls)
                    federation.AddFederation(url);
            }
            return federation;
        }

        public static Federation FederateIndex(IEnumerable<string> projects,
            Dictionary<string, string> properties,
            IDictionary<string, object> argParam)
        {
            bool noBuiltInFederations = (argParam["noBuiltInFederations"] as bool?) ?? false;
            var federation = Create(noBuiltInFederations);

            var offlineFederations = argParam["offlineFederations"] as Dictionary<string, string>;
            var serverPathMappings = argParam["serverPathMappings"] as Dictionary<string, string>;
            var pluginBlacklist = argParam["pluginBlacklist"] as IEnumerable<string>;

            foreach (var entry in offlineFederations)
            {
                federation.AddFederation(entry.Key, entry.Value);
            }

            IndexSolutions(projects, properties, federation, serverPathMappings, pluginBlacklist);

            return federation;
        }

        public static void Prepare(bool force)
        {
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler.HandleFirstChanceException;
            AssertTraceListener.Register();

            WorkspaceHacks.Prepare();

            if (Paths.SolutionDestinationFolder == null)
            {
                Paths.SolutionDestinationFolder = Path.Combine(Microsoft.SourceBrowser.Common.Paths.BaseAppFolder, "Index");
            }

            if (!Paths.SolutionDestinationFolder.EndsWith("Index"))
                Paths.SolutionDestinationFolder = Path.Combine(Paths.SolutionDestinationFolder, "index");
            //The actual index files need to be written to the "index" subdirectory

            // Warning, this will delete and recreate your destination folder
            Paths.PrepareDestinationFolder(force);

            Directory.CreateDirectory(Paths.SolutionDestinationFolder);

            Log.ErrorLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.ErrorLogFile);
            Log.MessageLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.MessageLogFile);
        }

        private static readonly Folder<Project> mergedSolutionExplorerRoot = new Folder<Project>();
        private static Federation Federation;

        public static void IndexSolutions(IEnumerable<string> solutionFilePaths,
            Dictionary<string, string> properties,
            Federation federation = null)
        {
            federation = federation ?? Federation;
            if (federation == null)
                federation = Federation = new Federation();

            IndexSolutions(solutionFilePaths, properties, federation);
        }

        private static void IndexSolutions(IEnumerable<string> solutionFilePaths,
            Dictionary<string, string> properties,
            Federation federation,
            Dictionary<string, string> serverPathMappings, IEnumerable<string> pluginBlacklist)
        {
            federation = federation ?? Federation;
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
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        public static void FinalizeProjects(Federation federation = null)
        {
            federation = federation ?? Federation;
            FinalizeProjects(true, federation);
        }

        public static void FinalizeProjects(bool emitAssemblyList, Federation federation)
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

    internal static class WebsiteFinalizer
    {
        public static void Finalize(string destinationFolder, bool emitAssemblyList, Federation federation)
        {
            string sourcePath = Assembly.GetEntryAssembly().Location;
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

namespace Tests
{
    using Microsoft.SourceBrowser.Common;
    using Microsoft.SourceBrowser.HtmlGenerator;

    public class Run
    {
        public static void MainCheck(string csproj, string[] args)
        {
            //if (args.Length >= 1 && args[0].StartsWith("/out:"))
            //    SolutionGenerator.SolutionOutFolder = Environment.CurrentDirectory + @"\" + args[0].Substring(5);

            RunCheck(args, csproj);
        }

        public static void RunCheck(string[] args, string csproj, bool force = true, bool emitAssemblyList = true)
        {
            var main = new Program();

            var mergedSolutionExplorerRoot = Microsoft.SourceBrowser.HtmlGenerator.Paths.SolutionDestinationFolder;

            Program.Prepare(force);
            SolutionGenerator.LoadPlugins = false;
            var projects = new List<string> { csproj };

            using (Disposable.Timing("Generating website"))
            {
                Federation federation = Program.Create(false);

                var solutionGenerator = new SolutionGenerator(
                      csproj,
                      Microsoft.SourceBrowser.HtmlGenerator.Paths.SolutionDestinationFolder);

                //solutionGenerator.GlobalAssemblyList = assemblyNames;
                solutionGenerator.Generate(null, solutionExplorerRoot: null);
                //solutionGenerator.CheckGenerate(csproj);

                Program.FinalizeProjects(emitAssemblyList, federation);

                solutionGenerator.Dispose();
            }
        }
    }

}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.SourceBrowser.Common;
using Hacks.HtmlGenerator.Utilities;
using System.Reflection;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var projects = new List<string>();
            var properties = new Dictionary<string, string>();
            Paths.SolutionDestinationFolder = Path.GetFullPath(@"srcweb"); // default

            #region Parse args

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            for (int idx = 0; idx < args.Length; idx++)
            {
                string arg = args[idx];

                if (arg.StartsWith("-debug"))
                {
                    Console.ReadLine();
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
                    continue;
                }

                if (arg.StartsWith("/out:"))
                {
                    Paths.SolutionDestinationFolder = Path.GetFullPath(arg.Substring("/out:".Length).StripQuotes());
                    continue;
                }
                if (arg.StartsWith("/outdir"))
                {
                    idx++;
                    Paths.SolutionDestinationFolder = Path.GetFullPath(args[idx].StripQuotes());
                    continue;
                }

                if (arg.StartsWith("-y", StringComparison.InvariantCulture))
                    continue;

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

                if (arg.StartsWith("/fast") || arg.StartsWith("/content"))
                {
                    // bypass slow .cs files processing
                    Configuration.ProcessAll = false;
                    continue;
                }

                AddProjectSafe(projects, arg);
            }

            if (projects.Count == 0)
            {
                ImmutableList<string> solutions = null;
                try
                {
                    solutions =
                        Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.sln", SearchOption.TopDirectoryOnly)
                        .ToImmutableList();
                }
                catch { }
                if (solutions == null || solutions.Count != 1)
                {
                    PrintUsage();
                    return;
                }

                AddProjectSafe(projects, solutions[0]);
            }
            #endregion

            AssertTraceListener.Register();
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler.HandleFirstChanceException;

            if (Paths.SolutionDestinationFolder == null)
            {
                Paths.SolutionDestinationFolder = Path.Combine(Microsoft.SourceBrowser.Common.Paths.BaseAppFolder, "Index");
            }

            Log.ErrorLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.ErrorLogFile);
            Log.MessageLogFilePath = Path.Combine(Paths.SolutionDestinationFolder, Log.MessageLogFile);

            // Warning, this will delete and recreate your destination folder
            Paths.PrepareDestinationFolder();

            string message = "Generating website" + (!Configuration.ProcessAll ? " /fast" : String.Empty);
            using (Disposable.Timing(message))
            {
                IndexSolutions(projects, properties);
                FinalizeProjects(emitAssemblyList: true);
            }
        }

        public static void AddProjectSafe(List<string> projects, string arg)
        {
            try
            {
                AddProject(projects, arg);
            }
            catch (Exception ex)
            {
                Log.Write("Exception: " + ex.ToString(), ConsoleColor.Red);
            }
        }

        private static void AddProject(List<string> projects, string path)
        {
            var project = Path.GetFullPath(path);
            if (IsSupportedProject(project))
            {
                if (project.EndsWith(ProjectJsonUtilities.globalJson))
                {
                    // var json = System.IO.File.ReadAllText(project);
                    var list = ProjectJsonUtilities.GetProjects(project);
                    foreach (string projectJson in list)
                        projects.Add(projectJson);

                    return;
                }

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

            if (filePath.EndsWith(".xproj", StringComparison.OrdinalIgnoreCase))
            {
                WorkSpaceXProj.ParseXProj(filePath);
                return File.Exists(Path.ChangeExtension(filePath, "csproj"));
            }

            if (filePath.EndsWith(ProjectJsonUtilities.projectJson // "project.json"))
                        , StringComparison.OrdinalIgnoreCase))
            {
                WorkspaceProjectJson.ParseJson(filePath);
                return File.Exists(Path.ChangeExtension(filePath, "csproj"));
            }
            if (filePath.EndsWith(ProjectJsonUtilities.globalJson // "global.json"))
                        , StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return filePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".kproj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".proj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith("project.json", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith("global.json");
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage: HtmlGenerator "
                + @"[/out:<outputdirectory>] "
                + @"<pathtosolution1.csproj|vbproj|sln> [more solutions/projects..] "
                + @"[/in:<filecontaingprojectlist>] "
                + @"[/assemblylist]");
        }

        private static readonly Folder<Project> mergedSolutionExplorerRoot = new Folder<Project>();

        public static void IndexSolutions(IEnumerable<string> solutionFilePaths, Dictionary<string, string> properties)
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

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Assembly.LoadFile(baseDir + "System.Reflection.Metadata.dll");
            Assembly.LoadFile(baseDir + "Microsoft.CodeAnalysis.dll");        // , Version=1.3.0.0
            Assembly.LoadFile(baseDir + "Microsoft.CodeAnalysis.CSharp.dll"); // , Version=1.3.0.0

            var federation = new Federation();
            foreach (var path in solutionFilePaths)
            {
                using (Disposable.Timing("Generating " + path))
                {
                    using (var solutionGenerator = new SolutionGenerator(
                        path,
                        Paths.SolutionDestinationFolder,
                        properties: properties.ToImmutableDictionary(),
                        federation: federation)
                        .Create())
                    {
                        solutionGenerator.GlobalAssemblyList = assemblyNames;
                        solutionGenerator.Generate(solutionExplorerRoot: mergedSolutionExplorerRoot);

                        if (Configuration.ProcessReferencies)
                            Extend.ExtendGenerator.TopReferencedAssemblies(solutionGenerator, federation, mergedSolutionExplorerRoot);
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

        }

        public static void FinalizeProjects(bool emitAssemblyList = true)
        {
            GenerateLooseFilesProject(Constants.MSBuildFiles, Paths.SolutionDestinationFolder);
            GenerateLooseFilesProject(Constants.TypeScriptFiles, Paths.SolutionDestinationFolder);
            using (Disposable.Timing("Finalizing references"))
            {
                SolutionFinalizer solutionFinalizer = null;
                bool error = false;
                try
                {
                    solutionFinalizer = new SolutionFinalizer(Paths.SolutionDestinationFolder);
                    solutionFinalizer.FinalizeProjects(emitAssemblyList, mergedSolutionExplorerRoot);
                }
                catch (Exception ex)
                {
                    error = true;
                    Log.Exception(ex, "Failure while finalizing projects");
                }

                Extend.ExtendGenerator.Finalize(solutionFinalizer, mergedSolutionExplorerRoot, error);
            }
        }

        private static void GenerateLooseFilesProject(string projectName, string solutionDestinationPath)
        {
            var projectGenerator = new ProjectGenerator(projectName, solutionDestinationPath);
            projectGenerator.GenerateNonProjectFolder();
        }
    }
}

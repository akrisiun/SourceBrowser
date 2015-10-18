using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.SourceBrowser.Common;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var projects = new List<string>();
            var properties = new Dictionary<string, string>();

            foreach (var arg in args)
            {
                if (arg.StartsWith("/out:"))
                {
                    Paths.SolutionDestinationFolder = Path.GetFullPath(arg.Substring("/out:".Length).StripQuotes());
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

                if (arg.StartsWith("/fast") || arg.StartsWith("/content"))
                {
                    // bypass slow .cs files processing
                    Configuration.ProcessAll = false;
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

            if (projects.Count == 0)
            {
                PrintUsage();
                return;
            }

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
                FinalizeProjects();
            }
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
                   filePath.EndsWith(".kproj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".proj", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"Usage: HtmlGenerator [/out:<outputdirectory>] <pathtosolution1.csproj|vbproj|sln> [more solutions/projects..] [/in:<filecontaingprojectlist>]");
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

            var federation = new Federation();
            foreach (var path in solutionFilePaths)
            {
                using (Disposable.Timing("Generating " + path))
                {
                    using (var solutionGenerator = new SolutionGenerator(
                        path,
                        Paths.SolutionDestinationFolder,
                        properties: properties.ToImmutableDictionary(),
                        federation: federation))
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

        public static void FinalizeProjects()
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
                    solutionFinalizer.FinalizeProjects(mergedSolutionExplorerRoot);
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

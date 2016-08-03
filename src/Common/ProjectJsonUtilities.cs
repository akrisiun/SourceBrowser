using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.FileSystemGlobbing;
using Microsoft.DotNet.ProjectModel.Workspaces;
using NuGet.Frameworks;
using RoslynWorkspace = Microsoft.CodeAnalysis.Workspace;
using System.Reflection;

namespace Microsoft.SourceBrowser.Common
{
    public class ProjectJsonUtilities
    {
        public const string projectJson = "project.json";
        public const string globalJson = "global.json";

        public static ProjectContext GetCompatibleProjectContext(string projectFilePath)
        {
            var folder = Path.GetDirectoryName(projectFilePath);
            var frameworks = ProjectReader.GetProject(folder).GetTargetFrameworks().Select(f => f.FrameworkName);
            var targetFramework = NuGetFrameworkUtility.GetNearest(frameworks,
                FrameworkConstants.CommonFrameworks.Net462, f => f);

            if (targetFramework == null)
            {
                throw new InvalidOperationException(
                    // $"Could not find framework in '{projectFilePath}' compatible with .NET 4.6.2.");
                    String.Format("Could not find framework in {0} compatible with.NET 4.6", projectFilePath));
            }

            ProjectContext project = null;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            HashSet<Assembly> asmList = new HashSet<Assembly>();
            asmList.Add(Assembly.Load(baseDir + "Microsoft.Extensions.DependencyModel.dll"));       // -> 1.0.0.0
            // Microsoft.DotNet.InternalAbstractions, Version = 1.0.1.0,  -> 1.0.0.0
            asmList.Add(Assembly.Load(baseDir + "Microsoft.DotNet.InternalAbstractions.dll"));
            // System.IO.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            asmList.Add(Assembly.Load(baseDir + "System.IO.FileSystem.dll"));

            try
            {
                project = ProjectContext.Create(folder, targetFramework);
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }

            return project;
        }

        public static RoslynWorkspace CreateWorkspace(string projectFile)
        {
            return new ProjectJsonWorkspace(GetCompatibleProjectContext(projectFile));
        }

        public static IEnumerable<string> GetProjects(string globalJsonPath)
        {
            GlobalSettings global;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Assembly jsonAsm = Assembly.LoadFile(baseDir + "Newtonsoft.Json.dll");

            if (!GlobalSettings.TryGetGlobalSettings(globalJsonPath, out global))
            {
                throw new InvalidOperationException("Could not load global.json file from " + globalJsonPath);
            }

            var dir = Directory.GetCurrentDirectory();
            var path = Path.GetDirectoryName(globalJsonPath);
            if (path != dir)
                Directory.SetCurrentDirectory(path);

            var matcher = new Matcher();
            matcher.AddInclude("*/project.json");
            if (!global.ProjectSearchPaths.Any())
            {
                return matcher.GetResultsInFullPath(global.DirectoryPath);
            }
            else
            {
                var found = new List<string>();
                foreach (var searchPath in global.ProjectSearchPaths)
                {
                    string searchPathDir = Path.Combine(path, searchPath).Replace(@"\", "/");
                    if (File.Exists(searchPathDir + "/project.json"))
                        found.Add(searchPathDir + "/project.json");
                    else
                        found.AddRange(matcher.GetResultsInFullPath(searchPathDir));
                }
                return found;
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name;
            var asm = args.RequestingAssembly;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fileName = Path.GetFileNameWithoutExtension(name);

            if (fileName == "Newtonsoft.Json" || fileName == "Microsoft.Extensions.DependencyModel"
                || File.Exists(baseDir + fileName + ".dll"))
                asm = Assembly.LoadFrom(baseDir + fileName + ".dll");

            return asm;
        }

        public static RoslynWorkspace CreateWorkspaceFromGlobal(string globalJsonPath)
        {
            return new ProjectJsonWorkspace(GetProjects(globalJsonPath));
        }
    }
}
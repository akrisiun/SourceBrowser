using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.FileSystemGlobbing;
using Microsoft.DotNet.ProjectModel.Workspaces;
using NuGet.Frameworks;
using RoslynWorkspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.SourceBrowser.Common
{
    public class ProjectJsonUtilities
    {
        public static ProjectContext GetCompatibleProjectContext(string projectFilePath)
        {
            var folder = Path.GetDirectoryName(projectFilePath);
            var frameworks = ProjectReader.GetProject(folder).GetTargetFrameworks().Select(f => f.FrameworkName);
            var targetFramework = NuGetFrameworkUtility.GetNearest(frameworks,
                FrameworkConstants.CommonFrameworks.Net462, f => f);

            if (targetFramework == null)
            {
                throw new InvalidOperationException(
                    $"Could not find framework in '{projectFilePath}' compatible with .NET 4.6.2.");
            }
            var project = ProjectContext.Create(folder, targetFramework);
            return project;
        }

        public static RoslynWorkspace CreateWorkspace(string projectFile)
        {
            return new ProjectJsonWorkspace(GetCompatibleProjectContext(projectFile));
        }

        public static IEnumerable<string> GetProjects(string globalJsonPath)
        {
            GlobalSettings global;
            if (!GlobalSettings.TryGetGlobalSettings(globalJsonPath, out global))
            {
                throw new InvalidOperationException("Could not load global.json file from " + globalJsonPath);
            }

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
                    found.AddRange(matcher.GetResultsInFullPath(searchPath));
                }
                return found;
            }
        }

        public static RoslynWorkspace CreateWorkspaceFromGlobal(string globalJsonPath)
        {
            return new ProjectJsonWorkspace(GetProjects(globalJsonPath));
        }
    }
}
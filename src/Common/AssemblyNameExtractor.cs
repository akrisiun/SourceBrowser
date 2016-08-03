﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
//using Microsoft.DotNet.ProjectModel;
//using Microsoft.DotNet.ProjectModel.FileSystemGlobbing;
//using Microsoft.DotNet.ProjectModel.FileSystemGlobbing.Abstractions;

namespace Microsoft.SourceBrowser.Common
{
    public static class AssemblyNameExtractor
    {
        private static readonly object projectCollectionLock = new object();

        private static readonly Regex assemblyNameRegex = new Regex(@"<(?:Module)?AssemblyName>((\w|\.|\$|\(|\)|-)+)</(?:Module)?AssemblyName>", RegexOptions.Compiled);
        private static readonly Regex rootNamespaceRegex = new Regex(@"<RootNamespace>((\w|\.)+)</RootNamespace>", RegexOptions.Compiled);
        public static string BaseDir;

        public static IEnumerable<string> GetAssemblyNames(string projectOrSolutionFilePath, string baseDir = null)
        {
            if (!File.Exists(projectOrSolutionFilePath))
            {
                return null;
            }

            BaseDir = baseDir;

            if (projectOrSolutionFilePath.EndsWith(".sln"))
            {
                return GetAssemblyNamesFromSolution(projectOrSolutionFilePath);
            }
            else
            if (projectOrSolutionFilePath.EndsWith(ProjectJsonUtilities.projectJson)) // "project.json"))
            {
                return new[] { GetAssemblyNameFromProjectJson(projectOrSolutionFilePath) };
            }
            else if (projectOrSolutionFilePath.EndsWith(ProjectJsonUtilities.globalJson)) // "global.json"))
            {
                return ProjectJsonUtilities.GetProjects(projectOrSolutionFilePath)
                    .Select(GetAssemblyNameFromProjectJson);
            }
            else
            {
                return new[] { GetAssemblyNameFromProject(projectOrSolutionFilePath) };
            }
        }

        public static string GetAssemblyNameFromProjectJson(string projectFilePath)
        {
            var project = ProjectJsonUtilities.GetCompatibleProjectContext(projectFilePath);

            return project.GetOutputPaths("Debug").CompilationFiles.Assembly;
        }

        public static string GetAssemblyNameFromProject(string projectFilePath)
        {
            string assemblyName = null;

            // first try regular expressions for the fast case
            var projectText = File.ReadAllText(projectFilePath);
            var match = assemblyNameRegex.Match(projectText);
            if (match.Groups.Count >= 2)
            {
                assemblyName = match.Groups[1].Value;

                if (assemblyName == "$(RootNamespace)")
                {
                    match = rootNamespaceRegex.Match(projectText);
                    if (match.Groups.Count >= 2)
                    {
                        assemblyName = match.Groups[1].Value;
                    }
                }

                return assemblyName;
            }

            // if regexes didn't work, try reading the XML ourselves
            var doc = XDocument.Load(projectFilePath);
            var ns = @"http://schemas.microsoft.com/developer/msbuild/2003";
            var propertyGroups = doc.Descendants(XName.Get("PropertyGroup", ns));
            var assemblyNameElement = propertyGroups.SelectMany(g => g.Elements(XName.Get("AssemblyName", ns))).LastOrDefault();
            if (assemblyNameElement != null && !assemblyNameElement.Value.Contains("$"))
            {
                assemblyName = assemblyNameElement.Value;
                return assemblyName;
            }

            var projectFileName = Path.GetFileNameWithoutExtension(projectFilePath);

            lock (projectCollectionLock)
            {
                try
                {
                    var project = ProjectCollection.GlobalProjectCollection.LoadProject(
                        projectFilePath,
                        toolsVersion: "14.0");

                    assemblyName = project.GetPropertyValue("AssemblyName");
                    if (assemblyName == "")
                    {
                        assemblyName = projectFileName;
                    }

                    if (assemblyName != null)
                    {
                        return assemblyName;
                    }
                }
                finally
                {
                    ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
                }
            }

            return projectFileName;
        }

        public static IEnumerable<string> GetAssemblyNamesFromSolution(string solutionFilePath)
        {
            var solution = SolutionFile.Parse(solutionFilePath);
            var assemblies = new List<string>(solution.ProjectsInOrder.Count);
            foreach (var project in solution.ProjectsInOrder)
            {
                if (project.ProjectType == SolutionProjectType.SolutionFolder)
                {
                    continue;
                }

                try
                {
                    string assembly = GetAssemblyNameFromProject(project.AbsolutePath);
                    assemblies.Add(assembly);
                }
                catch
                {
                }
            }

            return assemblies;
        }
    }
}

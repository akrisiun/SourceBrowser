using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.SourceBrowser.Common
{
    public static class AssemblyNameExtractor
    {
        private static readonly object projectCollectionLock = new object();

        private static readonly Regex assemblyNameRegex = new Regex(@"<(?:Module)?AssemblyName>((\w|\.|\$|\(|\)|-)+)</(?:Module)?AssemblyName>", RegexOptions.Compiled);
        private static readonly Regex rootNamespaceRegex = new Regex(@"<RootNamespace>((\w|\.)+)</RootNamespace>", RegexOptions.Compiled);

        public static IEnumerable<string> GetAssemblyNames(string projectOrSolutionFilePath)
        {
            if (!File.Exists(projectOrSolutionFilePath))
            {
                return null;
            }

            if (projectOrSolutionFilePath.EndsWith(".sln"))
            {
                return GetAssemblyNamesFromSolution(projectOrSolutionFilePath);
            }
            else
            {
                return new[] { GetAssemblyNameFromProject(projectOrSolutionFilePath) };
            }
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

            // $env: VSINSTALLDIR = "C:\Program Files (x86)\Microsoft Visual Studio\Preview\Community"
            // $env: VisualStudioVersion = "15.0"
            // $env: MSBUILD_EXE_PATH = "$env:VSINSTALLDIR\MSBuild\15.0\Bin\MSBuild.exe";

            lock (projectCollectionLock)
            {
                try
                {
                    var project = ProjectCollection.GlobalProjectCollection.LoadProject(
                        projectFilePath,
                        toolsVersion: "15.0");

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
                /*
                    WTF:  MSB0001: Internal MSBuild Error: Type information 
                    for Microsoft.Build.Utilities.ToolLocationHelper was present in the whitelist cache as 
                    Microsoft.Build.Utilities.ToolLocationHelper, Microsoft.Build.Utilities.Core,
                    Version = 15.1.0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a but the type could not be loaded.unexpectedly null
                */
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

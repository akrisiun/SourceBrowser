using System;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using Microsoft.Build.Construction;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    // app.config, web.config generate
    public class ContentXmlSupport : XmlSupport, IExtend
    {
        public static string[] Extensions = new[] { ".config", ".xslt", ".xml" };
        public ushort Glyph { get { return 227; } } // for DeclaredSymbolInfo like xaml file

        public string FilePath { get; protected set; }
        public string DestinationFolder { get; set; }
        public string ProjectSourceFolder { get { return Path.GetDirectoryName(generator.ProjectFilePath); } }
        public Project project { get; private set; }
        public ProjectGenerator generator { get; private set; }

        public ContentXmlSupport(ProjectGenerator generator)
        {
            this.generator = generator;
            FilePath = generator.ProjectFilePath;
            DestinationFolder = generator.ProjectDestinationFolder;
        }

        #region Parse

        public void ParseProject(Project msbuildProject, string extension)
        {
            this.project = msbuildProject;

            ICollection<ProjectItem> items = new Collection<ProjectItem>();
            foreach (ProjectItem none in msbuildProject.GetItems("None"))
                if (none.EvaluatedInclude.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
                    items.Add(none);
            foreach (ProjectItem content in msbuildProject.GetItems("Content"))
                if (content.EvaluatedInclude.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
                    items.Add(content);

            foreach (ProjectItem file in items)
            {
                var filePath = Path.Combine(ProjectSourceFolder, file.EvaluatedInclude);
                Generate(filePath, DestinationFolder, this.project);
            }
        }

        public void ParseProject(Project msbuildProject, string[] extensionList)
        {
            this.project = msbuildProject;
            ICollection<ProjectItem> items = new Collection<ProjectItem>();
            AddRange<ProjectItem>(items, msbuildProject.GetItems("None"));
            AddRange<ProjectItem>(items, msbuildProject.GetItems("Content"));
            AddRange<ProjectItem>(items, msbuildProject.GetItems("EmbeddedResource"));

            foreach (string file in WhereIncludeEnds(items, extensionList))
            {
                var filePath = Path.Combine(ProjectSourceFolder, file);
                Generate(filePath, DestinationFolder, this.project);
            }
        }

        // .cshtml files
        public void ParseRazorSrcFiles(Project msbuildProject, string extension = ".cshtml")
        {
            //<ItemGroup><RazorSrcFiles Include
            ParseProject(msbuildProject, extension);

            ICollection<ProjectItem> items = new Collection<ProjectItem>();
            foreach (ProjectItem none in msbuildProject.GetItems("RazorSrcFiles"))
                if (none.EvaluatedInclude.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
                    items.Add(none);

            foreach (ProjectItem file in items)
            {
                var filePath = Path.Combine(ProjectSourceFolder, file.EvaluatedInclude);
                Generate(filePath, DestinationFolder, this.project);
            }
        }

        static IEnumerable<string> WhereIncludeEnds(IEnumerable<ProjectItem> list, string[] extensionList)
        {
            var numer = list.GetEnumerator();
            while (numer.MoveNext())
            {
                string include = numer.Current.EvaluatedInclude;
                foreach (string ext in extensionList)
                {
                    if (include.EndsWith(ext, StringComparison.CurrentCultureIgnoreCase))
                    {
                        yield return include;
                        break;
                    }
                }
            }
        }

        static ICollection<T> AddRange<T>(ICollection<T> list, IEnumerable<T> range)
        {
            foreach (var content in range)
                list.Add(content);
            return list;
        }
        #endregion

        public void Generate(string filePath, string htmlFilePath, Project project)
        {
            this.project = project;
            this.FilePath = filePath;
            this.project = project;
            Contract.Assert(project != null);

            var relativePath = Paths.MakeRelativeToFolder(filePath, ProjectSourceFolder);
            relativePath = relativePath.Replace("..", "parent");
            var destinationHtmlFile = Path.Combine(DestinationFolder, relativePath) + ".html";
            var solutionFolder = generator.SolutionGenerator.SolutionDestinationFolder;
            base.Generate(filePath, destinationHtmlFile, solutionFolder);

            generator.OtherFiles.Add(relativePath);
            ProjectGenerator.AddDeclaredSymbolToRedirectMap(
                   generator.SymbolIDToListOfLocationsMap, SymbolIdService.GetId(relativePath), relativePath, 0);
        }

        // Implement XmlSupport
        protected override string GetDisplayName() { return FilePath; }
        protected override string GetAssemblyName() { return Path.GetFileNameWithoutExtension(project.FullPath); }
    }
}

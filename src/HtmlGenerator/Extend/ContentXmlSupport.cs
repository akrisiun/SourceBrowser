using System;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    // app.config, web.config generate
    public class ContentXmlSupport : XmlSupport, IExtend
    {
        public static string[] Extensions = new[] { ".config", ".xslt", ".xml" };

        public string FilePath { get; protected set; }
        public string ProjectSourceFolder { get { return Path.GetDirectoryName(generator.ProjectFilePath); } }
        public string DestinationFolder { get; set; }
        public Project project { get; private set; }
        public ProjectGenerator generator { get; private set; }

        public ContentXmlSupport(ProjectGenerator generator)
        {
            this.generator = generator;
            FilePath = generator.ProjectFilePath;
            DestinationFolder = generator.ProjectDestinationFolder;
        }

        public void ParseProject(Project msbuildProject, string extension)
        {
            this.project = msbuildProject;
            var types = msbuildProject.ItemTypes;
            ICollection<ProjectItem> items = msbuildProject.GetItems("None");
            foreach (ProjectItem file in
                System.Linq.Enumerable.Where(items, i => i.EvaluatedInclude.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase)))
            {
                var filePath = Path.Combine(ProjectSourceFolder, file.EvaluatedInclude);
                Generate(filePath, DestinationFolder, this.project);
            }
        }

        public void Generate(string filePath, string htmlFilePath, Project project)
        {
            this.project = project;
            this.FilePath = filePath;
            this.project = project;
            Contract.Assert(project != null);

            var relativePath = Paths.MakeRelativeToFolder(filePath, ProjectSourceFolder);
            relativePath = relativePath.Replace("..", "parent");
            var destinationHtmlFile = Path.Combine(DestinationFolder, relativePath) + ".html";
            base.Generate(filePath, destinationHtmlFile, DestinationFolder);

            generator.OtherFiles.Add(relativePath);
        }

        protected override string GetDisplayName()
        {
            return FilePath;
        }

        protected override string GetAssemblyName()
        {
            return Path.GetFileNameWithoutExtension(project.FullPath);
        }
    }
}

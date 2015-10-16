using System;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Diagnostics.Contracts;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    // app.config, web.config generate
    public class ContentXmlSupport : XmlSupport, IExtend
    {
        public static string[] Extensions = new[] { ".config", ".xslt", ".xml" };

        public string FilePath { get; protected set; }
        public string DestinationFolder { get; set; }
        public Project project { get; private set; }

        public ContentXmlSupport(ProjectGenerator generator)
	    {
            FilePath = generator.ProjectFilePath;
            DestinationFolder = generator.ProjectDestinationFolder;
	    }
        
        public void ParseProject(Project project)
        { }


        public void Generate(string filePath, string htmlFilePath, Project project)
        {
            this.FilePath = filePath;
            this.project = project;
            Contract.Assert(project != null);

            var resultHtml = Path.Combine(DestinationFolder, Path.GetFileName(filePath)) + ".html";
            base.Generate(filePath, resultHtml, DestinationFolder);
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

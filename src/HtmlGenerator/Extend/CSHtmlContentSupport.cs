using Microsoft.Build.Evaluation;
using System.Collections.Generic;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    class CSHtmlContentSupport : IExtend
    {
        public string FilePath { get; protected set; }
        public string DestinationFolder { get; set; }
        public Project project { get; private set; }

        public CSHtmlContentSupport(ProjectGenerator generator)
        {
            FilePath = generator.ProjectFilePath;
            DestinationFolder = generator.ProjectDestinationFolder;
        }

        public void ParseProject(Project project, string extension) { }

        public void Generate(string filePath, string htmlFilePath, Project project)
        {
        }

        public static string[] Extensions = new[] { ".cshtml" };
    }

    class AspxContentSupport : IExtend
    {
        public string FilePath { get; protected set; }
        public string DestinationFolder { get; set; }
        public Project project { get; private set; }

        public AspxContentSupport(ProjectGenerator generator)
        {
            FilePath = generator.ProjectFilePath;
            DestinationFolder = generator.ProjectDestinationFolder;
        }

        public void ParseProject(Project project, string extension) { }

        public void Generate(string filePath, string htmlFilePath, Project project)
        {
        }

        public static string[] Extensions = new[] { ".aspx" };
    }

}
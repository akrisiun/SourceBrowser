using System;
using Microsoft.Build.Evaluation;
using System.IO;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    public interface IExtend
    {
        string FilePath { get; }
        string DestinationFolder { get; }
        Project project { get; }

        // void ParseProject(Project project, string extension);
        void Generate(string filePath, string htmlFilePath, Project project);
    }

    public static class ExtendGenerator
    {
        public static void GenerateConfig(this ProjectGenerator @this, Project msbuildProject)
        {
            var content = new ContentXmlSupport(@this);

            content.ParseProject(msbuildProject, ".config");
        }

        public static void GenerateContentFiles(this ProjectGenerator @this, Project msbuildProject)
        {
            var content = new ContentXmlSupport(@this);

            content.ParseRazorSrcFiles(msbuildProject);  // .cshtml

            content.ParseProject(msbuildProject,
                new string[] {".js", ".css",
                    ".xslt", ".txt", ".md", ".sql"});

            // TODO:
            // var cshtml = new CSHtmlContentSupport(@this);
            // var sql = new SqlContentSupport(@this);
        }
    }
}
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
            var content = new ContentXmlSupport(@this); //  (msbuildProject);

            //var expectFiles = new string[] { "app.config", "web.config", "web.Debug.config", "web.Release.config", 
            //    "Nuget.config", @"View\web.config" };
            //var folder = Path.GetDirectoryName(content.FilePath);

            //foreach (string file in expectFiles)
            //{
            //    var fullFile = Path.Combine(folder, file);
            //    if (!File.Exists(fullFile))
            //        continue;

            //    content.Generate(fullFile, content.DestinationFolder, msbuildProject);
            //}

            content.ParseProject(msbuildProject, ".config");
        }

        public static void GenerateContentFiles(this ProjectGenerator @this, Project msbuildProject)
        {
            //public void GenerateMdFiles(Project msbuildProject)
            //public void GenerateXTextFiles(Project msbuildProject)

            var content = new ContentXmlSupport(@this); //  (msbuildProject);
            content.ParseProject(msbuildProject, ".xslt");
            content.ParseProject(msbuildProject, ".txt");
            content.ParseProject(msbuildProject, ".md");

            content.ParseProject(msbuildProject, ".cshtml");
            content.ParseProject(msbuildProject, ".sql");

            // var cshtml = new CSHtmlContentSupport(@this);
            //var sql = new SqlContentSupport(@this);
        }
    }
}
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    public class SqlContentSupport  : IExtend
    {
        public string FilePath { get; protected set; }
        public string DestinationFolder { get; set; }
        public Project project { get; private set; }

        public SqlContentSupport(ProjectGenerator generator)
        {
            FilePath = generator.ProjectFilePath;
            DestinationFolder = generator.ProjectDestinationFolder;
        }

        public void ParseProject(Project project)
        { }

        public void Generate(string filePath, string htmlFilePath, Project project)
        {
        } 

        public static string[] Extensions = new[] { ".sql", ".dd" };

    }
}

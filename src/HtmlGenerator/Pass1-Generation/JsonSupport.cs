using System.IO;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class JsonSupport
    {
        private ProjectGenerator _projectGenerator;

        public JsonSupport(ProjectGenerator projectGenerator)
        {
            _projectGenerator = projectGenerator;

        }

        public void Generate(string projectFilePath, string destinationFileName)
        {
            using (var file = new FileStream(destinationFileName, FileMode.OpenOrCreate))
            using (var writer = new StreamWriter(file))
            {
                writer.WriteLine("<pre>");

                using (var input = new FileStream(projectFilePath, FileMode.Open))
                {
                    // TODO actual syntax highlighting
                    input.CopyTo(file);
                    writer.WriteLine("</pre>");
                    writer.Flush();
                }
            }
        }
    }
}
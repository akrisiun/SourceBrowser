using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class DocumentsList
    {
        public static IList<Document> Load(ProjectGenerator generator,
            string ProjectSourcePath,
            Project Project)
        {
            IList<Document> list = new List<Document>();

            //    var list = Project.Documents;
            //    var service = Project.LanguageServices;

            //if (info != null || infoWrap.Items.Count > 0) // && info.Documents.Count() > 0)
            //{
            //    Project = info;


            var listDocs = Project.Documents;
            var service = Project.LanguageServices;
            var wks = service.WorkspaceServices.Workspace;
            var csproj = Project as Microsoft.CodeAnalysis.Project;

            var serve = wks.Services;
            // Debugger.Break();
            var infoWrap = ProjectFileLoaderWrap.ProjectCs(csproj);
            var info = infoWrap?.Project;

            if (info != null || infoWrap.Items.Count > 0)
            {
                Project = info;

                var items = infoWrap.Items;
                var listDoc = new List<Document>();
                var csProj = Project;
                var dir = Path.GetDirectoryName(csProj.FilePath)
                    + Path.DirectorySeparatorChar.ToString();

                foreach (var msItem in items)
                {
                    var proj = msItem.Project;
                    string item1 = null;
                    if (msItem.ItemType == null
                        || (!msItem.ItemType.Equals("Content")
                            && !msItem.ItemType.Equals("Compile")
                            && !msItem.ItemType.Equals("None")
                        ))
                        continue;

                    try
                    {
                        //  var meta = msItem.Metadata as ICollection<Microsoft.Build.Evaluation.ProjectMetadata>;
                        item1 = msItem.EvaluatedInclude.Substring(1, 1) == ":" ? msItem.EvaluatedInclude
                              : dir + msItem.EvaluatedInclude;
                    }
                    catch { }
                    var ext = (Path.GetExtension(item1) ?? "").ToLowerInvariant();
                    if (item1 != null && File.Exists(item1)
                        && !".png".Equals(ext) && !".jpg".Equals(ext))
                    {
                        Document doc = Document.Load(csProj, item1);
                        list.Add(doc);
                    }
                }
            }

            return list;
        }
    }

    //public class ProjectFileLoaderWrap
    //{

    //}

}

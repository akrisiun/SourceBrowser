using System;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    public interface IExtend
    {
        string FilePath { get; }
        string DestinationFolder { get; }
        Project project { get; }

        void ParseProject(Project project, string extension);
        void Generate(string filePath, string htmlFilePath, Project project);
    }

    public static class ExtendGenerator
    {
        public static void GenerateConfig(this ProjectGenerator @this, Project msbuildProject)
        {
            var content = new ContentXmlSupport(@this);

            var solution = @this.SolutionGenerator;
            References.Instance.ParseProject(msbuildProject, solution);

            content.ParseProject(msbuildProject, ".config");
        }

        #region Solution assembly referencies

        public static void ProjectReferencesList(Microsoft.CodeAnalysis.Project project, SortedSet<string> list)
        {
            if (!Configuration.ProcessReferencies)
                return;

            var name = project.Name;
            var ref1 = References.Instance.refList.Where((el) => el.CsProj.StartsWith(name + "."));
            var refList = project.ProjectReferences;
            var metaRefList = project.MetadataReferences;
            var fedAssemblies = Federation.ReferenceSourceAssemblies()[0];

            foreach (var refItem in ref1)
            {
                if (string.IsNullOrWhiteSpace(refItem.HintPath))
                {
                    var nameSplit = refItem.Dll.Split(new char[] { ',' });
                    var refAsm = nameSplit[0];

                    var find = fedAssemblies.Where((el) => el.StartsWith(refAsm, StringComparison.InvariantCultureIgnoreCase));
                    if (find.Any())
                    {
                        list.Add(refAsm);
                    }
                    continue;       
                }

                string asm = Path.GetFileNameWithoutExtension(refItem.HintPath);
                var search = list.Where((a) => asm.StartsWith(a));
                if (search == null || !search.Any())
                {
                    list.Add(asm);
                }
            }

        }

        public static void GenerateContentFiles(this ProjectGenerator @this, Project msbuildProject)
        {
            var content = new ContentXmlSupport(@this);

            // TODO: var cshtml = new CSHtmlContentSupport(@this);
            content.ParseRazorSrcFiles(msbuildProject);  // .cshtml

            content.ParseProject(msbuildProject,
                new string[] {".js", ".css",
                    ".aspx", ".html", ".xslt", ".txt", ".md", ".sql"});

            //  TODO: var sql = new SqlContentSupport(@this);
        }

        //public static void ExternalReferencesPrepare(SolutionGenerator solutionGenerator, HashSet<string> assemblyList)
        //{
        //    References.Instance.ExternalReferencesPrepare(solutionGenerator, assemblyList);
        //}

        public static void ExternalReferences(SolutionGenerator solutionGenerator, HashSet<string> assemblyList)
        {
            References.Instance.ExternalReferences(solutionGenerator, assemblyList);
        }

        public static void TopReferencedAssemblies(SolutionGenerator solutionGenerator, Federation federation, 
            Folder<Microsoft.CodeAnalysis.Project> mergeFolders)
        {
            var filePath = Path.Combine(solutionGenerator.SolutionDestinationFolder, Constants.TopReferencedAssemblies + ".txt");
            if (!File.Exists(filePath))
                return;

            string[] lines = File.ReadAllLines(filePath);

            //var workspace = solutionGenerator.Workspace;
            //if (workspace != null && workspace.CurrentSolution != null)
            //{
            //    var sln = workspace.CurrentSolution;
            //    //foreach (Microsoft.CodeAnalysis.Project proj in sln.Projects)
            //    //{   proj.MetadataReferences 
            //}

            //foreach (string assembly in solutionGenerator.GlobalAssemblyList)
            //{
            //    string resultFile = Path.Combine(solutionGenerator.SolutionDestinationFolder, assembly + "\\" + assembly + ".csproj");
            //}

            //var asmList = References.Instance.refList;

        }


        public static void WriteReference(StringBuilder sb, string reference, string referenceItem,
              string url, HashSet<string> usedReferences, 
              SolutionGenerator SolutionGenerator)
        {

            if (SolutionGenerator.IsPartOfSolution(reference) && usedReferences.Contains(reference))
            {
                sb.AppendLine(Markup.GetProjectExplorerReference(url, reference));
            }
            else
            {
                // TODO external site
                sb.AppendLine("<span class=\"referenceDisabled\">" + referenceItem + "</span>");
            }
        }

        #endregion

        public static void Finalize(SolutionFinalizer solutionFinalizer, 
            Folder<Microsoft.CodeAnalysis.Project> mergedSolutionExplorerRoot, bool afterError = false)
        {
            if (afterError || !Configuration.ProcessAll)
            {
                var solutionExplorerRoot = mergedSolutionExplorerRoot;
                // var output = Path.Combine(solutionFinalizer.SolutionDestinationFolder, Constants.SolutionExplorer + ".html");
                solutionFinalizer.WriteSolutionExplorer(solutionExplorerRoot);

                try
                {
                    solutionFinalizer.CreateProjectMap();
                    solutionFinalizer.CreateReferencingProjectLists();
                }
                catch { } // not fatal error

                // DeployFilesToRoot(SolutionDestinationFolder);
                Markup.GenerateResultsHtml(solutionFinalizer.SolutionDestinationFolder);
            }

            //  MetadataReference CreateReferenceFromFilePath(string assemblyFilePath)
        }
    }
}
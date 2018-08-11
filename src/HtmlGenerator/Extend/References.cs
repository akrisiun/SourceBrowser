using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Microsoft.SourceBrowser.HtmlGenerator.Extend
{
    public class References
    {
        public static References Instance { get; private set; }
        static References()
        {
            Instance = new References();
            Instance.refList = new Collection<ProjectDll>();
        }
        
        #region Referencies list

        public struct ProjectDll
        {
            public string CsProj { get; set; }
            public string Dll { get; set; }
            public string HintPath { get; set; }

            public override string ToString()
            {
                return Dll;
            }
        }

        public ICollection<ProjectDll> refList { get; protected set; }

        #endregion

        #region External links federation

        public const string refConfig = "Referencies.config";
        public string RefConfigFile { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, refConfig); } }

        #endregion

        //public void ExternalReferencesPrepare(SolutionGenerator solutionGenerator, HashSet<string> assemblyList)
        //{
        //    Federation fed = solutionGenerator.Federation;
        //    // http://referencesource.microsoft.com/Assemblies.txt
        //    // var assemblyNames = new HashSet<string>(assemblyList
        //    //            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        //    //            .Select(line => line.Split(';')[0]), StringComparer.OrdinalIgnoreCase);
        //}

        public void ExternalReferences(SolutionGenerator solutionGenerator, HashSet<string> assemblyList) { }

        // .csproj parse
        public void ParseProject(Project project, SolutionGenerator generator = null)
        {
            var items = project.GetItems("Reference");
            var projName = Path.GetFileName(project.FullPath);

            foreach (ProjectItem item in items)
            {
                refList.Add(new ProjectDll { CsProj = projName, Dll = item.EvaluatedInclude, HintPath = item.GetMetadataValue("HintPath") });
            }
        }

        public void GetDlls(string projName, SortedSet<string> asmList)
        {
            var list = this.refList.Where((el) => el.CsProj.StartsWith(projName));
            foreach (var item in list)
            {
                var dll = Path.GetFileNameWithoutExtension(item.Dll);
                if (!string.IsNullOrWhiteSpace(dll))
                {
                    var find = asmList.Contains(dll);
                    if (!find)
                        asmList.Add(dll);
                }
            }

        }
    }
}

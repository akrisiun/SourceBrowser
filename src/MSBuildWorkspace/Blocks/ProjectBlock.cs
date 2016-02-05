// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.ProjectBlock
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal sealed class ProjectBlock
    {
        private Guid _projectTypeGuid;
        private readonly string _projectName;
        private readonly string _projectPath;
        private Guid _projectGuid;
        private readonly IEnumerable<SectionBlock> _projectSections;

        public Guid ProjectTypeGuid
        {
            get
            {
                return this._projectTypeGuid;
            }
        }

        public string ProjectName
        {
            get
            {
                return this._projectName;
            }
        }

        public string ProjectPath
        {
            get
            {
                return this._projectPath;
            }
        }

        public Guid ProjectGuid
        {
            get
            {
                return this._projectGuid;
            }
        }

        public IEnumerable<SectionBlock> ProjectSections
        {
            get
            {
                return this._projectSections;
            }
        }

        public ProjectBlock(Guid projectTypeGuid, string projectName, string projectPath, Guid projectGuid, IEnumerable<SectionBlock> projectSections)
        {
            if (string.IsNullOrEmpty(projectName))
                throw new ArgumentException(string.Format("StringIsNullOrEmpty type error", (object)"projectName"));
            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentException(string.Format("StringIsNullOrEmpty type error", (object)"projectPath"));
            this._projectTypeGuid = projectTypeGuid;
            this._projectName = projectName;
            this._projectPath = projectPath;
            this._projectGuid = projectGuid;
            this._projectSections = (IEnumerable<SectionBlock>)Enumerable.ToList<SectionBlock>(projectSections).AsReadOnly();
        }

        internal string GetText()
        {
            StringBuilder stringBuilder1 = new StringBuilder();
            StringBuilder stringBuilder2 = stringBuilder1;
            string format = "Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"";
            object[] objArray = new object[4];
            int index1 = 0;
            string str1 = this.ProjectTypeGuid.ToString("B").ToUpper();
            objArray[index1] = (object)str1;
            int index2 = 1;
            string projectName = this.ProjectName;
            objArray[index2] = (object)projectName;
            int index3 = 2;
            string projectPath = this.ProjectPath;
            objArray[index3] = (object)projectPath;
            int index4 = 3;
            string str2 = this.ProjectGuid.ToString("B").ToUpper();
            objArray[index4] = (object)str2;
            stringBuilder2.AppendFormat(format, objArray);
            stringBuilder1.AppendLine();
            foreach (SectionBlock sectionBlock in this._projectSections)
                stringBuilder1.Append(sectionBlock.GetText(1));
            stringBuilder1.AppendLine("EndProject");
            return ((object)stringBuilder1).ToString();
        }

        internal static ProjectBlock Parse(TextReader reader)
        {
            throw new NotImplementedException();
            //LineScanner lineScanner = new LineScanner(reader.ReadLine().TrimStart((char[])null));
            //string delimiter1 = "(\"";
            //if (lineScanner.ReadUpToAndEat(delimiter1) != "Project")
            //    throw new Exception(string.Format(WorkspacesResources.InvalidProjectBlockInSolutionFile4, (object)"Project"));
            //string delimiter2 = "\")";
            //Guid projectTypeGuid = Guid.Parse(lineScanner.ReadUpToAndEat(delimiter2));
            //string delimiter3 = "\"";
            //if (lineScanner.ReadUpToAndEat(delimiter3).Trim() != "=")
            //    throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile);
            //string delimiter4 = "\"";
            //string projectName = lineScanner.ReadUpToAndEat(delimiter4);
            //string delimiter5 = "\"";
            //if (lineScanner.ReadUpToAndEat(delimiter5).Trim() != ",")
            //    throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile2);
            //string delimiter6 = "\"";
            //string projectPath = lineScanner.ReadUpToAndEat(delimiter6);
            //string delimiter7 = "\"";
            //if (lineScanner.ReadUpToAndEat(delimiter7).Trim() != ",")
            //    throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile3);
            //string delimiter8 = "\"";
            //Guid projectGuid = Guid.Parse(lineScanner.ReadUpToAndEat(delimiter8));
            //List<SectionBlock> list = new List<SectionBlock>();
            //while (char.IsWhiteSpace((char)reader.Peek()))
            //    list.Add(SectionBlock.Parse(reader));
            //if (reader.Peek() != 80 && reader.Peek() != 71 && reader.ReadLine() != "EndProject")
            //    throw new Exception(string.Format(WorkspacesResources.InvalidProjectBlockInSolutionFile4, (object)"EndProject"));
            //else
            //    return new ProjectBlock(projectTypeGuid, projectName, projectPath, projectGuid, (IEnumerable<SectionBlock>)list);
        }
    }
}

// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.SolutionFile
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
    internal sealed class SolutionFile
    {
        private readonly IEnumerable<string> _headerLines;
        private readonly string _visualStudioVersionLineOpt;
        private readonly string _minimumVisualStudioVersionLineOpt;
        private readonly IEnumerable<ProjectBlock> _projectBlocks;
        private readonly IEnumerable<SectionBlock> _globalSectionBlocks;

        public IEnumerable<string> HeaderLines
        {
            get
            {
                return this._headerLines;
            }
        }

        public string VisualStudioVersionLineOpt
        {
            get
            {
                return this._visualStudioVersionLineOpt;
            }
        }

        public string MinimumVisualStudioVersionLineOpt
        {
            get
            {
                return this._minimumVisualStudioVersionLineOpt;
            }
        }

        public IEnumerable<ProjectBlock> ProjectBlocks
        {
            get
            {
                return this._projectBlocks;
            }
        }

        public IEnumerable<SectionBlock> GlobalSectionBlocks
        {
            get
            {
                return this._globalSectionBlocks;
            }
        }

        public SolutionFile(IEnumerable<string> headerLines, string visualStudioVersionLineOpt, string minimumVisualStudioVersionLineOpt, IEnumerable<ProjectBlock> projectBlocks, IEnumerable<SectionBlock> globalSectionBlocks)
        {
            if (headerLines == null)
                throw new ArgumentNullException("headerLines");
            if (projectBlocks == null)
                throw new ArgumentNullException("projectBlocks");
            if (globalSectionBlocks == null)
                throw new ArgumentNullException("globalSectionBlocks");
            this._headerLines = (IEnumerable<string>)Enumerable.ToList<string>(headerLines).AsReadOnly();
            this._visualStudioVersionLineOpt = visualStudioVersionLineOpt;
            this._minimumVisualStudioVersionLineOpt = minimumVisualStudioVersionLineOpt;
            this._projectBlocks = (IEnumerable<ProjectBlock>)Enumerable.ToList<ProjectBlock>(projectBlocks).AsReadOnly();
            this._globalSectionBlocks = (IEnumerable<SectionBlock>)Enumerable.ToList<SectionBlock>(globalSectionBlocks).AsReadOnly();
        }

        public string GetText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            foreach (string str in this._headerLines)
                stringBuilder.AppendLine(str);
            foreach (ProjectBlock projectBlock in this._projectBlocks)
                stringBuilder.Append(projectBlock.GetText());
            stringBuilder.AppendLine("Global");
            foreach (SectionBlock sectionBlock in this._globalSectionBlocks)
                stringBuilder.Append(sectionBlock.GetText(1));
            stringBuilder.AppendLine("EndGlobal");
            return ((object)stringBuilder).ToString();
        }

        public static SolutionFile Parse(TextReader reader)
        {
            List<string> list1 = new List<string>();
            string nextNonEmptyLine = SolutionFile.GetNextNonEmptyLine(reader);
            if (nextNonEmptyLine == null || !nextNonEmptyLine.StartsWith("Microsoft Visual Studio Solution File", StringComparison.Ordinal))
                throw new Exception(string.Format("MissingHeaderInSolutionFile", (object)"Microsoft Visual Studio Solution File"));
            list1.Add(nextNonEmptyLine);
            while (reader.Peek() != -1 && Enumerable.Contains<char>((IEnumerable<char>)"#\r\n", (char)reader.Peek()))
                list1.Add(reader.ReadLine());
            string visualStudioVersionLineOpt = (string)null;
            if (reader.Peek() == 86)
            {
                visualStudioVersionLineOpt = SolutionFile.GetNextNonEmptyLine(reader);
                if (!visualStudioVersionLineOpt.StartsWith("VisualStudioVersion", StringComparison.Ordinal))
                    throw new Exception(string.Format("MissingHeaderInSolutionFile", (object)"VisualStudioVersion"));
            }
            string minimumVisualStudioVersionLineOpt = (string)null;
            if (reader.Peek() == 77)
            {
                minimumVisualStudioVersionLineOpt = SolutionFile.GetNextNonEmptyLine(reader);
                if (!minimumVisualStudioVersionLineOpt.StartsWith("MinimumVisualStudioVersion", StringComparison.Ordinal))
                    throw new Exception(string.Format("MissingHeaderInSolutionFile", (object)"MinimumVisualStudioVersion"));
            }
            List<ProjectBlock> list2 = new List<ProjectBlock>();
        label_15:
            while (reader.Peek() == 80)
            {
                list2.Add(ProjectBlock.Parse(reader));
                while (true)
                {
                    if (reader.Peek() != -1 && Enumerable.Contains<char>((IEnumerable<char>)"#\r\n", (char)reader.Peek()))
                        reader.ReadLine();
                    else
                        goto label_15;
                }
            }
            IEnumerable<SectionBlock> globalSectionBlocks = SolutionFile.ParseGlobal(reader);
            if (reader.Peek() != -1)
                throw new Exception("MissingEndOfFileInSolutionFile");
            else
                return new SolutionFile((IEnumerable<string>)list1, visualStudioVersionLineOpt, minimumVisualStudioVersionLineOpt, (IEnumerable<ProjectBlock>)list2, globalSectionBlocks);
        }

        private static IEnumerable<SectionBlock> ParseGlobal(TextReader reader)
        {
            if (reader.Peek() == -1)
                return Enumerable.Empty<SectionBlock>();
            if (SolutionFile.GetNextNonEmptyLine(reader) != "Global")
                throw new Exception(string.Format("MissingLineInSolutionFile", (object)"Global"));
            List<SectionBlock> list = new List<SectionBlock>();
            while (reader.Peek() != -1 && char.IsWhiteSpace((char)reader.Peek()))
                list.Add(SectionBlock.Parse(reader));
            if (SolutionFile.GetNextNonEmptyLine(reader) != "EndGlobal")
                throw new Exception(string.Format("MissingLineInSolutionFile", (object)"EndGlobal"));
            while (reader.Peek() != -1 && Enumerable.Contains<char>((IEnumerable<char>)"\r\n", (char)reader.Peek()))
                reader.ReadLine();
            return (IEnumerable<SectionBlock>)list;
        }

        private static string GetNextNonEmptyLine(TextReader reader)
        {
            string str;
            do
            {
                str = reader.ReadLine();
            }
            while (str != null && str.Trim() == string.Empty);
            return str;
        }
    }
}

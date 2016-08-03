﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.SourceBrowser.Common;
using Folder = Microsoft.SourceBrowser.HtmlGenerator.Folder<string>;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public partial class ProjectGenerator
    {
        private void GenerateProjectExplorer()
        {
            Log.Write("Project Explorer...");
            var projectExplorerFile = Path.Combine(ProjectDestinationFolder, Constants.ProjectExplorer) + ".html";
            var sb = new StringBuilder();
            Markup.WriteProjectExplorerPrefix(sb, Project.AssemblyName);
            WriteDocuments(sb);
            WriteProjectStats(sb);
            Markup.WriteProjectExplorerSuffix(sb);
            File.WriteAllText(projectExplorerFile, sb.ToString());
        }

        private void WriteProjectStats(StringBuilder sb)
        {
            sb.AppendLine("<p class=\"projectInfo\">");

            var namedTypes = this.DeclaredSymbols.Keys.OfType<INamedTypeSymbol>();
            sb.AppendLine("Project&nbsp;path:&nbsp;" + ProjectSourcePath + "<br>");
            sb.AppendLine("Files:&nbsp;" + DocumentCount.WithThousandSeparators() + "<br>");
            sb.AppendLine("Lines&nbsp;of&nbsp;code:&nbsp;" + LinesOfCode.WithThousandSeparators() + "<br>");
            sb.AppendLine("Bytes:&nbsp;" + BytesOfCode.WithThousandSeparators() + "<br>");
            sb.AppendLine("Declared&nbsp;symbols:&nbsp;" + this.DeclaredSymbols.Count.WithThousandSeparators() + "<br>");
            sb.AppendLine("Declared&nbsp;types:&nbsp;" + namedTypes.Count().WithThousandSeparators() + "<br>");
            sb.AppendLine("Public&nbsp;types:&nbsp;" + namedTypes.Where(t => t.DeclaredAccessibility == Accessibility.Public).Count().WithThousandSeparators() + "<br>");
            sb.AppendLine("Indexed&nbsp;on:&nbsp;" + DateTime.Now.ToString("MMMM dd"));

            sb.AppendLine("</p>");
        }

        private void WriteDocuments(StringBuilder sb)
        {
            Folder root = new Folder();
            root.Name = Project.Name;

            foreach (var otherFile in OtherFiles)
            {
                var parts = otherFile.Split('\\');
                AddDocumentToFolder(root, otherFile, parts.Take(parts.Length - 1).ToArray());
            }

            root.Sort((l, r) => Path.GetFileName(l).CompareTo(Path.GetFileName(r)));
            WriteRootFolder(root, sb);
        }

        private void WriteRootFolder(Folder folder, StringBuilder sb)
        {
            string className = IsCSharp ?
                "projectCS" :
                "projectVB";
            sb.AppendFormat(
                "<div id=\"rootFolder\" class=\"{0}\">{1}</div>",
                className,
                folder.Name);
            sb.AppendLine("<div>");

            Folder properties = null;
            if (folder.Folders != null && folder.Folders.TryGetValue("Properties", out properties))
            {
                WriteFolder(properties, sb);
                folder.Folders.Remove("Properties");
            }

            var assemblyNames = new SortedSet<string>();
            if (Project.ProjectReferences.Any() || Project.MetadataReferences.Any())
                FillReferencies(assemblyNames);

            string projName = folder.Name;
            Extend.References.Instance.GetDlls(projName, assemblyNames);

            if (assemblyNames.Any())
                WriteReferences(sb, assemblyNames);

            WriteFolders(folder, sb);
            WriteDocuments(folder, sb);
            sb.AppendLine("</div>");
        }

        private void FillReferencies(SortedSet<string> assemblyNames)
        {
            foreach (var projectReference in Project.ProjectReferences.Select(p => Project.Solution.GetProject(p.ProjectId).AssemblyName))
            {
                assemblyNames.Add(projectReference);
            }

            foreach (var metadata in Project.MetadataReferences)
            {
                if (metadata.Display.StartsWith("<"))
                    continue;   // <in memory

                var metadataReference = Path.GetFileNameWithoutExtension(metadata.Display);
                assemblyNames.Add(metadataReference);
            }
        }

        private void WriteReferences(StringBuilder sb, SortedSet<string> assemblyNames)
        {
            sb.Append("<div class=\"folderTitle\">References</div>");
            sb.AppendLine("<div class=\"folder\">");

            var usedReferences = new HashSet<string>(this.UsedReferences, StringComparer.OrdinalIgnoreCase);
            foreach (string referenceItem in assemblyNames)
            {
                string reference = referenceItem;
                if (reference.Contains(","))
                    reference = reference.Split(new[] { ',' })[0];

                var externalIndex = this.SolutionGenerator.GetExternalAssemblyIndex(reference);
                string url = "/#" + reference;
                if (externalIndex != -1)        // Federation assembly
                {
                    url = "@" + externalIndex.ToString() + "@#" + reference;
                    sb.AppendLine(Markup.GetProjectExplorerReference(url, reference));
                }
                else
                {
                    Extend.ExtendGenerator.WriteReference(sb, reference, referenceItem, 
                        url, usedReferences, SolutionGenerator);
                }
            }

            sb.AppendLine("</div>");
        }

        private void WriteFolder(Folder folder, StringBuilder sb)
        {
            WriteFolderName(folder, sb);
            sb.AppendLine("<div class=\"folder\">");
            WriteFolders(folder, sb);
            WriteDocuments(folder, sb);
            sb.AppendLine("</div>");
        }

        private void WriteFolders(Folder folder, StringBuilder sb)
        {
            if (folder.Folders != null)
            {
                foreach (var subfolder in folder.Folders.Values)
                {
                    WriteFolder(subfolder, sb);
                }
            }
        }

        private void WriteDocuments(Folder folder, StringBuilder sb)
        {
            if (folder.Items != null)
            {
                foreach (var document in folder.Items)
                {
                    WriteDocument(folder, document, sb);
                }
            }
        }

        private void WriteFolderName(Folder folder, StringBuilder sb)
        {
            sb.Append("<div class=\"folderTitle\">" + folder.Name + "</div>");
        }

        private void WriteDocument(Folder folder, string document, StringBuilder sb)
        {
            string hyperlink = GetHyperlink(document);
            sb.AppendFormat(
                "<a href=\"{0}\"></a>",
                hyperlink);
            sb.AppendLine();
        }

        private string GetHyperlink(string document)
        {
            if (document.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
            {
                var fullPath = Path.Combine(Path.GetDirectoryName(this.ProjectFilePath), document);
                var destination = TypeScriptSupport.GetDestinationFilePath(fullPath);
                destination = destination.Substring(this.SolutionGenerator.SolutionDestinationFolder.Length + 1);
                destination = destination.Replace('\\', '/');
                destination = "/" + destination;
                return destination;
            }

            string localPath = document + ".html";
            localPath = localPath.Replace('\\', '/');
            return localPath;
        }

        private void AddDocumentToFolder(Folder folder, string document, string[] subfolders)
        {
            if (subfolders == null || subfolders.Length == 0)
            {
                folder.Add(document);
                return;
            }

            if (subfolders[0].EndsWith(":"))
            {
                return;
            }

            var folderName = Paths.SanitizeFolder(subfolders[0]);
            Folder subfolder = folder.GetOrCreateFolder(folderName);
            AddDocumentToFolder(subfolder, document, subfolders.Skip(1).ToArray());
        }
    }
}
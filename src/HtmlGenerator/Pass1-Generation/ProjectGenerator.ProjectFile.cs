using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.SourceBrowser.Common;
using Microsoft.SourceBrowser.HtmlGenerator.Extend;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public partial class ProjectGenerator
    {
        private Project msbuildProject;
        public Project BuildProject { get { return msbuildProject; } }

        public void GenerateProjectFile()
        {
            var projectExtension = Path.GetExtension(ProjectFilePath);
            if (!File.Exists(ProjectFilePath) ||
                ".dll".Equals(projectExtension, StringComparison.OrdinalIgnoreCase) ||
                ".winmd".Equals(projectExtension, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ProjectCollection projectCollection = null;

            try
            {
                var title = Path.GetFileName(ProjectFilePath);
                projectCollection = new ProjectCollection();

                GenerateMsBuildProject(projectCollection);
                ExtendGenerator.GenerateConfig(this, msbuildProject); // .config

                if (Configuration.ProcessContent)
                {
                    // process content files
                    GenerateXamlFiles(msbuildProject);  // .xaml

                    GenerateTypeScriptFiles(msbuildProject);   // .ts

                    ExtendGenerator.GenerateContentFiles(this, msbuildProject); // .md, .cshtml, .aspx, .xml, .xslt, .json, .sql
                }

                OtherFiles.Add(title);
            }
            catch (Exception ex)
            {
                Log.Exception("Exception during Project file generation: " + ProjectFilePath + "\r\n" + ex.ToString());
            }
            finally
            {
                if (projectCollection != null)
                {
                    projectCollection.UnloadAllProjects();
                    projectCollection.Dispose();
                }
            }
        }

        public void GenerateMsBuildProject(ProjectCollection projectCollection)
        {
            var title = Path.GetFileName(ProjectFilePath);
            var destinationFileName = Path.Combine(ProjectDestinationFolder, title) + ".html";

            AddDeclaredSymbolToRedirectMap(SymbolIDToListOfLocationsMap, SymbolIdService.GetId(title), title, 0);

            // ProjectCollection caches the environment variables it reads at startup
            // and doesn't re-get them later. We need a new project collection to read
            // the latest set of environment variables.
            this.msbuildProject = new Project(
                ProjectFilePath,
                null,
                null,
                projectCollection,
                ProjectLoadSettings.IgnoreMissingImports);

            var msbuildSupport = new MSBuildSupport(this);
            msbuildSupport.Generate(ProjectFilePath, destinationFileName, msbuildProject, true);
        }

        private void GenerateTypeScriptFiles(Project msbuildProject)
        {
            var typeScriptCompileItems = msbuildProject.GetItems("TypeScriptCompile");
            foreach (var typeScriptFile in typeScriptCompileItems)
            {
                var filePath = typeScriptFile.EvaluatedInclude;
                GenerateTypeScriptFile(filePath);
            }
        }

        private void GenerateTypeScriptFile(string filePath)
        {
            var projectSourceFolder = Path.GetDirectoryName(ProjectFilePath);
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(Path.GetDirectoryName(ProjectFilePath), filePath);
            }

            SolutionGenerator.AddTypeScriptFile(filePath);

            var relativePath = Paths.MakeRelativeToFile(filePath, ProjectFilePath);
            relativePath = relativePath.Replace("..", "parent");
            OtherFiles.Add(relativePath);
        }

        private void GenerateXamlFiles(Project msbuildProject)
        {
            var xamlItems = msbuildProject.GetItems("Page").Concat(msbuildProject.GetItems("ApplicationDefinition"));
            foreach (var xamlItem in xamlItems)
            {
                var xamlFile = xamlItem.EvaluatedInclude;
                GenerateXamlFile(xamlFile);
            }
        }

        public void GenerateXamlFile(string xamlFile)
        {
            var projectSourceFolder = Path.GetDirectoryName(ProjectFilePath);
            if (!Path.IsPathRooted(xamlFile))
            {
                xamlFile = Path.Combine(Path.GetDirectoryName(ProjectFilePath), xamlFile);
            }

            xamlFile = Path.GetFullPath(xamlFile);
            var sourceXmlFile = xamlFile;

            if (!File.Exists(sourceXmlFile))
            {
                return;
            }

            var relativePath = Paths.MakeRelativeToFolder(sourceXmlFile, projectSourceFolder);
            relativePath = relativePath.Replace("..", "parent");

            var destinationHtmlFile = Path.Combine(ProjectDestinationFolder, relativePath) + ".html";

            var xamlSupport = new XamlSupport(this);
            xamlSupport.GenerateXaml(sourceXmlFile, destinationHtmlFile, relativePath);

            OtherFiles.Add(relativePath);
            AddDeclaredSymbolToRedirectMap(SymbolIDToListOfLocationsMap, SymbolIdService.GetId(relativePath), relativePath, 0);
        }

    }
}

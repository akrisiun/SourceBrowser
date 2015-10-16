using Microsoft.Build.Evaluation;
using Microsoft.SourceBrowser.HtmlGenerator.Extend;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SourceBrowser.HtmlGenerator.Tests
{
    [TestClass]
    public class XmlFilesTests
    {
        string TestSolution { get { return Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\..\TestCode\TestSolution.sln"); } }
        // SourceBrowser\TestCode\TestSolution
        string TestFolder1 { get { return System.AppDomain.CurrentDomain.BaseDirectory + @"\..\TestCode\TestSolution"; } }
        string OutputFolder { get { return System.AppDomain.CurrentDomain.BaseDirectory + @"\output"; } }

        static List<string> projects = new List<string>();
        static ProjectGeneratorWrap gen;

        [TestInitialize]
        public void Init()
        {
            var sln = TestSolution;
            Assert.IsTrue(File.Exists(sln));
            projects.Add(TestSolution);

            var testDir1 = Path.GetFullPath(TestFolder1);
            var output = Path.GetFullPath(OutputFolder);
            string TestProject1 = testDir1 + @"\TestSolution.csproj";
            Assert.IsTrue(File.Exists(TestProject1));
            
            gen = new ProjectGeneratorWrap();
            gen.Prepare(testDir1, TestProject1, output);

            var outputHtml = gen.DestinationFileName;

            var msbuildProject = gen.msbuildProject;
            var msbuildSupport = gen.msbuildSupport;
            msbuildSupport.Generate(gen.ProjectFilePath, outputHtml, msbuildProject, true);
            Assert.IsTrue(File.Exists(outputHtml));
        }

        [TestMethod]
        public void ExtendTest_XmlConfig()
        {
            ExtendGenerator.GenerateConfig(gen.ProjectGenerator, gen.msbuildProject);
        }

        [TestMethod]
        public void ExtendTest_Xslt()
        {
            var projGen = gen.ProjectGenerator;
            var content = new ContentXmlSupport(projGen);
            content.ParseProject(gen.msbuildProject, ".xslt");
        }

        [TestMethod]
        public void ExtendTest_SqlContent()
        {
            ExtendGenerator.GenerateConfig(gen.ProjectGenerator, gen.msbuildProject);
            // void GenerateContentFiles(this ProjectGenerator @this, Project msbuildProject)
        }
    }

    public class ProjectGeneratorWrap
    {
        public ProjectGenerator ProjectGenerator { [DebuggerStepThrough] get; set; }
        public ProjectCollection ProjectCollection { [DebuggerStepThrough] get; set; }
        public Project msbuildProject { [DebuggerStepThrough] get; set; }
        public MSBuildSupport msbuildSupport { [DebuggerStepThrough] get; set; }

        public string ProjectFilePath { [DebuggerStepThrough] get { return ProjectGenerator.ProjectFilePath; } }

        public string OutputDir { [DebuggerStepThrough] get; set; }
        public string Title { [DebuggerStepThrough] get; private set; }
        public string DestinationFileName { [DebuggerStepThrough] get; set; }

        public ProjectGeneratorWrap Prepare(string testDir1, string TestProject1, string output)
        {
            this.OutputDir = output;
            var gen = new ProjectGenerator(testDir1, output);
            ProjectGenerator = gen;

            gen.SetProjectFilePath(TestProject1);
            gen.SetSolutionGenerator(new SolutionGenerator(TestProject1, output));
            Assert.IsTrue(File.Exists(gen.ProjectFilePath));
            Assert.IsTrue(File.Exists(gen.SolutionGenerator.ProjectFilePath));

            // the latest set of environment variables.
            var projectCollection = new ProjectCollection();
            this.msbuildProject = new Project(
                gen.ProjectFilePath,
                null,
                null,
                projectCollection,
                ProjectLoadSettings.IgnoreMissingImports);

            this.msbuildSupport = new MSBuildSupport(gen);

            Title = Path.GetFileNameWithoutExtension(gen.ProjectFilePath);
            DestinationFileName = Path.Combine(gen.ProjectDestinationFolder, Title) + ".html";
            return this;
        }
    }
}

using Microsoft.Build.Evaluation;
using Microsoft.SourceBrowser.HtmlGenerator.Extend;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SourceBrowser.Common;

namespace Microsoft.SourceBrowser.HtmlGenerator.Tests
{
    [TestClass]
    public class XmlFilesTests
    {
        string TestSolution { get { return Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\..\TestCode\TestSolution.sln"); } }
        string TestProject1 { get { return "TestSolution.csproj"; } }
        // SourceBrowser\TestCode\TestSolution
        string TestFolder1 { get { return System.AppDomain.CurrentDomain.BaseDirectory + @"\..\TestCode\TestSolution"; } }
        string OutputFolder { get { return System.AppDomain.CurrentDomain.BaseDirectory + @"\output"; } }

        static ProjectGeneratorWrap gen;

        [TestInitialize]
        public void Init()
        {
            var sln = TestSolution;
            Assert.IsTrue(File.Exists(sln));

            var testDir1 = Path.GetFullPath(TestFolder1);
            var output = Path.GetFullPath(OutputFolder);
            string sourceTestProject1 = testDir1 + @"\" + TestProject1;
            Assert.IsTrue(File.Exists(sourceTestProject1));

            var outputProject1 = output + sourceTestProject1;
            gen = new ProjectGeneratorWrap()
                .Prepare(testDir1, sourceTestProject1, output)
                .MsbuildGenerate(outputProject1);

            Assert.IsTrue(File.Exists(gen.DestinationFileName));
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
        public void ExtendTest_ContentFiles()
        {
            ExtendGenerator.GenerateContentFiles(gen.ProjectGenerator, gen.msbuildProject);
        }
    }

    [TestClass]
    public class ExtendProject3_Tests
    {
        // SourceBrowser\TestCode\Project3\Project3.csproj, Solution3.sln
        // string TestSolution { get { return Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\..\TestCode\TestSolution.sln"); } }
        string TestProject3 { get { return "Project3.csproj"; } }
        string TestFolder3 { get { return Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\..\..\TestCode\Project3"); } }
        string OutputFolder { get { return Path.GetFullPath(System.AppDomain.CurrentDomain.BaseDirectory + @"\output"); } }
        static ProjectGeneratorWrap gen;

        [TestInitialize]
        public void Init()
        {
            var testDir = (TestFolder3); var output = (OutputFolder);
            string sourceTestProject1 = testDir + @"\" + TestProject3;
            Assert.IsTrue(File.Exists(sourceTestProject1));

            var outputProject1 = output + sourceTestProject1;
            gen = new ProjectGeneratorWrap();
            gen.Prepare(testDir, sourceTestProject1, output);
            gen.MsbuildGenerate(outputProject1);
        }

        [TestMethod]
        public void ExtendTest3_ContentCsHtml()
        {
            var msbuildProject = gen.msbuildProject;
            var content = new ContentXmlSupport(gen.ProjectGenerator);
            content.ParseRazorSrcFiles(msbuildProject, ".cshtml");
        }

        [TestMethod]
        public void ExtendTest3_ContentFiles()
        {
            // .csproj
            var csproj = new MSBuildSupport(gen.ProjectGenerator);
            string sourceTestProject = TestFolder3 + @"\" + TestProject3;
            csproj.Generate(sourceTestProject, gen.DestinationFileName, gen.ProjectGenerator.ProjectDestinationFolder);

            ExtendGenerator.GenerateConfig(gen.ProjectGenerator, gen.msbuildProject);
            ExtendGenerator.GenerateContentFiles(gen.ProjectGenerator, gen.msbuildProject);
        }

        [TestMethod]
        [Ignore]
        public void ExtendTest3_All()
        {
            Configuration.ProcessAll = true;
            var projects = gen.projects;
            var properties = gen.properties;

            Paths.SolutionDestinationFolder = gen.OutputDir;
            var SolutionDestinationFolder = Paths.SolutionDestinationFolder;
            if (Directory.Exists(SolutionDestinationFolder))
            {
                Log.Write("Deleting " + SolutionDestinationFolder);
                Directory.Delete(SolutionDestinationFolder, recursive: true);
            }
            Directory.CreateDirectory(SolutionDestinationFolder);

            using (Disposable.Timing("Generating website all"))
            {
                Program.IndexSolutions(projects, properties);
                Program.FinalizeProjects();
            }
        }

        [TestMethod]
        public void ExtendTest3_FastCsProj_ContentFiles()
        {
            var projects = gen.projects;
            var properties = gen.properties;

            Paths.SolutionDestinationFolder = gen.OutputDir;
            // PrepareDestinationFolder
            Directory.CreateDirectory(Paths.SolutionDestinationFolder);

            Configuration.ProcessAll = false;
            using (Disposable.Timing("Generating website fast"))
            {
                Program.IndexSolutions(projects, properties);
                Program.FinalizeProjects();
            }
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

        public List<string> projects = new List<string>();
        public Dictionary<string, string> properties = new Dictionary<string, string>();

        public ProjectGeneratorWrap Prepare(string testDir1, string TestProject1, string output)
        {
            this.OutputDir = output;
            var gen = new ProjectGenerator(testDir1, output);
            ProjectGenerator = gen;

            gen.SetProjectFilePath(TestProject1);
            gen.SetSolutionGenerator(new SolutionGenerator(TestProject1, output));
            Assert.IsTrue(File.Exists(gen.ProjectFilePath));
            Assert.IsTrue(File.Exists(gen.SolutionGenerator.ProjectFilePath));

            projects.Add(TestProject1);

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


        public ProjectGeneratorWrap MsbuildGenerate(string DestinationFileName = null)
        {
            var gen = this;
            var outputHtml = gen.DestinationFileName;
            var msbuildProject = gen.msbuildProject;
            var msbuildSupport = gen.msbuildSupport;

            msbuildSupport.Generate(gen.ProjectFilePath, outputHtml, msbuildProject, true);
            return this;
        }
    }
}

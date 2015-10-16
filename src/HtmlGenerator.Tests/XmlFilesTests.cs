using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        [TestInitialize]
        public void Init()
        {
            var sln = TestSolution;
            Assert.IsTrue(File.Exists(sln));
            projects.Add(TestSolution);
        }

        [TestMethod]
        public void TestXml()
        {
            var testDir1 = Path.GetFullPath(TestFolder1);
            var output = Path.GetFullPath(OutputFolder);
            string TestProject1 = testDir1 + @"\TestSolution.csproj";
            Assert.IsTrue(File.Exists(TestProject1));
            var gen = new ProjectGenerator(testDir1, output).SetProjectFilePath(TestProject1);
            Assert.IsTrue(File.Exists(gen.ProjectFilePath));
            
            // gen.GenerateXamlFile();
            gen.GenerateProjectFile();
            var proj = gen.BuildProject;

        }

        [TestMethod]
        public void TestXslt()
        {

        }
    }
}

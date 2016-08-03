using System;
using System.Diagnostics;
using System.IO;
using Microsoft.SourceBrowser.Common;
using Microsoft.SourceBrowser.HtmlGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.SourceBrowser.HtmlGenerator.Tests
{
    [TestClass]
    public class TestJsonProject
    {
        [TestMethod]
        public void TestJsonProject1()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory + "../../../";
            dir = Path.GetFullPath(dir);
            var newDir = dir + @"TestCode\JsonProject\";
            string baseDir = dir + @"bin\Debug\HtmlGenerator\";

            Directory.SetCurrentDirectory(newDir);

            List<string> projectsList = new List<string>();

            Program.AddProjectSafe(projectsList, "project.json");

            var properties = new Dictionary<string, string>();

            projectsList.Add(newDir + "project.json");

            Paths.SolutionDestinationFolder = dir + "srcweb";
            Configuration.ProcessAll = true;
            //Configuration.WriteProjectAuxiliaryFilesToDisk = true;

            Exception lastError = null;
            try
            {
                Program.IndexSolutions(projectsList, properties, baseDir);
                Program.FinalizeProjects();
            }
            catch (Exception ex)
            {
                Debugger.Log(0, "Json", ex.Message);
                lastError = ex;
            }

            Assert.IsNull(lastError);
            //Task Generate()
        }
    }
}

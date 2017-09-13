using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SourceBrowser.HtmlGenerator;
using System.IO;
using System.Reflection;
using Microsoft.SourceBrowser.Common;
using System.Diagnostics;

namespace HtmlGenerator.Tests
{
    [TestClass]
    public class GenTest
    {
        [TestMethod]
        public void Run()
        {
            string[] args = new[] { "HtmlGenerator.exe", "SourceBrowser.sln", "web4/Index" };
            var dir = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\..\";
            var path = Path.GetFullPath(dir);
            if (File.Exists(args[1]))
                path = Directory.GetCurrentDirectory() + @"\";

            args[1] = path + args[1];
            Assert.IsTrue(File.Exists(args[1]));
            args[2] = "/out:" + path + args[2];

            Log.WriteWrap = (str) 
                => Debug.WriteLine(str);

            Log.WriteWrap($"Testing {args[1]}");

            var asmDir = AppDomain.CurrentDomain.BaseDirectory + @"\";
            var asm1 = Assembly.LoadFile(asmDir + "System.IO.FileSystem.dll");
            var asm2 = Assembly.LoadFile(asmDir + "System.Collections.Immutable.dll");
            var asm3 = Assembly.LoadFile(asmDir + "System.ValueTuple.dll");

            var p = Program.ParseArgs(args);
            p.Prepare(true);

            p.Run(true);
        }
    }
}

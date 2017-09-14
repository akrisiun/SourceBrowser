using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SourceBrowser.HtmlGenerator;
using System.IO;
using System.Reflection;
using Microsoft.SourceBrowser.Common;
using System.Diagnostics;

namespace HtmlGenerator.Tests
{
    using TraceOut;

    [TestClass]
    public class GenTest
    {
        [TestMethod]
        public void _Run_1()
        {
            string[] args = new[] { "HtmlGenerator.exe", "SourceBrowser.sln", "web4/Index" };
            var dir = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\..\";
            var path = Path.GetFullPath(dir);
            if (File.Exists(args[1]))
                path = Directory.GetCurrentDirectory() + @"\";

            TestGen(args, path);

            Console.WriteLine($"Finished {path}");
            Console.WriteLine($"SLN      {args[1]}");
            Console.WriteLine($"/Out     {args[2]}");
            var err = SolutionGenerator.Errors;
            Assert.IsTrue(false);
        }

        [TestMethod]
        public void _Run_TestCode()
        {
            // \src2\master\SourceBrowser\TestCode\TestSolution.sln
            string[] args = new[] { "HtmlGenerator.exe", @"TestCode\TestSolution.sln", "webTest/Index" };
            var dir = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\..\..\";
            var path = Path.GetFullPath(dir);
            if (File.Exists(args[1]))
                path = Directory.GetCurrentDirectory() + @"\";

            TestGen(args, path);

            Console.WriteLine($"Finished {path}");
            Console.WriteLine($"SLN      {args[1]}");
            Console.WriteLine($"/Out     {args[2]}");

            var err = SolutionGenerator.Errors;
            Assert.IsTrue(false);
        }


        public void TestGen(string[] args, string path = null)
        {
            args[1] = path + args[1];
            var sln = args[1];
            Assert.IsTrue(File.Exists(sln));

            path = path ?? Directory.GetCurrentDirectory();
            args[2] = "/out:" + (path + args[2]).Replace("/", @"\");

            Log.WriteWrap = (str)
                => Debug(str);

            Program.Assert = false;
            // AssertTraceListener.Register();
            Listener.Register();

            Console.WriteLine($"base= {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine(path);
            Console.WriteLine(args[1]);
            Console.WriteLine(args[2]);
            Console.WriteLine($"Testing {args[1]}");

            var asmDir = AppDomain.CurrentDomain.BaseDirectory + @"\";
            var asm1 = Assembly.LoadFile(asmDir + "System.IO.FileSystem.dll");
            var asm2 = Assembly.LoadFile(asmDir + "System.Collections.Immutable.dll");
            var asm3 = Assembly.LoadFile(asmDir + "System.ValueTuple.dll");
            // System.IO.FileSystem 4.0.3
            // System.IO.FileSystem.Primitives 4.0.3
            var asm4 = Assembly.LoadFile(asmDir + "System.IO.FileSystem.dll");
            Console.WriteLine(asm4.ToString());
            var asm5 = Assembly.LoadFile(asmDir + "System.IO.FileSystem.Primitives.dll");
            Console.WriteLine(asm5.ToString());

            // "System.Security.Cryptography.Primitives" Version="4.0.3" 0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a
            var asm6 = Assembly.LoadFile(asmDir + "System.Security.Cryptography.Primitives.dll");
            Console.WriteLine(asm6.ToString());

            var p = Program.ParseArgs(args);
            p.Prepare(true);

            p.Run(true);
        }

        public static void Debug(string str)
        {
            // F9 ?
            global::System.Diagnostics.Debug
               .WriteLine(str);
        }
    }
}
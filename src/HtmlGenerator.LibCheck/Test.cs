using LibCheck;
using Microsoft;
using Microsoft.SourceBrowser.HtmlGenerator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HtmlGenerator.Check
{
    public class Test
    {
        public static void Main()
        {
            var t = new Caller();

            if (File.Exists("list.txt"))
                t.Run();

            t.DllSolution();
        }


        public static void LoadAssemblies()
        {
            // Lets prevent strange dotnet runtime errors, when .exe called from other folder

            var dlls = new[] {
                "System.Threading.Thread.dll",
                "System.IO.FileSystem.dll",
                "Newtonsoft.Json.dll",
                "System.Collections.Immutable.dll",
                "System.Composition.TypedParts.dll",
                "System.AppContext.dll",
                "Microsoft.CodeAnalysis.CSharp.dll",
                "Microsoft.Build.dll",
                "Microsoft.Build.Framework.dll",
                "Microsoft.CodeAnalysis.dll",
                "Microsoft.CodeAnalysis.Workspaces.dll"
                // LOG: DisplayName = System.Composition.TypedParts, Version = 1.0.27.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a
                // Calling assembly : Microsoft.CodeAnalysis.Workspaces, Version = 2.0.0.0, Culture = neutral, PublicKeyToken = 31bf3856ad364e35.
                // .Mef.MefHostServices.Create(IEnumerable`1 assemblies)
                //  at Microsoft.SourceBrowser.HtmlGenerator.WorkspaceHacks..cctor()
            };

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var file in dlls)
            {
                var path = Path.Combine(baseDir, file);
                Console.WriteLine($"Loading {path}");
                Assembly.LoadFile(path);
            }

            var fileXml = "Microsoft.Language.Xml.dll";
            var pathXml = Path.Combine(baseDir, fileXml);
            var asm = Assembly.LoadFile(pathXml);
            var type =      // make really loaded that assembly
                asm.GetType("Microsoft.Language.Xml.ErrorFactory");
        }

    }

    public class Caller
    {
        public void DllSolution()
        {
            if (!Debugger.IsAttached)
                return;   // just for debugging

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var folder = Path.GetFullPath(baseDir + @"\..\..\webfolder");
            Directory.SetCurrentDirectory(folder);

            WorkspaceHacks.Prepare();

            //  ProgramLoader.Main(new[] { "-y", @"Newtonsoft.Json\Json.csproj", "/out:index" });

            Tests.Run.MainCheck(@"..\src\SourceIndexServer\SourceIndexServer.csproj", new [] { "/out:index2" } );
                // new[] { "-y", @"..\src\SourceIndexServer\SourceIndexServer.csproj", "/out:index2" });

            ProgramLoader.Main(new[] { "-y", "SourceBrowser.Src.sln", "/out:index" });
        }

        public void Process()
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = "-debug -y SourceBrowser.Src.sln /out:index",
                    FileName =
                    @"d:\Beta\webstack\Mvc\SourceBrowser\src\HtmlGenerator\bin\Debug\net46\HtmlGenerator.exe"
                }
            };
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.CreateNoWindow = false;

            proc.Start();
            proc.WaitForExit();
        }

        public void Run()
        {
            var asm = typeof(LibChk).Assembly;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            //  ProgramLoader.LoadAssemblies();
            var dllsDefault = new[] { "Newtonsoft.Json.dll", "System.Buffers.dll", "System.AppContext.dll", "System.Reflection.Metadata.dll"
                , "System.ValueTuple.dll", "System.Collections.Immutable.dll"
                , "System.Diagnostics.StackTrace.dll", "System.IO.dll", "System.Composition.AttributedModel.dll", "System.Composition.TypedParts.dll",
                  "Microsoft.Build.dll", "Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.Workspaces.dll" };

            string[] dlls = dllsDefault;
            if (File.Exists("list.txt"))
                dlls = System.IO.File.ReadAllLines("list.txt");

            var white = Console.ForegroundColor;
            var listDll = new List<string>();

            foreach (var file in dlls)
            {
                var path = Path.Combine(baseDir, file);
                Console.WriteLine($"Loading {path}");
                try
                {
                    Assembly.LoadFile(path);

                    listDll.Add(path);
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" failed");
                    Console.ForegroundColor = white;
                }
            }

            var folder = Environment.CurrentDirectory;
            if (!File.Exists("list.txt") && !File.Exists("webfolder"))
                folder = Path.GetFullPath(baseDir + @"\..\..\webfolder");
            Directory.SetCurrentDirectory(folder);

            // libcheck.exe - store full src -file Microsoft.CodeAnalysis * -full % dir %
            String[] args = new[] { "-store", "full", "src", "-fill", folder };
            // LibChk.Main(args);
            LibChk.Prepare();
            LibChk.byDll = true;

            foreach (var dllFile in listDll)
            {
                var name = Path.GetFileName(dllFile);
                Console.WriteLine($"Src {name}");

                try
                {
                    LibChk.OneAssembly(dllFile, true);
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" failed");
                    Console.ForegroundColor = white;
                }
            }
        }
    }
}
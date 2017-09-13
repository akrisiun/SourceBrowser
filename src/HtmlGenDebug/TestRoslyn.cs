using HtmlGenDebug.Gen;
using System;

namespace HtmlGenDebug
{
    // dotnet add package SQLitePCLRaw.bundle_e_sqlite3 --version 1.1.8 

    public class Load
    {
        [STAThread]
        public static void Main() { new TestSourceBrowser().Debug(); }
    }
}

namespace HtmlGenDebug.Gen
{
    using Microsoft;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.SourceBrowser.HtmlGenerator;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    public class TestSourceBrowser
    {
        // [TestMethod]
        public void Debug()
        {
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.GetFullPath(appDir + @"\..\..\..") + @"\";

            var sln = dir + "SourceBrowser.sln";

            Console.Write($"testing {sln}");

            string[] args = new[] { // "/debug", 
                 "/force", sln, @"/out:..\..\Index" };

            Solution test = CreateSolutionWithOneCSharpProject();
            Solution test2 = Test2();

            dynamic argParam = ProgramLoader.MainLoad(args);
            new Program().Run(args, argParam);
        }

        public static Solution Test2()
        { 
            var mefCsProj = @"c:\Beta\src2\SourceBrowser\src\MEF\MEF.csproj";
            Solution test2 = new AdhocWorkspace().CurrentSolution;

            //  test2.AddProject()
            var debug2 = CSharpProjectFileLoaderDebug.Solution(test2);
            //  var infoX = default(LoadedProjectInfo);

            // Project csproj = Project
            object buildProj = null;
            Exception err = null;
            try
            {
                var asmBuildFr = @"c:\Beta\src2\msbuild\bin\net46\Microsoft.Build.Framework.dll";
                // c:\Beta\src2\msbuild\bin\Debug\x86\Windows_NT\Output\Microsoft.Build.Framework.dll";
                var asmBuild = @"c:\Beta\src2\msbuild\bin\net46\Microsoft.Build.dll";
                //  asmBuild = @"D:\Beta\src2\SourceBrowser\bin\Debug\net47\Microsoft.Build.DLL";
                var asm1 = Assembly.LoadFile(asmBuildFr);
                var asm2 = Assembly.LoadFile(asmBuild);

                Type Proj = asm2.GetType("Microsoft.Build.Evaluation.Project");
                // buildProj = Activator.CreateInstance(Proj, false);
                buildProj = new Microsoft.Build.Evaluation.Project(mefCsProj);
            } catch (Exception ex) { err = ex; }

            AdhocWorkspace wksAdHoc = null;
            MSBuildProjectLoader buildLoader = null;

            ProjectStateDebug debug = ProjectStateDebug.Load(test2, mefCsProj);
            var projInfo = debug.Info;
            var state = debug.StateWrap;

            var info2 = ProjectStateDebug.ProjectCsProj(test2, mefCsProj);

            var name = Path.GetFileNameWithoutExtension(mefCsProj);
            ProjectInfo info = ProjectInfo.CreateCsProj(name, filePath: mefCsProj);
            object loader = buildProj;
            var info3 = debug2.ProjectCsProj(test2, info, loader);
            // var task = debug2.GetProjectFileInfoAsync(file, cancellationToken);

            var proj = info3.Project;
            var docsIDs = info2?.ProjectState.DocumentIdsList;
            var docs = info2?.Documents;

            return test2;
        }

        // private static readonly MetadataReference s_mscorlib = TestReferences.NetFx.v4_0_30319.mscorlib;

        // http://source.roslyn.io/#Roslyn.Services.UnitTests/SolutionTests/SolutionTests.cs,159
        Solution CreateSolutionWithOneCSharpProject()
        {
            var sln = this.CreateSolution()
                       .AddProject("goo", "goo.dll", LanguageNames.CSharp);
            //  .AddMetadataReference(s_mscorlib)
            var sln2 = sln
                       .AddDocument("goo.cs", "public class Goo { }");
            var result = sln2
                       .Project.Solution;
            return result;
        }

        private Solution CreateSolution()
        {
            return new AdhocWorkspace().CurrentSolution;
        }

        public ProjectFileInfoWrap GetProjectFileInfoAsync(object source, object file)
        {
            CancellationToken cancellationToken = CancellationToken.None;

            Solution sln = source as Solution;

            // source as CSharpProjectFileLoader 
            var debug = CSharpProjectFileLoaderDebug.Solution(sln);

            var host = debug.Host;
            var x = debug.Loader;

            LoadedProjectInfo info = default(LoadedProjectInfo);
            var num = sln.Projects.GetEnumerator();
            Project csproj = num.MoveNext() ? num.Current : null;
            object loader = null;

            if (csproj != null)
            {
                // default(LoadedProjectInfo);
                info = debug.ProjectCsProj(csproj, loader);
            }

            //   ProjectFileLoaderWrap.ProjectCsProj

            //var compilerInputs = new CSharpCompilerInputs(this);

            //var buildInfo = await this.BuildAsync("Csc", compilerInputs, cancellationToken).ConfigureAwait(false);

            //if (!compilerInputs.Initialized && buildInfo.Project != null)
            //{
            //    // if msbuild didn't reach the CSC task for some reason, attempt to initialize using the variables that were defined so far.
            //    this.InitializeFromModel(compilerInputs, buildInfo.Project);
            //}

            // var file = source as CSharpProjectFileLoader.CSharpProjectFile;
            var task = debug.GetProjectFileInfoAsync(file, cancellationToken);

            //return CreateProjectFileInfo(compilerInputs, buildInfo);
            ProjectFileInfoWrap wrap = null;
            if (task != null && task.Status != TaskStatus.Faulted)
                wrap = task.GetAwaiter().GetResult();

            // problem:
            // MSBuild.CouldNotResolveSdk

            return wrap;
        }

    }

    public class TestRoslyn
    {
        public void Debug()
        {
            var dir = @"d:\Beta\src2\roslyn\";

            var sln = dir + "Roslyn.sln";
            var appDir = AppDomain.CurrentDomain.BaseDirectory;

            Console.Write($"testing {sln}");

            string[] args = new[] { // "/debug", 
                "/force", sln };

            dynamic argParam = ProgramLoader.MainLoad(args);
            new Program().Run(args, argParam);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.FileSystemGlobbing;
using Microsoft.DotNet.ProjectModel.FileSystemGlobbing.Abstractions;
using System.Threading;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.SourceBrowser.Common
{
    public static class AssemblyNameExtractor
    {
        private static readonly object projectCollectionLock = new object();

        private static readonly Regex assemblyNameRegex = new Regex(@"<(?:Module)?AssemblyName>((\w|\.|\$|\(|\)|-)+)</(?:Module)?AssemblyName>", RegexOptions.Compiled);
        private static readonly Regex rootNamespaceRegex = new Regex(@"<RootNamespace>((\w|\.)+)</RootNamespace>", RegexOptions.Compiled);

        public static IEnumerable<string> GetAssemblyNames(string projectOrSolutionFilePath)
        {
            if (!File.Exists(projectOrSolutionFilePath))
            {
                return null;
            }

            if (projectOrSolutionFilePath.EndsWith(".sln"))
            {
                return GetAssemblyNamesFromSolution(projectOrSolutionFilePath);
            }
            else
            if (projectOrSolutionFilePath.EndsWith("project.json"))
            {
                return new[] { GetAssemblyNameFromProjectJson(projectOrSolutionFilePath) };
            }
            else if (projectOrSolutionFilePath.EndsWith("global.json"))
            {
                return ProjectJsonUtilities.GetProjects(projectOrSolutionFilePath)
                    .Select(GetAssemblyNameFromProjectJson);
            }
            else
            {
                return new[] { GetAssemblyNameFromProject(projectOrSolutionFilePath) };
            }
        }

        public static string GetAssemblyNameFromProjectJson(string projectFilePath)
        {
            var project = ProjectJsonUtilities.GetCompatibleProjectContext(projectFilePath);

            return project.GetOutputPaths("Debug").CompilationFiles.Assembly;
        }

        public static string GetAssemblyNameFromProject(string projectFilePath)
        {
            string assemblyName = null;

            // first try regular expressions for the fast case
            var projectText = File.ReadAllText(projectFilePath);
            var match = assemblyNameRegex.Match(projectText);
            if (match.Groups.Count >= 2)
            {
                assemblyName = match.Groups[1].Value;

                if (assemblyName == "$(RootNamespace)")
                {
                    match = rootNamespaceRegex.Match(projectText);
                    if (match.Groups.Count >= 2)
                    {
                        assemblyName = match.Groups[1].Value;
                    }
                }

                return assemblyName;
            }

            // if regexes didn't work, try reading the XML ourselves
            var doc = XDocument.Load(projectFilePath);
            var ns = @"http://schemas.microsoft.com/developer/msbuild/2003";
            var propertyGroups = doc.Descendants(XName.Get("PropertyGroup", ns));
            var assemblyNameElement = propertyGroups.SelectMany(g => g.Elements(XName.Get("AssemblyName", ns))).LastOrDefault();
            if (assemblyNameElement != null && !assemblyNameElement.Value.Contains("$"))
            {
                assemblyName = assemblyNameElement.Value;
                return assemblyName;
            }

            var projectFileName = Path.GetFileNameWithoutExtension(projectFilePath);

            lock (projectCollectionLock)
            {
                try
                {
                    var project = ProjectCollection.GlobalProjectCollection.LoadProject(
                        projectFilePath,
                        toolsVersion: "14.0");

                    assemblyName = project.GetPropertyValue("AssemblyName");
                    if (assemblyName == "")
                    {
                        assemblyName = projectFileName;
                    }

                    if (assemblyName != null)
                    {
                        return assemblyName;
                    }
                }
                finally
                {
                    ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
                }
            }

            return projectFileName;
        }

        public static IEnumerable<string> GetAssemblyNamesFromSolution(string solutionFilePath)
        {
            var hostServices = new SolutionHost();

            //var solution = SolutionFile.Parse(solutionFilePath);
            //var assemblies = new List<string>(solution.ProjectsInOrder.Count);
            //foreach (var project in solution.ProjectsInOrder)

            MSBuildWorkspace solution = null;
            Solution sln = null;
            try
            {
                solution = global::Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create(hostServices);
            }
            catch (Exception ex) { Console.WriteLine(ex); }

            try
            {
                if (hostServices.Workspace != null)
                    sln = hostServices.OpenSolution(solutionFilePath);

            }
            catch (Exception ex) { Console.WriteLine(ex); }
            if (sln == null)
                yield break;

            int Count = 0; // solution.ProjectsInOrder.Count
            var assemblies = new List<string>(Count);

            IEnumerable<string> projectsInOrder = null;
            if (projectsInOrder == null)
                yield break;

            foreach (var project in projectsInOrder)
            {
                //if (project.ProjectType == SolutionProjectType.SolutionFolder)
                //{
                //    continue;
                //}

                string assembly = null;
                try
                {
                    assembly = GetAssemblyNameFromProject(project); // .AbsolutePath);
                    //assemblies.Add(assembly);
                }
                catch
                {
                }

                if (assembly != null)
                    yield return assembly;
            }
        }
    }

    internal enum SolutionProjectType
    {
        Unknown = 0,
        KnownToBeMSBuildFormat = 1,
        SolutionFolder = 2,
        WebProject = 3,
        WebDeploymentProject = 4
    }

    public class SolutionHost : Microsoft.CodeAnalysis.Host.HostServices
    {
        // Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace

        public Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace Workspace { get; set; }

        public class Services : HostWorkspaceServices
        {
            public SolutionHost Host { get; set; }

            public override HostServices HostServices
            {
                get { return Host; }
            }

            public override CodeAnalysis.Workspace Workspace
            {
                get
                {
                    return Host.Workspace;
                }
            }

            public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter)
            {
                yield break;
            }

            public override TWorkspaceService GetService<TWorkspaceService>()
            {
                var type = typeof(TWorkspaceService);
                if (type.Name == "IWorkspaceTaskSchedulerFactory")
                {
                    // { Name = "IWorkspaceTaskSchedulerFactory" FullName = "Microsoft.CodeAnalysis.Host.IWorkspaceTaskSchedulerFactory"}
                    // IWorkspaceTaskSchedulerFactory: IWorkspaceService
                    return default(TWorkspaceService);
                }
                return default(TWorkspaceService);
            }
        }

        private MSBuildProjectLoader _loader;
        public SolutionHost() { }

        protected override HostWorkspaceServices CreateWorkspaceServices(CodeAnalysis.Workspace workspace)
        {
            Workspace = workspace as Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace;
            var properties = ImmutableDictionary<string, string>.Empty;
            this._loader = new MSBuildProjectLoader(workspace, properties);

            return new Services { Host = this };
        }

        //private IProjectFile _applyChangesProjectFile;

        public Solution OpenSolution(string solutionFilePath)
        {
            if (Workspace == null)
                return null;


            CancellationToken cancellationToken = CancellationToken.None;
            Solution CurrentSolution = null;

            Task<SolutionInfo> solutionInfo = _loader.LoadSolutionInfoAsync(solutionFilePath, cancellationToken);

            solutionInfo.RunSynchronously();
            if (solutionInfo.IsFaulted) // ???
                solutionInfo.Start();
            solutionInfo.Wait();
            var info = solutionInfo.Result;

            Task<Solution> openTask = Workspace.OpenSolutionAsync(solutionFilePath, cancellationToken);

            //var task = Task.Factory.StartNew<Solution>();
            // openTask.RunSynchronously();
            openTask.Start();
            openTask.Wait();
            CurrentSolution = openTask.Result;

            //if (solutionFilePath == null)
            //    throw new ArgumentNullException("solutionFilePath");
            //this.ClearSolution();
            //this.OnSolutionAdded(await this._loader.LoadSolutionInfoAsync(solutionFilePath, cancellationToken).ConfigureAwait(false));
            //this.UpdateReferencesAfterAdd();

            return CurrentSolution;
        }
    }

}


/*
 
// Source:  /webstack/Mvc/SourceBrowserExt/analysis/bin/Microsoft.CodeAnalysis.Workspaces.Desktop.dll  Build 1.3.0.60613
using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.MSBuild
{  

   [Guid("02d9d0ec-50e4-360c-8940-d2e20d68301b")]
   public sealed class MSBuildWorkspace : Microsoft.CodeAnalysis.Workspace
   {
       public static MSBuildWorkspace Create() { throw new NotImplementedException(); }
       public static MSBuildWorkspace Create(System.Collections.Generic.IDictionary<string,string> properties) { throw new NotImplementedException(); }
       public static MSBuildWorkspace Create(Microsoft.CodeAnalysis.Host.HostServices hostServices) { throw new NotImplementedException(); }
       public static MSBuildWorkspace Create(System.Collections.Generic.IDictionary<string,string> properties, Microsoft.CodeAnalysis.Host.HostServices hostServices) { throw new NotImplementedException(); }
       public void AssociateFileExtensionWithLanguage(string projectFileExtension, string language) {}
       public void CloseSolution() {}
       public System.Threading.Tasks.Task<Microsoft.CodeAnalysis.Solution> OpenSolutionAsync(string solutionFilePath, System.Threading.CancellationToken cancellationToken) { throw new NotImplementedException(); }
       public System.Threading.Tasks.Task<Microsoft.CodeAnalysis.Project> OpenProjectAsync(string projectFilePath, System.Threading.CancellationToken cancellationToken) { throw new NotImplementedException(); }
  

// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.3.1.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: 2BA3A08D-4FE0-467C-8C54-C8BC37C61987
// Assembly location: D:\webstack\Mvc\SourceBrowser\bin\HtmlGenerator\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild
{
  public sealed class MSBuildWorkspace : Workspace
  {
    private readonly NonReentrantLock _serializationLock = new NonReentrantLock(false);
    private MSBuildProjectLoader _loader;
    private IProjectFile _applyChangesProjectFile;

    public ImmutableDictionary<string, string> Properties
    {
      get
      {
        return this._loader.Properties;
      }
    }

    public bool LoadMetadataForReferencedProjects
    {
      get
      {
        return this._loader.LoadMetadataForReferencedProjects;
      }
      set
      {
        this._loader.LoadMetadataForReferencedProjects = value;
      }
    }

    public bool SkipUnrecognizedProjects
    {
      get
      {
        return this._loader.SkipUnrecognizedProjects;
      }
      set
      {
        this._loader.SkipUnrecognizedProjects = value;
      }
    }

    private MSBuildWorkspace(HostServices hostServices, ImmutableDictionary<string, string> properties)
      : base(hostServices, "MSBuildWorkspace")
    {
      this._loader = new MSBuildProjectLoader((Workspace) this, properties);
    }

    public static MSBuildWorkspace Create()
    {
      return MSBuildWorkspace.Create((IDictionary<string, string>) ImmutableDictionary<string, string>.Empty);
    }

    public static MSBuildWorkspace Create(IDictionary<string, string> properties)
    {
      return MSBuildWorkspace.Create(properties, (HostServices) DesktopMefHostServices.DefaultServices);
    }

    public static MSBuildWorkspace Create(HostServices hostServices)
    {
      return MSBuildWorkspace.Create((IDictionary<string, string>) ImmutableDictionary<string, string>.Empty, hostServices);
    }

    public static MSBuildWorkspace Create(IDictionary<string, string> properties, HostServices hostServices)
    {
      if (properties == null)
        throw new ArgumentNullException("properties");
      if (hostServices == null)
        throw new ArgumentNullException("hostServices");
      return new MSBuildWorkspace(hostServices, properties.ToImmutableDictionary<string, string>());
    }

    public void AssociateFileExtensionWithLanguage(string projectFileExtension, string language)
    {
      this._loader.AssociateFileExtensionWithLanguage(projectFileExtension, language);
    }

    public void CloseSolution()
    {
      using (this._serializationLock.DisposableWait(new CancellationToken()))
        this.ClearSolution();
    }

    private string GetAbsolutePath(string path, string baseDirectoryPath)
    {
      return System.IO.Path.GetFullPath(FileUtilities.ResolveRelativePath(path, baseDirectoryPath) ?? path);
    }

    public async Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken = null)
    {
      if (solutionFilePath == null)
        throw new ArgumentNullException("solutionFilePath");
      this.ClearSolution();
      this.OnSolutionAdded(await this._loader.LoadSolutionInfoAsync(solutionFilePath, cancellationToken).ConfigureAwait(false));
      this.UpdateReferencesAfterAdd();
      return this.CurrentSolution;
    }

    public async Task<Project> OpenProjectAsync(string projectFilePath, CancellationToken cancellationToken = null)
    {
      if (projectFilePath == null)
        throw new ArgumentNullException("projectFilePath");
      ImmutableArray<ProjectInfo> immutableArray = await this._loader.LoadProjectInfoAsync(projectFilePath, this.GetCurrentProjectMap(), cancellationToken).ConfigureAwait(false);
      foreach (ProjectInfo projectInfo in immutableArray)
        this.OnProjectAdded(projectInfo);
      this.UpdateReferencesAfterAdd();
      return this.CurrentSolution.GetProject(immutableArray[0].Id);
    }

    private ImmutableDictionary<string, ProjectId> GetCurrentProjectMap()
    {
      IEnumerable<Project> source = this.CurrentSolution.Projects.Where<Project>((Func<Project, bool>) (p => !string.IsNullOrEmpty(p.FilePath)));
      Func<Project, string> func = (Func<Project, string>) (p => p.FilePath);
      Func<Project, string> keySelector;
      return source.ToImmutableDictionary<Project, string, ProjectId>(keySelector, (Func<Project, ProjectId>) (p => p.Id));
    }

    public override bool CanApplyChange(ApplyChangesKind feature)
    {
      switch (feature)
      {
        case ApplyChangesKind.AddProjectReference:
        case ApplyChangesKind.RemoveProjectReference:
        case ApplyChangesKind.AddMetadataReference:
        case ApplyChangesKind.RemoveMetadataReference:
        case ApplyChangesKind.AddDocument:
        case ApplyChangesKind.RemoveDocument:
        case ApplyChangesKind.ChangeDocument:
        case ApplyChangesKind.AddAnalyzerReference:
        case ApplyChangesKind.RemoveAnalyzerReference:
          return true;
        default:
          return false;
      }
    }

    private bool HasProjectFileChanges(ProjectChanges changes)
    {
      if (!changes.GetAddedDocuments().Any<DocumentId>() && !changes.GetRemovedDocuments().Any<DocumentId>() && (!changes.GetAddedMetadataReferences().Any<MetadataReference>() && !changes.GetRemovedMetadataReferences().Any<MetadataReference>()) && (!changes.GetAddedProjectReferences().Any<ProjectReference>() && !changes.GetRemovedProjectReferences().Any<ProjectReference>() && !changes.GetAddedAnalyzerReferences().Any<AnalyzerReference>()))
        return changes.GetRemovedAnalyzerReferences().Any<AnalyzerReference>();
      return true;
    }

    public override bool TryApplyChanges(Solution newSolution)
    {
      return this.TryApplyChanges(newSolution, (IProgressTracker) new ProgressTracker());
    }

    internal override bool TryApplyChanges(Solution newSolution, IProgressTracker progressTracker)
    {
      using (this._serializationLock.DisposableWait(new CancellationToken()))
        return base.TryApplyChanges(newSolution, progressTracker);
    }

    protected override void ApplyProjectChanges(ProjectChanges projectChanges)
    {
      Project project = projectChanges.OldProject ?? projectChanges.NewProject;
      try
      {
        if (this.HasProjectFileChanges(projectChanges))
        {
          string filePath = project.FilePath;
          IProjectFileLoader loader;
          if (this._loader.TryGetLoaderFromProjectPath(filePath, out loader))
          {
            try
            {
              this._applyChangesProjectFile = loader.LoadProjectFileAsync(filePath, (IDictionary<string, string>) this._loader.Properties, CancellationToken.None).Result;
            }
            catch (IOException ex)
            {
              this.OnWorkspaceFailed((WorkspaceDiagnostic) new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, projectChanges.ProjectId));
            }
          }
        }
        base.ApplyProjectChanges(projectChanges);
        if (this._applyChangesProjectFile == null)
          return;
        try
        {
          this._applyChangesProjectFile.Save();
        }
        catch (IOException ex)
        {
          this.OnWorkspaceFailed((WorkspaceDiagnostic) new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, projectChanges.ProjectId));
        }
      }
      finally
      {
        this._applyChangesProjectFile = (IProjectFile) null;
      }
    }

    protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText text)
    {
      Document document = this.CurrentSolution.GetDocument(documentId);
      if (document == null)
        return;
      System.Text.Encoding encoding = MSBuildWorkspace.DetermineEncoding(text, document);
      this.SaveDocumentText(documentId, document.FilePath, text, encoding ?? (System.Text.Encoding) new UTF8Encoding(false));
      this.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
    }

    private static System.Text.Encoding DetermineEncoding(SourceText text, Document document)
    {
      if (text.Encoding != null)
        return text.Encoding;
      try
      {
        using (ExceptionHelpers.SuppressFailFast())
        {
          using (System.IO.FileStream fileStream = new System.IO.FileStream(document.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            return EncodedStringText.Create((Stream) fileStream, (System.Text.Encoding) null, SourceHashAlgorithm.Sha1).Encoding;
        }
      }
      catch (IOException ex)
      {
      }
      catch (InvalidDataException ex)
      {
      }
      return (System.Text.Encoding) null;
    }

    protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
    {
      Project project = this.CurrentSolution.GetProject(info.Id.ProjectId);
      IProjectFileLoader loader;
      if (!this._loader.TryGetLoaderFromProjectPath(project.FilePath, out loader))
        return;
      string documentExtension = this._applyChangesProjectFile.GetDocumentExtension(info.SourceCodeKind);
      string str1 = System.IO.Path.ChangeExtension(info.Name, documentExtension);
      string str2 = info.Folders == null || info.Folders.Count <= 0 ? str1 : System.IO.Path.Combine(System.IO.Path.Combine(info.Folders.ToArray<string>()), str1);
      string absolutePath = this.GetAbsolutePath(str2, System.IO.Path.GetDirectoryName(project.FilePath));
      DocumentInfo documentInfo = info.WithName(str1).WithFilePath(absolutePath).WithTextLoader((TextLoader) new FileTextLoader(absolutePath, text.Encoding));
      this._applyChangesProjectFile.AddDocument(str2, (string) null);
      this.OnDocumentAdded(documentInfo);
      if (text == null)
        return;
      this.SaveDocumentText(info.Id, absolutePath, text, text.Encoding ?? System.Text.Encoding.UTF8);
    }

    private void SaveDocumentText(DocumentId id, string fullPath, SourceText newText, System.Text.Encoding encoding)
    {
      try
      {
        using (ExceptionHelpers.SuppressFailFast())
        {
          string directoryName = System.IO.Path.GetDirectoryName(fullPath);
          if (!System.IO.Directory.Exists(directoryName))
            System.IO.Directory.CreateDirectory(directoryName);
          using (StreamWriter streamWriter = new StreamWriter(fullPath, false, encoding))
            newText.Write((TextWriter) streamWriter, new CancellationToken());
        }
      }
      catch (IOException ex)
      {
        this.OnWorkspaceFailed((WorkspaceDiagnostic) new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, id));
      }
    }

    protected override void ApplyDocumentRemoved(DocumentId documentId)
    {
      Document document = this.CurrentSolution.GetDocument(documentId);
      if (document == null)
        return;
      this._applyChangesProjectFile.RemoveDocument(document.FilePath);
      this.DeleteDocumentFile(document.Id, document.FilePath);
      this.OnDocumentRemoved(documentId);
    }

    private void DeleteDocumentFile(DocumentId documentId, string fullPath)
    {
      try
      {
        if (!System.IO.File.Exists(fullPath))
          return;
        System.IO.File.Delete(fullPath);
      }
      catch (IOException ex)
      {
        this.OnWorkspaceFailed((WorkspaceDiagnostic) new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, documentId));
      }
      catch (NotSupportedException ex)
      {
        this.OnWorkspaceFailed((WorkspaceDiagnostic) new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, documentId));
      }
      catch (UnauthorizedAccessException ex)
      {
        this.OnWorkspaceFailed((WorkspaceDiagnostic) new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, ex.Message, documentId));
      }
    }

    protected override void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
    {
      AssemblyIdentity assemblyIdentity = this.GetAssemblyIdentity(projectId, metadataReference);
      this._applyChangesProjectFile.AddMetadataReference(metadataReference, assemblyIdentity);
      this.OnMetadataReferenceAdded(projectId, metadataReference);
    }

    protected override void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
    {
      AssemblyIdentity assemblyIdentity = this.GetAssemblyIdentity(projectId, metadataReference);
      this._applyChangesProjectFile.RemoveMetadataReference(metadataReference, assemblyIdentity);
      this.OnMetadataReferenceRemoved(projectId, metadataReference);
    }

    private AssemblyIdentity GetAssemblyIdentity(ProjectId projectId, MetadataReference metadataReference)
    {
      Project project = this.CurrentSolution.GetProject(projectId);
      if (!project.MetadataReferences.Contains<MetadataReference>(metadataReference))
        project = project.AddMetadataReference(metadataReference);
      IAssemblySymbol assemblySymbol = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult_CanCallOnBackground<Compilation>(CancellationToken.None).GetAssemblyOrModuleSymbol(metadataReference) as IAssemblySymbol;
      if (assemblySymbol == null)
        return (AssemblyIdentity) null;
      return assemblySymbol.Identity;
    }

    protected override void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
    {
      Project project = this.CurrentSolution.GetProject(projectReference.ProjectId);
      if (project != null)
        this._applyChangesProjectFile.AddProjectReference(project.Name, new ProjectFileReference(project.FilePath, projectReference.Aliases));
      this.OnProjectReferenceAdded(projectId, projectReference);
    }

    protected override void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
    {
      Project project = this.CurrentSolution.GetProject(projectReference.ProjectId);
      if (project != null)
        this._applyChangesProjectFile.RemoveProjectReference(project.Name, project.FilePath);
      this.OnProjectReferenceRemoved(projectId, projectReference);
    }

    protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
    {
      this._applyChangesProjectFile.AddAnalyzerReference(analyzerReference);
      this.OnAnalyzerReferenceAdded(projectId, analyzerReference);
    }

    protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
    {
      this._applyChangesProjectFile.RemoveAnalyzerReference(analyzerReference);
      this.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
    }
  }
}


*/

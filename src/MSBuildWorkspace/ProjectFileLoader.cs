// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.ProjectFileLoader
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: D:\Sanitex\webstack\SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.CodeAnalysis.MSBuild
{
  
  // internal 
  public interface IProjectFileLoader : ILanguageService
  {
    string Language { get; }

    Task<IProjectFile> LoadProjectFileAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken);
  }


  public interface IProjectFile
  {
    string FilePath { get; }

    // Task<ProjectFileInfo> GetProjectFileInfoAsync(CancellationToken cancellationToken);

    SourceCodeKind GetSourceCodeKind(string documentFileName);

    string GetDocumentExtension(SourceCodeKind kind);

    string GetPropertyValue(string name);

    void AddDocument(string filePath, string logicalPath = null);

    void RemoveDocument(string filePath);

    void AddMetadataReference(MetadataReference reference, AssemblyIdentity identity);

    void RemoveMetadataReference(MetadataReference reference, AssemblyIdentity identity);

    void AddProjectReference(string projectName, ProjectFileReference reference);

    void RemoveProjectReference(string projectName, string projectFilePath);

    //void AddAnalyzerReference(AnalyzerReference reference);
    //void RemoveAnalyzerReference(AnalyzerReference reference);

    void Save();
  }



  // internal 
   public abstract class ProjectFileLoader : IProjectFileLoader, ILanguageService
  {
    private static readonly XmlReaderSettings s_xmlSettings;

    public abstract string Language { get; }
    static private int _state; //  _state

    static ProjectFileLoader()
    {
      XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
      int num = 0;
      xmlReaderSettings.DtdProcessing = (DtdProcessing) num;
      // ISSUE: variable of the null type
      // __Null 
      XmlResolver local = null;
      xmlReaderSettings.XmlResolver = (XmlResolver) local;
      ProjectFileLoader.s_xmlSettings = xmlReaderSettings;
    }

    protected abstract ProjectFile CreateProjectFile(Microsoft.Build.Evaluation.Project loadedProject);

    public async Task<IProjectFile> LoadProjectFileAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
    {
      // ISSUE: explicit reference operation
      // ISSUE: reference to a compiler-generated field
      if (_state != 0)
      {
        if (path == null)
          throw new ArgumentNullException("path");
      }
      else
      {
        ConfiguredTaskAwaitable<Microsoft.Build.Evaluation.Project>.ConfiguredTaskAwaiter
            configuredTaskAwaiter = new ConfiguredTaskAwaitable<Microsoft.Build.Evaluation.Project>.ConfiguredTaskAwaiter();
        int num;
        // ISSUE: explicit reference operation
        // ISSUE: reference to a compiler-generated field
        _state = num = -1;
      }
      return (IProjectFile) this.CreateProjectFile(await ProjectFileLoader.LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<Microsoft.Build.Evaluation.Project> LoadProjectAsync(string path,
        IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
    {
      Dictionary<string, string> properties;
      // ISSUE: explicit reference operation
      // ISSUE: reference to a compiler-generated field
      
      if (_state != 0)
      {
        properties = new Dictionary<string, string>(globalProperties ?? (IDictionary<string, string>) ImmutableDictionary<string, string>.Empty);
        properties["DesignTimeBuild"] = "true";
        properties["BuildingInsideVisualStudio"] = "true";
      }
      else
      {
        ConfiguredTaskAwaitable<System.IO.MemoryStream>.ConfiguredTaskAwaiter configuredTaskAwaiter
            = new ConfiguredTaskAwaitable<System.IO.MemoryStream>.ConfiguredTaskAwaiter();
        int num;
        // ISSUE: explicit reference operation
        // ISSUE: reference to a compiler-generated field
        _state = num = -1;
      }
      XmlReader xmlReader = XmlReader.Create((Stream) 
          await ProjectFileLoader.ReadFileAsync(path, cancellationToken).ConfigureAwait(false), ProjectFileLoader.s_xmlSettings);

      ProjectCollection projectCollection1 = new ProjectCollection();
      ProjectCollection projectCollection2 = projectCollection1;
      ProjectRootElement xml = ProjectRootElement.Create(xmlReader, projectCollection2);
      string str = path;
      xml.FullPath = str;
      
        //Dictionary<string, string> dictionary = properties;
      // ISSUE: variable of the null type
      //__Null 
        string local = null;

        throw new NotImplementedException();
      //ProjectCollection projectCollection3 = projectCollection1;
      //return new Microsoft.Build.Evaluation.Project(xml, (IDictionary<string, string>) dictionary, (string) local, projectCollection3);
    }

    public static // async 
        Task<string> GetOutputFilePathAsync(string path, 
        IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
    {
      // ISSUE: explicit reference operation
      // ISSUE: reference to a compiler-generated field

       throw new NotImplementedException();
      //if (this._state == 0)
      //{
      //  ConfiguredTaskAwaitable<Microsoft.Build.Evaluation.Project>.ConfiguredTaskAwaiter configuredTaskAwaiter = new ConfiguredTaskAwaitable<Microsoft.Build.Evaluation.Project>.ConfiguredTaskAwaiter();
      //  int num;
      //  // ISSUE: explicit reference operation
      //  // ISSUE: reference to a compiler-generated field
      //  this._state = num = -1;
      //}

      //return await ProjectFileLoader.LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false)
      //    .GetPropertyValue("TargetPath");
    }

    private static async Task<System.IO.MemoryStream> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
      // ISSUE: explicit reference operation
      // ISSUE: reference to a compiler-generated field
        int num = _state;
      Stream stream;
      byte[] buffer;
      System.IO.MemoryStream memoryStream;
      if (num != 0)
      {
        memoryStream = new System.IO.MemoryStream();
        buffer = new byte[1024];
        stream = FileUtilities.OpenAsyncRead(path);
      }
      try
      {
        if (num == 0)
          goto label_6;
label_5:
        goto label_7;
label_6:
        ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter configuredTaskAwaiter = new ConfiguredTaskAwaitable<int>.ConfiguredTaskAwaiter();
        // ISSUE: explicit reference operation
        // ISSUE: reference to a compiler-generated field
        _state = num = -1;
label_7:

        throw new NotImplementedException();
        //int count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        //memoryStream.Write(buffer, 0, count);
        //if (count > 0)
        //  goto label_5;
      }
      finally
      {
        //if (num < 0 && stream != null)
        //  stream.Dispose();
      }
      stream = (Stream) null;
      memoryStream.Position = 0L;
      return memoryStream;
    }

    public static IProjectFileLoader GetLoaderForProjectTypeGuid(Workspace workspace, Guid guid)
    {
      Func<string, bool> func;

      throw new NotImplementedException();
      //return Enumerable.FirstOrDefault<IProjectFileLoader>(workspace.Services.FindLanguageServices<IProjectFileLoader>(
      //    (HostWorkspaceServices.MetadataFilter) (d => Enumerable.Any<string>(IReadOnlyDictionaryExtensions.GetEnumerableMetadata<string>(d, "ProjectTypeGuid"), func ?? (func = (Func<string, bool>) (g => guid == new Guid(g)))))));
    }

    public static IProjectFileLoader GetLoaderForProjectFileExtension(Workspace workspace, string extension)
    {
      Func<string, bool> func;

      throw new NotImplementedException();
      //return Enumerable.FirstOrDefault<IProjectFileLoader>(workspace.Services.FindLanguageServices<IProjectFileLoader>((HostWorkspaceServices.MetadataFilter) (d => Enumerable.Any<string>(IReadOnlyDictionaryExtensions.GetEnumerableMetadata<string>(d, "ProjectFileExtension"), func ?? (func = (Func<string, bool>) (e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))))));
    }
  }
}

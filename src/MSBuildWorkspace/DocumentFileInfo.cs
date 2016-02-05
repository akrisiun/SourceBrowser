// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

namespace Microsoft.CodeAnalysis.MSBuild
{
  // internal 
  public sealed class DocumentFileInfo
  {
    public string FilePath
    {
      get ; set;
    }

    public string LogicalPath
    {
      get; set;
    }

    public bool IsLinked
    {
        get;
        set;
    }

    public bool IsGenerated
    {
      get; set;
    }

    public DocumentFileInfo(string filePath, string logicalPath, bool isLinked, bool isGenerated)
    {
      // ISSUE: reference to a compiler-generated field
      this.FilePath = filePath;
      this.LogicalPath = logicalPath;
      this.IsLinked = isLinked;
      this.IsGenerated = isGenerated;
    }
  }
}

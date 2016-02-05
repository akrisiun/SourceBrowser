// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.GlobalAssemblyCache
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
  internal static class GlobalAssemblyCache
  {
    public static readonly ImmutableArray<ProcessorArchitecture> CurrentArchitectures 
        = IntPtr.Size == 4 ? ImmutableArray.Create<ProcessorArchitecture>(ProcessorArchitecture.None,
            ProcessorArchitecture.MSIL, ProcessorArchitecture.X86) : ImmutableArray.Create<ProcessorArchitecture>(
            ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.Amd64);
    public static readonly ImmutableArray<string> RootLocations = 
        ImmutableArray.Create<string>(GlobalAssemblyCache.GetLocation(GlobalAssemblyCache.ASM_CACHE.ROOT),
        GlobalAssemblyCache.GetLocation(GlobalAssemblyCache.ASM_CACHE.ROOT_EX));
    private const int MAX_PATH = 260;
    private const int ERROR_INSUFFICIENT_BUFFER = -2147024774;
    private const int S_OK = 0;
    private const int S_FALSE = 1;

    //[DllImport("clr", CharSet = CharSet.Auto)]
    //private static extern int CreateAssemblyEnum(out GlobalAssemblyCache.IAssemblyEnum ppEnum,
    //    FusionAssemblyIdentity.IApplicationContext pAppCtx, FusionAssemblyIdentity.IAssemblyName pName, 
    //    GlobalAssemblyCache.ASM_CACHE dwFlags, IntPtr pvReserved);

    [DllImport("clr", CharSet = CharSet.Auto)]
    private static extern unsafe int GetCachePath(GlobalAssemblyCache.ASM_CACHE id, byte* path, ref int length);

    [DllImport("clr", CharSet = CharSet.Auto, PreserveSig = false)]
    private static extern void CreateAssemblyCache(out GlobalAssemblyCache.IAssemblyCache ppAsmCache, uint dwReserved);

    private static unsafe string GetLocation(GlobalAssemblyCache.ASM_CACHE gacId)
    {
      int length = 0;
      int cachePath1 = GlobalAssemblyCache.GetCachePath(gacId, (byte*) null, ref length);
      if (cachePath1 != -2147024774)
        throw Marshal.GetExceptionForHR(cachePath1);
      fixed (byte* path = new byte[(length + 1) * 2])
      {
        int cachePath2 = GlobalAssemblyCache.GetCachePath(gacId, path, ref length);
        if (cachePath2 != 0)
          throw Marshal.GetExceptionForHR(cachePath2);
        else
          return Marshal.PtrToStringUni((IntPtr) ((void*) path));
      }
    }

    //public static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, ImmutableArray<ProcessorArchitecture> architectureFilter = null)
    //{
    //  return GlobalAssemblyCache.GetAssemblyIdentities(FusionAssemblyIdentity.ToAssemblyNameObject(partialName), architectureFilter);
    //}

    //public static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, ImmutableArray<ProcessorArchitecture> architectureFilter = null)
    //{
    //  FusionAssemblyIdentity.IAssemblyName partialName1;
    //  if (partialName != null)
    //  {
    //    partialName1 = FusionAssemblyIdentity.ToAssemblyNameObject(partialName);
    //    if (partialName1 == null)
    //      return SpecializedCollections.EmptyEnumerable<AssemblyIdentity>();
    //  }
    //  else
    //    partialName1 = (FusionAssemblyIdentity.IAssemblyName) null;
    //  return GlobalAssemblyCache.GetAssemblyIdentities(partialName1, architectureFilter);
    //}

    //public static IEnumerable<string> GetAssemblySimpleNames(ImmutableArray<ProcessorArchitecture> architectureFilter = null)
    //{
    //  // ISSUE: reference to a compiler-generated field
    //  // ISSUE: reference to a compiler-generated field
    //  // ISSUE: reference to a compiler-generated field
    //  // ISSUE: reference to a compiler-generated method
    //  return Enumerable.Distinct<string>(Enumerable.Select<FusionAssemblyIdentity.IAssemblyName, string>(
    //      GlobalAssemblyCache.GetAssemblyObjects((FusionAssemblyIdentity.IAssemblyName) null, architectureFilter), 
    //      GlobalAssemblyCache.\u003C\u003Ec.\u003C\u003E9__15_0 ?? (GlobalAssemblyCache.\u003C\u003Ec.\u003C\u003E9__15_0 
    //          = new Func<FusionAssemblyIdentity.IAssemblyName, string>(GlobalAssemblyCache.\u003C\u003Ec.\u003C\u003E9.\u003CGetAssemblySimpleNames\u003Eb__15_0))));
    //}

    //private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(FusionAssemblyIdentity.IAssemblyName 
    //    partialName, ImmutableArray<ProcessorArchitecture> architectureFilter)
    //{
    //  // ISSUE: reference to a compiler-generated field
    //  // ISSUE: reference to a compiler-generated field
    //  // ISSUE: reference to a compiler-generated field
    //  // ISSUE: reference to a compiler-generated method
    //  return Enumerable.Select<FusionAssemblyIdentity.IAssemblyName,
    //      AssemblyIdentity>(GlobalAssemblyCache.GetAssemblyObjects(partialName, architectureFilter), 
    //      GlobalAssemblyCache.\u003C\u003Ec.\u003C\u003E9__16_0 ?? (GlobalAssemblyCache.\u003C\u003Ec.\u003C\u003E9__16_0 
    //          = new Func<FusionAssemblyIdentity.IAssemblyName, AssemblyIdentity>(
    //              GlobalAssemblyCache.\u003C\u003Ec.\u003C\u003E9.\u003CGetAssemblyIdentities\u003Eb__16_0)));
    //}

    //[IteratorStateMachine(typeof (GlobalAssemblyCache.\u003CGetAssemblyObjects\u003Ed__19))]
    //internal static IEnumerable<FusionAssemblyIdentity.IAssemblyName> GetAssemblyObjects(FusionAssemblyIdentity.IAssemblyName partialNameFilter, ImmutableArray<ProcessorArchitecture> architectureFilter)
    //{
    //  // ISSUE: object of a compiler-generated type is created
    //  // ISSUE: variable of a compiler-generated type
    //  GlobalAssemblyCache.\u003CGetAssemblyObjects\u003Ed__19 assemblyObjectsD19 = new GlobalAssemblyCache.\u003CGetAssemblyObjects\u003Ed__19(-2);
    //  FusionAssemblyIdentity.IAssemblyName assemblyName = partialNameFilter;
    //  // ISSUE: reference to a compiler-generated field
    //  assemblyObjectsD19.\u003C\u003E3__partialNameFilter = assemblyName;
    //  ImmutableArray<ProcessorArchitecture> immutableArray = architectureFilter;
    //  // ISSUE: reference to a compiler-generated field
    //  assemblyObjectsD19.\u003C\u003E3__architectureFilter = immutableArray;
    //  return (IEnumerable<FusionAssemblyIdentity.IAssemblyName>) assemblyObjectsD19;
    //}

    public static AssemblyIdentity ResolvePartialName(string displayName,
        ImmutableArray<ProcessorArchitecture>? xarchitectureFilter = null, System.Globalization.CultureInfo preferredCulture = null)
    {
      string location;
      ImmutableArray<ProcessorArchitecture> architectureFilter = xarchitectureFilter ?? default(ImmutableArray<ProcessorArchitecture>);
      throw new NotImplementedException();
      //return GlobalAssemblyCache.ResolvePartialName(displayName, architectureFilter, preferredCulture, out location, false);
    }

    public static AssemblyIdentity ResolvePartialName(string displayName, out string location, 
        ImmutableArray<ProcessorArchitecture>? xarchitectureFilter = null, System.Globalization.CultureInfo preferredCulture = null)
    {
       ImmutableArray<ProcessorArchitecture> architectureFilter = xarchitectureFilter ?? default(ImmutableArray<ProcessorArchitecture>);
       throw new NotImplementedException();
      //return GlobalAssemblyCache.ResolvePartialName(displayName, architectureFilter, preferredCulture, out location, true);
    }

    //private static AssemblyIdentity ResolvePartialName(string displayName, ImmutableArray<ProcessorArchitecture> architectureFilter, System.Globalization.CultureInfo preferredCulture, out string location, bool resolveLocation)
    //{
    //  if (displayName == null)
    //    throw new ArgumentNullException("displayName");
    //  location = (string) null;
    //  FusionAssemblyIdentity.IAssemblyName partialNameFilter = FusionAssemblyIdentity.ToAssemblyNameObject(displayName);
    //  if (partialNameFilter == null)
    //    return (AssemblyIdentity) null;
    //  FusionAssemblyIdentity.IAssemblyName bestMatch = FusionAssemblyIdentity.GetBestMatch(GlobalAssemblyCache.GetAssemblyObjects(partialNameFilter, architectureFilter), preferredCulture == null || preferredCulture.IsNeutralCulture ? (string) null : preferredCulture.Name);
    //  if (bestMatch == null)
    //    return (AssemblyIdentity) null;
    //  if (resolveLocation)
    //    location = GlobalAssemblyCache.GetAssemblyLocation(bestMatch);
    //  return FusionAssemblyIdentity.ToAssemblyIdentity(bestMatch);
    //}

    //internal static unsafe string GetAssemblyLocation(FusionAssemblyIdentity.IAssemblyName nameObject)
    //{
    //  string displayName = FusionAssemblyIdentity.GetDisplayName(nameObject, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);
    //  fixed (char* chPtr = new char[260])
    //  {
    //    GlobalAssemblyCache.ASSEMBLY_INFO pAsmInfo = new GlobalAssemblyCache.ASSEMBLY_INFO()
    //    {
    //      cbAssemblyInfo = (uint) Marshal.SizeOf(typeof (GlobalAssemblyCache.ASSEMBLY_INFO)),
    //      pszCurrentAssemblyPathBuf = chPtr,
    //      cchBuf = 260U
    //    };
    //    GlobalAssemblyCache.IAssemblyCache ppAsmCache;
    //    GlobalAssemblyCache.CreateAssemblyCache(out ppAsmCache, 0U);
    //    ppAsmCache.QueryAssemblyInfo(0U, displayName, ref pAsmInfo);
    //    return Marshal.PtrToStringUni((IntPtr) ((void*) pAsmInfo.pszCurrentAssemblyPathBuf), (int) pAsmInfo.cchBuf - 1);
    //  }
    //}

    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //[Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
    //[ComImport]
    //private interface IAssemblyEnum
    //{
    //  [MethodImpl(MethodImplOptions.PreserveSig)]
    //  int GetNextAssembly(out FusionAssemblyIdentity.IApplicationContext ppAppCtx, out FusionAssemblyIdentity.IAssemblyName ppName, uint dwFlags);

    //  [MethodImpl(MethodImplOptions.PreserveSig)]
    //  int Reset();

    //  [MethodImpl(MethodImplOptions.PreserveSig)]
    //  int Clone(out GlobalAssemblyCache.IAssemblyEnum ppEnum);
    //}

    [Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    private interface IAssemblyCache
    {
      void UninstallAssembly();

      void QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName,
          ref GlobalAssemblyCache.ASSEMBLY_INFO pAsmInfo);

      void CreateAssemblyCacheItem();

      void CreateAssemblyScavenger();

      void InstallAssembly();
    }

    private struct ASSEMBLY_INFO
    {
      public uint cbAssemblyInfo;
      public readonly uint dwAssemblyFlags;
      public readonly ulong uliAssemblySizeInKB;
      public unsafe char* pszCurrentAssemblyPathBuf;
      public uint cchBuf;
    }

    private enum ASM_CACHE
    {
      ZAP = 1,
      GAC = 2,
      DOWNLOAD = 4,
      ROOT = 8,
      GAC_MSIL = 16,
      GAC_32 = 32,
      GAC_64 = 64,
      ROOT_EX = 128,
    }
  }
}


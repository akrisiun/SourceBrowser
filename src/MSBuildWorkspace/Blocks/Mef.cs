// Decompiled with JetBrains decompiler
// Type: Microsoft.CodeAnalysis.Host.Mef.DesktopMefHostServices
// Assembly: Microsoft.CodeAnalysis.Workspaces.Desktop, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
// MVID: D215115A-535F-4F97-A96F-CBBE58E1FDB0
// Assembly location: SourceBrowser\bin\Microsoft.CodeAnalysis.Workspaces.Desktop.dll

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;


namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal interface IMefHostExportProvider
    {
        IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>();
        IEnumerable<Lazy<TExtension>> GetExports<TExtension>();
    }
}

namespace Microsoft.CodeAnalysis.Host.Mef
{
    public static class DesktopMefHostServices
    {
        private static MefHostServices s_defaultServices;
        private static ImmutableArray<Assembly> s_defaultAssemblies;

        public static MefHostServices DefaultServices
        {
            get
            {
                //if (DesktopMefHostServices.s_defaultServices == null)
                //    Interlocked.CompareExchange<MefHostServices>(ref DesktopMefHostServices.s_defaultServices,
                //        MefHostServices.Create((IEnumerable<Assembly>)DesktopMefHostServices.DefaultAssemblies), 
                //        (MefHostServices)null);
                return DesktopMefHostServices.s_defaultServices;
            }
        }

        private static ImmutableArray<Assembly> DefaultAssemblies
        {
            get
            {
                if (new ImmutableArray<Assembly>?(DesktopMefHostServices.s_defaultAssemblies) == new ImmutableArray<Assembly>?())
                    ImmutableInterlocked.InterlockedCompareExchange<Assembly>(ref DesktopMefHostServices.s_defaultAssemblies,
                    DesktopMefHostServices.CreateDefaultAssemblies(), new ImmutableArray<Assembly>());
                return DesktopMefHostServices.s_defaultAssemblies;
            }
        }

        private static ImmutableArray<Assembly> CreateDefaultAssemblies()
        {
            //return ImmutableArrayExtensions.Concat<Assembly>(MefHostServices.DefaultAssemblies, MefHostServices.LoadNearbyAssemblies(new string[1]
            var array = new ImmutableArray<Assembly>();
            
            var num = MefHostServices.DefaultAssemblies.GetEnumerator();
            while (num.MoveNext())
                array.Add(num.Current);

            //var nearby = MefHostServices.LoadNearbyAssemblies(new string[1]
            //  {
            //    "MSBuildWorkspace"
            //     "Microsoft.CodeAnalysis.Workspaces.Desktop"
            //});
            array.Add(Assembly.GetExecutingAssembly()); // nearby[0]);

            return array;
        }
    }
}


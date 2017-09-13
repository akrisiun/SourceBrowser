using System;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public static class WorkspaceHacks
    {
        public static HostServices Pack { get; private set; }
        public static HostServices Pack2 { get; private set; }

        static WorkspaceHacks() { LoadedList = new List<Assembly>(); }

        public static bool Prepare()
        {
            if (Pack != null)
                return true;  // prepared

            var assemblyNames = new[]
            {
                // .dll problems list
                "System.Collections.Immutable.dll",
                "System.Composition.TypedParts.dll",        // 1.0.30.0
                "System.Composition.Hosting.dll",
                "System.Composition.AttributedModel.dll",

                // https://stackoverflow.com/questions/403731/strong-name-validation-failed
                // reg ADD "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\*,*" /f
                "Microsoft.Build.Framework.dll",
                "Microsoft.Build.dll",

                "Microsoft.CodeAnalysis.Workspaces.dll",
                "Microsoft.CodeAnalysis.Workspaces.Desktop.dll",
                "Microsoft.CodeAnalysis.CSharp.dll",
                "Microsoft.CodeAnalysis.CSharp.Workspaces.dll",
                "Microsoft.CodeAnalysis.Features.dll",
                "Microsoft.CodeAnalysis.CSharp.Features.dll",
                
            };

            var assemblyNamesA = new[]
            {
                "System.IO.FileSystem.dll", //, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' 
                "System.IO.FileSystem.Primitives.dll"
            };

            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            lastDll = "???";
            IEnumerable<Assembly> assembliesIO = null;
            IEnumerable<Assembly> assemblies = null;

            try
            {
                assembliesIO = assemblyNamesA
                    .Select(n => Load(n)).ToList();
            } catch { }

            try
            {
                assemblies = assemblyNames
                    .Select(n => Load(n)).ToList();

                // default
                Pack = MefHostServices.Create(MefHostServices.DefaultAssemblies);

                // custom .dll pack
                Pack2 = MefHostServices.Create(assemblies);
                Pack = Pack2 ?? Pack;
            }
            catch (Exception ex) {
                Console.Write($"Mef {lastDll} load failed: {ex.Message}");
            } 

            return Pack != null;
        }

        static string BaseDir;
        static string lastDll;
        static List<Assembly> LoadedList;
        static Assembly Load(string dll)
        {
            lastDll = Path.Combine(BaseDir, dll);

            var asm = Assembly.LoadFrom(lastDll);
            LoadedList.Add(asm);
            return asm;
        }

        public static dynamic GetSemanticFactsService(Document document)
        {
            return GetService(document, "Microsoft.CodeAnalysis.LanguageServices.ISemanticFactsService", "Microsoft.CodeAnalysis.Workspaces");
        }

        public static dynamic GetSyntaxFactsService(Document document)
        {
            return GetService(document, "Microsoft.CodeAnalysis.LanguageServices.ISyntaxFactsService", "Microsoft.CodeAnalysis.Workspaces");
        }

        public static object GetMetadataAsSourceService(Document document)
        {
            var language = document.Project.Language;
            var workspace = document.Project.Solution.Workspace;
            var serviceAssembly = Assembly.Load("Microsoft.CodeAnalysis.Features");
            var serviceInterfaceType = serviceAssembly.GetType("Microsoft.CodeAnalysis.MetadataAsSource.IMetadataAsSourceService");
            var result = GetService(workspace, language, serviceInterfaceType);
            return result;
        }

        private static object GetService(Workspace workspace, string language, Type serviceType)
        {
            var languageServices = workspace.Services.GetLanguageServices(language);
            var languageServicesType = typeof(HostLanguageServices);
            var genericMethod = languageServicesType.GetMethod("GetService", BindingFlags.Public | BindingFlags.Instance);
            var closedGenericMethod = genericMethod.MakeGenericMethod(serviceType);
            var result = closedGenericMethod.Invoke(languageServices, new object[0]);
            if (result == null)
            {
                throw new NullReferenceException("Unable to get language service: " + serviceType.FullName + " for " + language);
            }

            return result;
        }

        private static object GetService(Document document, string serviceType, string assemblyName)
        {
            var assembly = typeof(Document).Assembly;
            var documentExtensions = assembly.GetType("Microsoft.CodeAnalysis.Shared.Extensions.DocumentExtensions");
            var serviceAssembly = Assembly.Load(assemblyName);
            var serviceInterfaceType = serviceAssembly.GetType(serviceType);
            var getLanguageServiceMethod = documentExtensions.GetMethod("GetLanguageService");
            getLanguageServiceMethod = getLanguageServiceMethod.MakeGenericMethod(serviceInterfaceType);
            var service = getLanguageServiceMethod.Invoke(null, new object[] { document });
            return service;
        }
    }
}

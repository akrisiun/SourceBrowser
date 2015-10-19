using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class Federation
    {
        public static IEnumerable<string> FederatedIndexUrls = new[] { @"http://referencesource.microsoft.com", @"http://source.roslyn.io" };

        private List<HashSet<string>> assemblies = new List<HashSet<string>>();

        public static Federation Instance { get; protected set; }
        public static List<HashSet<string>> ReferenceSourceAssemblies()
        {
            var instance = Instance ?? new Federation(FederatedIndexUrls.First());
            return instance.assemblies;
        }

        public Federation() : this(FederatedIndexUrls)
        {
        }

        public Federation(IEnumerable<string> servers) : this(servers.ToArray())
        {
        }

        public Federation(params string[] servers)
        {
            if (servers == null || servers.Length == 0)
            {
                return;
            }

            foreach (var server in servers)
            {
                var url = this.GetAssemblyUrl(server);

                string assemblyList = "";
                try
                {
                    assemblyList = new WebClient().DownloadString(url);
                }
                catch
                { 
                    // clnt Proxy Authentication error
                    assemblyList = ms_assemblies;
                }

                var assemblyNames = new HashSet<string>(assemblyList
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Split(';')[0]), StringComparer.OrdinalIgnoreCase);

                assemblies.Add(assemblyNames);
            }

            Instance = this;
        }

        private string GetAssemblyUrl(string server)
        {
            var url = server;
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            url += "Assemblies.txt";

            return url;
        }

        public int GetExternalAssemblyIndex(string assemblyName)
        {
            for (int i = 0; i < assemblies.Count; i++)
            {
                if (assemblies[i].Contains(assemblyName))
                {
                    return i;
                }
            }

            return -1;
        }

        private const string ms_assemblies = 
@"Accessibility;0;11
ComSvcConfig;37;0
FxCopTask;-1;0
GuidAssembly;-1;0
Microsoft.Build.Engine;1;3
Microsoft.Build.Framework;2;10
Microsoft.Build.Tasks.v12.0;-1;0
Microsoft.Build.Tasks.v4.0;3;3
Microsoft.Build.Utilities.v3.5;4;2
Microsoft.Build.Utilities.v4.0;5;7
Microsoft.CSharp;6;2
Microsoft.Data.Entity.Build.Tasks;-1;0
Microsoft.JScript;7;2
Microsoft.QualityTools.Testing.Fakes.Tasks;-1;0
Microsoft.Transactions.Bridge;8;1
Microsoft.VisualBasic;10;2
Microsoft.VisualBasic.Activities.Compiler;9;1
Microsoft.VisualStudio.TestTools.BuildShadowsTask;-1;0
Microsoft.Web.Administration;11;1
MSBuildFiles;-1;0
MSBuildItems;-1;0
MSBuildProperties;-1;0
MSBuildTargets;-1;0
MSBuildTasks;-1;0
mscorlib;45;110
PresentationBuildTasks;102;0
PresentationCore;103;12
PresentationFramework;101;11
PresentationFramework.Aero;104;0
PresentationFramework.Classic;105;0
PresentationFramework.Luna;106;0
PresentationFramework.Royale;107;0
PresentationUI;78;5
ReachFramework;79;4
SMDiagnostics;33;13
SMSvcHost;34;0
svcutil;38;0
System;65;103
System.Activities;19;7
System.Activities.Core.Presentation;26;0
System.Activities.DurableInstancing;18;1
System.Activities.Presentation;27;1
System.AddIn;48;1
System.AddIn.Contract;49;3
System.ComponentModel.Composition;80;1
System.ComponentModel.DataAnnotations;74;4
System.Configuration;51;46
System.Configuration.Install;81;4
System.Core;52;54
System.Data;61;33
System.Data.DataSetExtensions;55;3
System.Data.Entity;54;7
System.Data.Entity.Design;53;0
System.Data.Linq;62;4
System.Data.Services;60;0
System.Data.Services.Client;58;4
System.Data.Services.Design;59;2
System.Data.SqlXml;71;0
System.Deployment;82;5
System.Design;83;12
System.DirectoryServices;84;14
System.DirectoryServices.Protocols;85;1
System.Drawing;66;28
System.Drawing.Design;86;4
System.EnterpriseServices;87;9
System.IdentityModel;30;13
System.IdentityModel.Selectors;35;1
System.IO.Log;15;0
System.Management;69;4
System.Management.Automation;88;1
System.Messaging;12;4
System.Net;68;0
System.Net.Http;89;6
System.Numerics;64;3
System.Printing;90;5
System.Runtime.Caching;50;1
System.Runtime.DurableInstancing;20;12
System.Runtime.Remoting;46;7
System.Runtime.Serialization;31;26
System.Runtime.Serialization.Formatters.Soap;91;3
System.Security;47;29
System.ServiceModel;32;15
System.ServiceModel.Activation;36;8
System.ServiceModel.Activities;21;4
System.ServiceModel.Channels;22;2
System.ServiceModel.Discovery;23;0
System.ServiceModel.Internals;29;23
System.ServiceModel.Routing;24;0
System.ServiceModel.WasHosting;41;0
System.ServiceModel.Web;16;1
System.ServiceProcess;92;8
System.Transactions;13;25
System.Web;77;23
System.Web.ApplicationServices;73;5
System.Web.DynamicData;75;0
System.Web.Entity;57;2
System.Web.Entity.Design;56;0
System.Web.Extensions;76;5
System.Web.Extensions.Design;93;1
System.Web.Mobile;63;0
System.Web.RegularExpressions;94;4
System.Web.Services;14;14
System.Windows.Forms;67;23
System.Windows.Input.Manipulations;95;1
System.Windows.Presentation;96;1
System.Workflow.Activities;42;2
System.Workflow.ComponentModel;43;3
System.Workflow.Runtime;44;1
System.WorkflowServices;17;0
System.Xaml;97;29
System.Xaml.Hosting;25;1
System.Xml;72;73
System.Xml.Linq;70;24
UIAutomationClient;108;3
UIAutomationClientsideProviders;109;1
UIAutomationProvider;98;12
UIAutomationTypes;99;11
WindowsBase;110;16
WindowsFormsIntegration;100;1
WsatConfig;39;0
WsatUI;40;0
XamlBuildTask;28;0
XsdBuildTask;-1;0";

    }
}

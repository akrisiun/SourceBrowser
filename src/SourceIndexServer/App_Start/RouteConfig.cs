using Microsoft.SourceBrowser.SourceIndexServer.Controllers;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Microsoft.SourceBrowser.SourceIndexServer
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.Clear();
            // routes.IgnoreRoute("");
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("{resource}.aspx/{*pathInfo}");

            ControllerDefault.MapDefault("file");
            ControllerDefault.SetFactory<FileController>();
            ControllerDefault.ChooseController = (ctx, controller) =>
            {
                if (controller.IndexOf("api", comparisonType: StringComparison.InvariantCultureIgnoreCase) >= 0)
                    return null; // new SymbolsController();

                return new FileController();
            }; 
        }
    }
}



namespace Source
{
    public static class EngineDebug
    {
        public static void Output(HttpResponse Response)
        {
            Response.Write("<br/>HostingEnvironment.ApplicationPhysicalPath=" + System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath);
            Response.Write("<br/>HostingEnvironment.ApplicationVirtualPath=" + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            Response.Write("<br/>HostingEnvironment.SiteName=" + System.Web.Hosting.HostingEnvironment.SiteName + "\n");
            //RazorGenerator.Mvc.EngineDebug.ListRoutes(Response);

            try
            {
                var index = Microsoft.SourceBrowser.SourceIndexServer.Models.Index.Instance;

                Response.Write(String.Format("<br/>Index.RootPath={0}\n", index.RootPath));
                Response.Write(String.Format("<br/>Index.ProjPath={0}\n", index.ProjPath));
                // RoutingModule.EnumerateHttpModules(url, Response);
            }
            catch (Exception ex) { Response.Write(String.Format("Route modules error {0}", ex.Message)); }
        }

    }
}
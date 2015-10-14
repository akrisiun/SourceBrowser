using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;

[assembly: System.Web.PreApplicationStartMethod(typeof(Microsoft.SourceBrowser.SourceIndexServer.Start), "Up")]

namespace Microsoft.SourceBrowser.SourceIndexServer
{
    public static class Start
    {
        public static void Up()
        {
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
        public static bool Started { get {return posted;} }
        static bool posted = false;

        public static void PostUp()
        {
            if (posted)
                return;
            posted = true;
            AreaRegistration.RegisterAllAreas();
        }
    }
}
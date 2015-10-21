using Microsoft.SourceBrowser.Common;
using Microsoft.SourceBrowser.SourceIndexServer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;

namespace Microsoft.SourceBrowser.SourceIndexServer.Controllers
{
    public class ControllerDefault : DefaultControllerFactory
    {
        public static void SetFactory<T>() where T : Controller
        {
            DefaultType = typeof(T);
            ControllerBuilder.Current.SetControllerFactory(new ControllerDefault());
        }

        public static void MapDefault(string controller = null, string action = "Default")
        {
            RouteCollection routes = RouteTable.Routes;

            routes.IgnoreRoute("{WebPage}.aspx/{*pathInfo}");
            routes.MapRoute(
                         name: "Default",
                         url: "{controller}/{action}/{ext}",
                         defaults: new
                         {
                             controller = controller,
                             action = action,
                             ext = UrlParameter.Optional
                         }
                     );
        }

        public static string Segment2(RequestContext requestContext)
        {
            var routeData = requestContext.RouteData;
            string segment2 = string.Empty;
            if (routeData.Values != null)
            {
                var values = routeData.Values.GetEnumerator();
                while (values.MoveNext())
                {
                    if (!string.IsNullOrWhiteSpace(values.Current.Value as string))
                    {
                        segment2 = values.Current.Value as string;
                        break;
                    }
                }
            }
            return segment2;
        }

        public static Type DefaultType { get; private set; }
        public static Func<RequestContext, string, Controller> ChooseController { get; set; }

        public override IController CreateController(RequestContext requestContext, string controllerName)
        {
            Controller controller = null;

            if (ChooseController != null)
                controller = ChooseController(requestContext, controllerName);

            if (controller == null)
                controller = Activator.CreateInstance(DefaultType) as Controller;

            return controller;
        }

        public override void ReleaseController(IController controller)
        {
            var disposable = controller as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }


    public class FileController : Controller
    {
        public FileController()
        {
        }

        // GET: File
        public ActionResult Index()
        {
            HttpContext.Trace.Write("file.Index=" + HttpContext.Request.Url.PathAndQuery);

            return IndexFile();
            
        }

        ActionResult IndexFile()
        {
            var path = HostingEnvironment.ApplicationPhysicalPath + "index.html";
            HttpContext.Trace.Write("file.path=" + path);

            // IIS7 HttpRuntime.FinishRequestNotification handle is invalid
            //   return new FilePathResult(path, "text/html");
            if (!System.IO.File.Exists(path))
                return null;

            var content = System.IO.File.ReadAllBytes(path);
            var res = new FileContentResult(content, "text/html");
            return res;
        }

        protected override void HandleUnknownAction(string actionName)
        {
            var url = Request.Url;
            HttpContext.Trace.Write("File.Uknown=" + url.PathAndQuery + " localPath=" + (url.LocalPath ?? "-"));
            if (url.LocalPath == "/" || url.PathAndQuery == "/")
            {
                var res = IndexFile();
                res.ExecuteResult(ControllerContext);

                Response.StatusCode = 200;
                return;
            }

            // base.HandleUnknownAction(actionName);
            var path = url.AbsolutePath;
            var index = Microsoft.SourceBrowser.SourceIndexServer.Models.Index.Instance;
            //index.File();

            var symbol = Request.Params["symbol"] as string;
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                string api = GetHtml(symbol);
                var res = new FileContentResult(Encoding.ASCII.GetBytes(api), "text/html");
                res.ExecuteResult(this.ControllerContext);
            }
        }

        private const int MaxInputLength = 260;
        private static readonly Dictionary<string, int> usages = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly DateTime serviceStarted = DateTime.UtcNow;
        private static int requestsServed = 0;


        string GetHtml(string symbol)
        {
            string result = null;
            try
            {
                result = GetHtmlCore(symbol);
            }
            catch (Exception ex)
            {
                result = Markup.Note(ex.ToString());
            }
            return result;
        }

        static string GetHtmlCore(string symbol, string usageStats = null)
        {
            if (symbol == null || symbol.Length < 3)
            {
                return Markup.Note("Enter at least 3 characters.");
            }

            if (symbol.Length > 260)
            {
                return Markup.Note(string.Format(
                    "Query string is too long (maximum is {0} characters, input is {1} characters)",
                    MaxInputLength,
                    symbol.Length));
            }

            using (Disposable.Timing("Get symbols"))
            {
                Stopwatch sw = Stopwatch.StartNew();

                var index = Models.Index.Instance;
                var query = index.Get(symbol);

                var result = new ResultsHtmlGenerator(query).Generate(sw, index, usageStats);
                return result;
            }
        }

        private string UpdateUsages()
        {
            lock (usages)
            {
                var userName = this.User.Identity.Name;
                int requests = 0;
                usages.TryGetValue(userName, out requests);
                requests++;
                requestsServed++;
                usages[userName] = requests;
                return string.Format(
                    "Since {0}:<br>&nbsp;&nbsp;{1} unique users<br>&nbsp;&nbsp;{2} requests served.",
                    serviceStarted.ToLocalTime().ToString("m"),
                    usages.Keys.Count,
                    requestsServed);
            }
        }

    }
}

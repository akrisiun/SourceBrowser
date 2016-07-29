using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Source
{
    public class HandlerModule : IHttpModule, IHttpHandler, IDisposable
    {
        // bool ProcessRequestCore(RequestEvent requestEvent);
        // RequestProcessState RequestProcessState { get; }

        static HandlerModule() { }
 
        #region App static

        static HttpResponse Response { get { return HttpContext.Current == null ? null : HttpContext.Current.Response; } }
        static HttpRequest Request { get { return HttpContext.Current == null ? null : HttpContext.Current.Request; } }
        static HttpServerUtility Server { get { return HttpContext.Current == null ? null : HttpContext.Current.Server; } }

        public static void AppEndRequest()
        {
            if (Response == null)
                return;

            if (Response.StatusCode >= 400) // == 404)
            {
                var exception = Server.GetLastError();

                Response.Clear();
                Response.Write("Status=" + Response.StatusCode);
                Response.Write(" Url=" + Request.Url);

                if (exception != null)
                {
                    Response.Write("Error " + exception.Message);
                    Response.Write("<br>" + exception.StackTrace);
                }

                Server.ClearError();
            }
        }
        #endregion

        public void Dispose() { }

        public void ProcessRequest(HttpContext context) { 
            HttpResponse response = Response;
            if (response.StatusCode >= 400)
            {
                AppEndRequest();
                return;
            }

            response.Clear();
            try
            {
                // Make

                // Caching:
                response.Cache.SetCacheability(HttpCacheability.ServerAndPrivate);
                response.Cache.SetOmitVaryStar(true);
                response.Cache.SetLastModifiedFromFileDependencies();
            }
            catch (Exception ex)
            {
                response.Clear();
                response.StatusCode = 500;
                response.StatusDescription = ex.Message;
            }
        }

        #region IHttpModule

        public const string Flag = "HandlerModuleRegistered";
        void IHttpModule.Init(HttpApplication context)
        {
            HttpContext.Current.Application[Flag] = true;

            if (!Microsoft.SourceBrowser.SourceIndexServer.Start.Started)
                Microsoft.SourceBrowser.SourceIndexServer.Start.PostUp();
        }
        void IHttpModule.Dispose()
        {
            Dispose();
        }

        bool IHttpHandler.IsReusable
        {
            get { return true; }
        }
        #endregion

    }
}
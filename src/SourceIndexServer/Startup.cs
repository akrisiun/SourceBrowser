using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.SourceBrowser.SourceIndexServer.Models;

namespace Microsoft.SourceBrowser.SourceIndexServer
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Environment = env;
        }

        public IHostingEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            RootPath = Environment.WebRootPath ?? Directory.GetCurrentDirectory();
            var folder = System.Environment.GetEnvironmentVariable("FOLDER") ?? "";
            if (folder.Length > 0 && Directory.Exists(folder))
                RootPath = folder;
            
            var subfolder = Path.Combine(RootPath, "Index");
            if (File.Exists(Path.Combine(subfolder, "Projects.txt")))
            {
                RootPath = subfolder;
            }

            Console.WriteLine($"RootPath={RootPath}");

            services.AddSingleton(new Index(RootPath));
            services.AddMvc();
        }

        public static string RootPath { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-UA-Compatible"] = "IE=edge";
                string path = context.Request.Path.Value ?? "";
                if (path.Length >= 2 && path.StartsWith("/@"))
                {
                    path = context.Request.Scheme + "://" + context.Request.Host.Value
                         + (path.Length == 2 ? "" : "/" + path.Substring(2, path.Length - 2));
                    context.Response.Redirect(path);
                }
                else 
                    await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // TODO: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/url-rewriting?tabs=aspnetcore1x
            /* HTTPS redirect
            var options = new RewriteOptions()
                .AddRedirectToHttps(301, 5001);

            app.UseRewriter(options);
            */

            var baseDir = System.AppContext.BaseDirectory;
            var root = Index.RootPath;

            var feat = app.ServerFeatures.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            string addr = feat != null ? System.Linq.Enumerable.FirstOrDefault(feat.Addresses) ?? "" : "-";

            Console.WriteLine($"{env.EnvironmentName} Content: {root}  Host:{addr}");

            var DefaultFile = new DefaultFilesOptions();
            DefaultFile.DefaultFileNames.Clear();
            DefaultFile.DefaultFileNames.Add("index.html");

            app.UseDefaultFiles(DefaultFile);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(RootPath),
            });
            app.UseStaticFiles();

            // https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/routing
            app.UseMvc(routes =>
            {
              routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

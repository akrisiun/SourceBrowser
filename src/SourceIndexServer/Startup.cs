using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.SourceBrowser.SourceIndexServer.Models;
using System;

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
            var root = Environment.WebRootPath;
            var folder = System.Environment.GetEnvironmentVariable("FOLDER") ?? "";
            if (folder.Length > 0 && Directory.Exists(folder))
                root = folder;

            services.AddSingleton(new Index(root));

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-UA-Compatible"] = "IE=edge";
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var baseDir = System.AppContext.BaseDirectory;
            var root = Index.RootPath;

            //if (env.ContentRootPath != null && Directory.Exists(Path.Combine(env.ContentRootPath, "index")))
            //    root = Path.Combine(env.ContentRootPath, "index");
            //else if (Directory.Exists(Path.Combine(root, "wwwroot")))
            //    root = Path.Combine(root, "wwwroot");

            var feat = app.ServerFeatures.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            string addr = feat != null ? System.Linq.Enumerable.FirstOrDefault(feat.Addresses) ?? "" : "-";

            Console.WriteLine($"{env.EnvironmentName} Content: {root}  Host:{addr}");

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(root)
            });
            app.UseStaticFiles();

            app.UseMvc();

        }
    }
}

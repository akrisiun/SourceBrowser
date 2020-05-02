﻿using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.SourceBrowser.SourceIndexServer.Models;

namespace Microsoft.SourceBrowser.SourceIndexServer
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            Environment = env;
        }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            RootPath = Path.Combine(Environment.ContentRootPath, "Index");

            var subfolder = Path.Combine(RootPath, "Index");
            if (File.Exists(Path.Combine(subfolder, "Projects.txt")))
            {
                RootPath = subfolder;
            }

            services.AddSingleton(new Models.Index(RootPath));
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        public string RootPath { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers["X-UA-Compatible"] = "IE=edge";
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            
            Console.WriteLine("RootPath=" + Path.GetFullPath(RootPath));
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(RootPath),
            });
            app.UseStaticFiles();
            app.UseRouting();


            app.UseEndpoints(endPoints =>
            {
                endPoints.MapRazorPages();
                endPoints.MapControllers();
            });
        }
    }
}

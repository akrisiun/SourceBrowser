using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.SourceBrowser.SourceIndexServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting program..");

            string[] urls = new[] { "http://0.0.0.0:5000" }; // default

            if (args != null && args.Length > 0)
            {
                bool isDebug = Enumerable.Select(args, a => a.Equals("-debug")).FirstOrDefault();
                if (isDebug)
                {
                    Console.WriteLine("Debug hosting?");
                    Console.ReadKey();
                    if (Debugger.IsAttached)
                        Debugger.Break();
                }

                // --urls "http://*:5001"
                var list = args.GetEnumerator();
                while (list.MoveNext())
                {
                    var item = list.Current as string ?? "";
                    if (item.Equals("--urls") && list.MoveNext())
                    {
                        item = list.Current  as string;
                        urls[0] = item ?? urls[0];

                        Console.WriteLine($"--urls {item}");
                        break;
                     }
                }

            }

            var host = new WebHostBuilder()
                .UseUrls(urls)
                .UseSetting("detailedErrors", "true")
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}

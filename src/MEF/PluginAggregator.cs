using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.SourceBrowser.MEF
{
    public class PluginAggregator : IReadOnlyCollection<SourceBrowserPluginWrapper>, IDisposable
    {
        private CompositionContainer container;

        // Assembly System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
        [ImportMany]
#pragma warning disable CS0649
        IEnumerable<Lazy<ISourceBrowserPlugin, ISourceBrowserPluginMetadata>> plugins;
#pragma warning restore CS0649
        private List<SourceBrowserPluginWrapper> Plugins;
        private ILog Logger;

        private Dictionary<string, Dictionary<string, string>> PluginConfigurations;

        public Exception LoadErrors { get; set; }

        public int Count
        {
            get
            {
                return Plugins.Count;
            }
        }

        public PluginAggregator(Dictionary<string, Dictionary<string, string>> pluginConfigurations, ILog logger, IEnumerable<string> blackList)
        {
            PluginConfigurations = pluginConfigurations;
            Logger = logger;

            //Create the CompositionContainer with the parts in the catalog
            container = new CompositionContainer(new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory));

            var BlackListSet = new HashSet<string>(blackList);
        }
#pragma warning disable CS0649
        public  HashSet<string> BlackListSet {get; set;}
#pragma warning restore CS0649

        public void Wrap()
        {
            try
            {

                //Fill the imports of this object
                container.ComposeParts(this);

                Plugins = plugins
                .Select(pair => new SourceBrowserPluginWrapper(pair.Value, pair.Metadata, Logger))
                .Where(w => !BlackListSet.Contains(w.Name))
                .ToList();
            }
            catch (Exception ex)
            {
                // Assembly load errors
                LoadErrors = ex.InnerException ?? ex;
            }
        }

        public void Init()
        {
            foreach (var plugin in Plugins)
            {
                Dictionary<string, string> config;
                if (!PluginConfigurations.TryGetValue(plugin.Name, out config))
                {
                    config = new Dictionary<string, string>();
                }
                plugin.Init(config, Logger);
            }
        }

        public IEnumerable<ISymbolVisitor> ManufactureSymbolVisitors(Project project)
        {
            return Enumerable.Empty<ISymbolVisitor>();
            //try
            //{
            //    return Plugins?.SelectMany(p => p.ManufactureSymbolVisitors(project.FilePath));
            //}
            //catch (Exception ex)
            //{
            //    Logger.Info("Plugin failed to manufacture ISymbolVisitor visitors", ex);
            //    return Enumerable.Empty<ISymbolVisitor>();
            //}
        }

        private IEnumerable<ISymbolVisitor> ManufactureSymbolVisitors(string name, ISourceBrowserPlugin plugin, Project project)
        {
            //try
            //{
            //    return plugin.ManufactureSymbolVisitors(project.FilePath);
            //}
            //catch (Exception ex)
            //{
            //    Logger.Info(name + " Plugin failed to manufacture symbol visitors", ex);
                return Enumerable.Empty<ISymbolVisitor>();
            //}
        }

        public IEnumerable<ITextVisitor> ManufactureTextVisitors(Project project)
        {
            return Plugins?.SelectMany(p => p.ManufactureTextVisitors(project.FilePath));
        }

        public void Dispose()
        {
            if (container != null)
                container.Dispose();
        }

        public IEnumerator<SourceBrowserPluginWrapper> GetEnumerator()
        {
            return Plugins?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Plugins?.GetEnumerator();
        }
    }
}

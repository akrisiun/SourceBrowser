using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using System.IO;
using System.Reflection;
using System.Collections.Immutable;
using System.Linq;

namespace Mef
{
    public class ProjectMap : IDictionary<string, object> // , IList<object>
    {
        protected Dictionary<string, object> data;

        public static string SolutionPath { get; set; }
        public static Assembly Versioning;

        public ProjectMap()
        {
            data = new Dictionary<string, object>();
        }

        public IDictionary<string, string> GetProperties(string csproj, IDictionary<string, string> dict = null)
        {
            var solutionFilePath = csproj;

            var properties = ImmutableDictionary<string, string>.Empty;
            if (dict != null)
                properties.AddRange(dict);

            var key = CsProjKey(csproj);
            properties = properties.Add("TargetPath", Path.GetDirectoryName(solutionFilePath) + @"\bin\" + key);

            properties = properties.Add("MSBuildRuntimeVersion", "4.0.30319");
            properties = properties.Add("MSBuildAssemblyVersion", "15.0");
            if (PlatformID.Win32NT.Equals(Environment.OSVersion.Platform)) //as OperatingSystem
                properties = properties.Add("OS", "Windows_NT");

            var msb = this[key] as Microsoft.Build.Evaluation.Project;
            var p = msb?.Properties; // null?
            var net = p?.SingleOrDefault(x => x.Name == "TargetFramework")?.EvaluatedValue ?? "net461";
            properties = properties.Add("TargetFramework", net);

            return properties;
        }

        public IDictionary<string, string> PropertiesImmutable(IDictionary<string, string> Properties)
        { 
            var properties = ImmutableDictionary<string, string>.Empty;
            if (properties != null)
                properties.SetItems(Properties);
            return properties;
        }

        public Project Project(string key) => this[key] as Project;

        // MSB : Microsoft.Build.Evaluation.Project
        public Project ProjectGetOrRead(string csproj)
        {
            var key = CsProjKey(csproj);
            var msb = this[key] as Project;
            if (msb != null)
                return msb;

            var dir = Path.GetDirectoryName(csproj);

            if (Versioning == null)
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dll = Path.Combine(baseDir, "NuGet.Versioning.dll");
                if (File.Exists(dll))
                    Versioning = Assembly.LoadFile(dll);
                else
                    Console.WriteLine($"Missed file {dll}");    // posible problems with SDK .csproj...
            }

#if OMNI
            //  with OmniSharp.MSBuild.dll
            using (var loader = Mef.ProjectLoader.Load(dir))
            {
                // NuGet.Versioning, Version=4.3.0.5, Culture=neutral, PublicKeyToken=31bf3856ad364e35
                msb = loader.EvaluateProjectFile(csproj);
                if (msb != null)
                    Add(key, msb);
            }
#endif

            return msb;
        }

        public string CsProjKey(string csproj) =>
            Path.GetFileNameWithoutExtension(csproj) + ".dll";

        public object this[string key] {
            [DebuggerStepThrough]
            get {
                if (data.ContainsKey(key)) return data[key];
                if (data.ContainsKey(key + ".dll")) return data[key + ".dll"];
                return null;
            }
            set {
                if (data.ContainsKey(key))
                    data[key] = value;
                else
                    Add(key, value);
            }
        }

        public ICollection<string> Keys => data.Keys;
        public ICollection<object> Values => data.Values;
        public int Count => data.Count;
        public bool IsReadOnly => false;

        public void Add(string key, object value) => data.Add(key, value);
        public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);
        public void Clear() => data.Clear();
        public bool Remove(string key) => data.Remove(key);
        public bool Remove(KeyValuePair<string, object> item) => data.Remove(item.Key);

        public bool Contains(KeyValuePair<string, object> item) => data.ContainsKey(item.Key);
        public bool ContainsKey(string key) => data.ContainsKey(key);
        public bool TryGetValue(string key, out object value) => data.TryGetValue(key, out value);

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) { } // => data.CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

    }
}

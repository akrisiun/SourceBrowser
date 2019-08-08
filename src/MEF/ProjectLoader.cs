#if OMNI
using Microsoft.Extensions.Logging;
using OmniSharp.Eventing;
using OmniSharp.MSBuild;
using OmniSharp.MSBuild.Logging;
using OmniSharp.Options;
using OmniSharp.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using MSB = Microsoft.Build;

namespace Mef
{
    // OmniSharp.MsBuild part:  SDK .csproj loader

    public class ProjectLoader : IDisposable
    {
        #region ctor 

        private readonly Dictionary<string, string> _globalProperties;
        private readonly MSBuildOptions _options;
        private readonly SdksPathResolver _sdksPathResolver;

        public void Dispose() { 
            // _sdksPathResolver.Dispose();  
        }

        public static ProjectLoader Load(string solutionDirectory)
        {
            IEventEmitter mevent = NullEventEmitter.Instance;
            ILoggerFactory factory = new NullLoggerFactory();

            var dot = new DotNetCliService(factory, mevent);
            SdksPathResolver sdksPathResolver = new SdksPathResolver(dot);

            ImmutableDictionary<string, string> propertyOverrides = ImmutableDictionary<string, string>.Empty;
            var loader = new ProjectLoader(new MSBuildOptions(), solutionDirectory, propertyOverrides, sdksPathResolver);
            return loader;
        }

        public ProjectLoader(MSBuildOptions options, string solutionDirectory,
            ImmutableDictionary<string, string> propertyOverrides, // ILoggerFactory loggerFactory, 
            SdksPathResolver sdksPathResolver)
        {
            // _logger = loggerFactory.CreateLogger<ProjectLoader>();
            _options = options ?? new MSBuildOptions();
            _sdksPathResolver = sdksPathResolver ?? throw new ArgumentNullException(nameof(sdksPathResolver));
            _globalProperties = CreateGlobalProperties(_options, solutionDirectory, propertyOverrides); // , _logger);
        }

        private static Dictionary<string, string> CreateGlobalProperties(
            MSBuildOptions options, string solutionDirectory, ImmutableDictionary<string, string> propertyOverrides) // , ILogger logger)
        {
            var globalProperties = new Dictionary<string, string>
            {
                { PropertyNames.DesignTimeBuild, "true" },
                { PropertyNames.BuildingInsideVisualStudio, "true" },
                { PropertyNames.BuildProjectReferences, "false" },
                { PropertyNames._ResolveReferenceDependencies, "true" },
                { PropertyNames.SolutionDir, solutionDirectory + Path.DirectorySeparatorChar },

                // This properties allow the design-time build to handle the Compile target without actually invoking the compiler.
                // See https://github.com/dotnet/roslyn/pull/4604 for details.
                { PropertyNames.ProvideCommandLineArgs, "true" },
                { PropertyNames.SkipCompilerExecution, "true" }
            };

            object logger = null;
            globalProperties.AddPropertyOverride(PropertyNames.MSBuildExtensionsPath, options.MSBuildExtensionsPath,
                propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.TargetFrameworkRootPath, options.TargetFrameworkRootPath, 
                propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.RoslynTargetsPath, options.RoslynTargetsPath, propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.CscToolPath, options.CscToolPath, propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.CscToolExe, options.CscToolExe, propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.VisualStudioVersion, options.VisualStudioVersion, 
                propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.Configuration, options.Configuration, propertyOverrides, logger);
            globalProperties.AddPropertyOverride(PropertyNames.Platform, options.Platform, propertyOverrides, logger);

            return globalProperties;
        }

        #endregion

        public // (MSB.Execution.ProjectInstance projectInstance, ImmutableArray<MSBuildDiagnostic> diagnostics) 
            Tuple<MSB.Execution.ProjectInstance, ImmutableArray<MSBuildDiagnostic>>
            BuildProject(string filePath)
        {
            using (_sdksPathResolver.SetSdksPathEnvironmentVariable(filePath))
            {
                var evaluatedProject = EvaluateProjectFileCore(filePath);

                //  SetTargetFrameworkIfNeeded(evaluatedProject);
                var projectInstance = evaluatedProject.CreateProjectInstance();
                //  var msbuildLogger = new MSBuildLogger(_logger);
                var buildResult = false;
                //projectInstance.Build(
                //  targets: new string[] { TargetNames.Compile, TargetNames.CoreCompile },
                //  loggers: new[] { msbuildLogger });

                ImmutableArray<MSBuildDiagnostic> diagnostics = ImmutableArray<MSBuildDiagnostic>.Empty;
                // msbuildLogger.GetDiagnostics();

                return buildResult
                    ? new Tuple<MSB.Execution.ProjectInstance, ImmutableArray<MSBuildDiagnostic>>(projectInstance, diagnostics)
                    : new Tuple<MSB.Execution.ProjectInstance, ImmutableArray<MSBuildDiagnostic>>(null, diagnostics);
            }
        }

        public MSB.Evaluation.Project EvaluateProjectFile(string filePath)
        {
            using (_sdksPathResolver.SetSdksPathEnvironmentVariable(filePath))
            {
                return EvaluateProjectFileCore(filePath);
            }
        }

        private MSB.Evaluation.Project EvaluateProjectFileCore(string filePath)
        {
            // Evaluate the MSBuild project
            var projectCollection = new MSB.Evaluation.ProjectCollection(_globalProperties);

            var toolsVersion = _options.ToolsVersion;
            if (string.IsNullOrEmpty(toolsVersion) || Version.TryParse(toolsVersion, out _))
            {
                toolsVersion = projectCollection.DefaultToolsVersion;
            }

            toolsVersion = GetLegalToolsetVersion(toolsVersion, projectCollection.Toolsets);

            var vsV = Environment.GetEnvironmentVariable("VisualStudioVersion"); // @"14.0");
            if (toolsVersion == "2.0") //  && !string.IsNullOrWhiteSpace(vsV))
                toolsVersion = vsV ?? "14.0";

            return projectCollection.LoadProject(filePath, toolsVersion);
        }

        //private static void SetTargetFrameworkIfNeeded(MSB.Evaluation.Project evaluatedProject)
        //{
        //    var targetFramework = evaluatedProject.GetPropertyValue(PropertyNames.TargetFramework);
        //    var targetFrameworks = PropertyConverter.SplitList(evaluatedProject.GetPropertyValue(PropertyNames.TargetFrameworks), ';');

        //    // If the project supports multiple target frameworks and specific framework isn't
        //    // selected, we must pick one before execution. Otherwise, the ResolveReferences
        //    // target might not be available to us.
        //    if (string.IsNullOrWhiteSpace(targetFramework) && targetFrameworks.Length > 0)
        //    {
        //        // For now, we'll just pick the first target framework. Eventually, we'll need to
        //        // do better and potentially allow OmniSharp hosts to select a target framework.
        //        targetFramework = targetFrameworks[0];
        //        evaluatedProject.SetProperty(PropertyNames.TargetFramework, targetFramework);
        //    }
        //    else if (!string.IsNullOrWhiteSpace(targetFramework) && targetFrameworks.Length == 0)
        //    {
        //        targetFrameworks = ImmutableArray.Create(targetFramework);
        //    }
        //}

        private static string GetLegalToolsetVersion(string toolsVersion, ICollection<MSB.Evaluation.Toolset> toolsets)
        {
            // It's entirely possible the the toolset specified does not exist. In that case, we'll try to use
            // the highest version available.
            var version = new Version(toolsVersion);

            bool exists = false;
            Version highestVersion = null;

            var legalToolsets = new SortedList<Version, MSB.Evaluation.Toolset>(toolsets.Count);
            foreach (var toolset in toolsets)
            {
                // Only consider this toolset if it has a legal version, we haven't seen it, and its path exists.
                if (Version.TryParse(toolset.ToolsVersion, out var toolsetVersion) &&
                    !legalToolsets.ContainsKey(toolsetVersion) &&
                    Directory.Exists(toolset.ToolsPath))
                {
                    legalToolsets.Add(toolsetVersion, toolset);

                    if (highestVersion == null ||
                        toolsetVersion > highestVersion)
                    {
                        highestVersion = toolsetVersion;
                    }

                    if (toolsetVersion == version)
                    {
                        exists = true;
                    }
                }
            }

            if (highestVersion == null)
            {
                throw new InvalidOperationException("No legal MSBuild toolsets available.");
            }

            if (!exists)
            {
                toolsVersion = legalToolsets[highestVersion].ToolsPath;
            }

            return toolsVersion;
        }
    }

    internal static class Extensions
    {
        public static void AddPropertyOverride(
            this Dictionary<string, string> properties,
            string propertyName,
            string userOverrideValue,
            ImmutableDictionary<string, string> propertyOverrides,
            object logger)
        {
            var overrideValue = propertyOverrides.GetValueOrDefault(propertyName);

            if (!string.IsNullOrEmpty(userOverrideValue))
            {
                // If the user set the option, we should use that.
                properties.Add(propertyName, userOverrideValue);
                // logger.LogDebug($"'{propertyName}' set to '{userOverrideValue}' (user override)");
            }
            else if (!string.IsNullOrEmpty(overrideValue))
            {
                // If we have a custom environment value, we should use that.
                properties.Add(propertyName, overrideValue);
                // logger.LogDebug($"'{propertyName}' set to '{overrideValue}'");
            }
        }
    }

    internal static class PropertyNames
    {
        public const string DesignTimeBuild = nameof(DesignTimeBuild);
        public const string BuildingInsideVisualStudio = nameof(BuildingInsideVisualStudio);
        public const string BuildProjectReferences = nameof(BuildProjectReferences);
        public const string _ResolveReferenceDependencies = nameof(_ResolveReferenceDependencies);
        public const string SolutionDir = nameof(SolutionDir);
        public const string ProvideCommandLineArgs = nameof(ProvideCommandLineArgs);
        public const string SkipCompilerExecution = nameof(SkipCompilerExecution);

        public const string MSBuildExtensionsPath = nameof(MSBuildExtensionsPath);
        public const string TargetFrameworkRootPath = nameof(TargetFrameworkRootPath);
        public const string RoslynTargetsPath = nameof(RoslynTargetsPath);
        public const string CscToolPath = nameof(CscToolPath);
        public const string CscToolExe = nameof(CscToolExe);
        public const string VisualStudioVersion = nameof(VisualStudioVersion);
        public const string Configuration = nameof(Configuration);
        public const string Platform = nameof(Platform);
    }

    public class NullEventEmitter : IEventEmitter
    {
        public static IEventEmitter Instance { get; } = new NullEventEmitter();

        private NullEventEmitter() { }

        public void Emit(string kind, object args)
        {
            // nothing
        }
    }

    public class NullLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => new TraceLogger();

        public void Dispose() { }
    }

    public class TraceLogger : ILogger, IDisposable
    {
        public TraceLogger()
        {
        }

        public IDisposable BeginScope<TState>(TState state) => this;
        public void Dispose() { }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            Trace.WriteLine(exception?.Message ?? "");
        }
    }
}

#endif
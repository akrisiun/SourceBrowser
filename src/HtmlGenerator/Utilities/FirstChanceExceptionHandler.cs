using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Xml;
using Microsoft.SourceBrowser.Common;
using ExceptionAnalysis.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace Microsoft.SourceBrowser.HtmlGenerator
{
    public class FirstChanceExceptionHandler
    {
        #region Private 

        private static HashSet<Module> IgnoredModules = new HashSet<Module>();

        public static void IgnoreModules(IEnumerable<Module> t)
        {
            IgnoredModules.UnionWith(t);
        }

        private static HashSet<string> knownMessages = new HashSet<string>()
        {
            "Unable to load DLL 'api-ms-win-core-file-l1-2-0.dll': The specified module could not be found. (Exception from HRESULT: 0x8007007E)",
            "Invalid cast from 'System.String' to 'System.Int32[]'.",
            "The given assembly name or codebase was invalid. (Exception from HRESULT: 0x80131047)",
            "Value was either too large or too small for a Decimal.",
        };

        private static bool isReentrant = false;

        #endregion

        public static void HandleFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (isReentrant)
            {
                return;
            }

            isReentrant = true;
            try
            {
                var ex = e.Exception;
                ex = ex.InnerException ?? ex;

                #region Known #1

                if ( ex is EntryPointNotFoundException )
                {
                    if ( ex.Message.Contains("Unable to find an entry point named 'ReadFile'"))
                    {
                        return;
                    }
                }

                if (ex is InvalidCastException)
                {
                    if (ex.Message.Contains("Invalid cast from 'System.String' to"))
                    {
                        return;
                    }

                    if (ex.Message.Contains("Unable to cast object of type 'Microsoft.Build.Tasks.Windows.MarkupCompilePass1' to type 'Microsoft.Build.Framework.ITask'."))
                    {
                        return;
                    }
                }

                if (ex is InvalidOperationException)
                {
                    if (ex.Message.Contains("An attempt was made to transition a task to a final state when it had already completed."))
                    {
                        return;
                    }
                }

                if (ex is AggregateException)
                {
                    return;
                }

                if (ex is DecoderFallbackException)
                {
                    return;
                }

                if (ex is DirectoryNotFoundException)
                {
                    return;
                }

                if (ex is Microsoft.Build.Exceptions.InvalidProjectFileException)
                {
                    return;
                }

                if (ex is FileNotFoundException)
                {
                    return;
                }

                if (ex is MissingMethodException)
                {
                    // MSBuild evaluation has a known one
                    return;
                }

                if (ex is XmlException && ex.Message.Contains("There are multiple root elements"))
                {
                    return;
                }

                if (knownMessages.Contains(ex.Message))
                {
                    return;
                }

                string exceptionType = ex.GetType().FullName;

                if (exceptionType.Contains("UnsupportedSignatureContent"))
                {
                    return;
                }

                #endregion

                var message = ex?.Message ?? "";
                if (ex != null && ex.InnerException != null)
                    message += $"\n{ex.InnerException.Message}";

                if (message.Length == 0
                    || message.Contains("E_SQLITE")
                    || message.Contains(@"line switch for ""Csc.exe"".Value cannot be null"))
                {
                    //  "Could not load file or assembly 'E_SQLITE3.DLL'
                    //  Csc issues
                    return;
                }

                string stackTrace = ex.StackTrace ?? "";
                if (stackTrace.Contains("Antlr"))
                {
                    return;
                }

                if (stackTrace.Contains("at System.Guid.StringToInt"))
                {
                    return;
                }

                var trace = new TraceFactory().Manufacture(ex);

                if ( trace.Select(f => f.Method.Module).Any(IgnoredModules.Contains) )
                {
                    return;
                }

                var reflectionLoad = ex as System.Reflection.ReflectionTypeLoadException;
                if (reflectionLoad != null)
                {
                    var list = reflectionLoad.LoaderExceptions;
                    foreach (var error in list)
                    {
                        if (Debugger.IsAttached)
                            Trace.WriteLine($"Failed load {error.Message}");
                        else
                            Console.WriteLine($"Failed load {error.Message}");
                    }
                    return;
                }

                message = DateTime.Now.ToString() + ": First chance exception";
                if (SolutionGenerator.CurrentAssemblyName != null)
                {
                    message += " while processing assembly: " + SolutionGenerator.CurrentAssemblyName;

                    if (SolutionGenerator.CurrentAssemblyName == "Microsoft.VisualStudio.Diagnostics.ManagedHeapAnalyzerUnitTests" 
                        && ex.Message.Contains("The network path was not found"))
                    {
                        // their project file isn't authored correctly but we deal with it well
                        // so just ignore this one
                        return;
                    }
                }

                Log.Exception(ex, message, isSevere: false);
            }
            finally
            {
                isReentrant = false;
            }
        }
    }
}

using System;

namespace TraceOut
{
    using System.Linq;
    using System.Diagnostics;
    using System.IO;

    internal class ConsoleRedirector : IDisposable
    {
        private static StringWriter _consoleOutput = null;
        private TextWriter _originalConsoleOutput;

        internal ConsoleRedirector()
        {
            this._originalConsoleOutput = Console.Out;
            if (_consoleOutput == null)
            {
                _consoleOutput = new TraceStringWriter();
                Console.SetOut(_consoleOutput);
            }
        }
        ~ConsoleRedirector() { Dispose(); }

        public void Dispose()
        {
            if (_consoleOutput == null)
                return;
            Console.SetOut(_originalConsoleOutput);
            Console.Write(this.ToString());
            _consoleOutput.Dispose();
            _consoleOutput = null;
        }

        public override string ToString()
        {
            return _consoleOutput.ToString();
        }

        class TraceStringWriter : StringWriter
        {
            public override void Write(string value)
            {
                if (Listener.StackLevel <= 1)
                    Trace.Write(value);
            }

            public override void WriteLine(string value)
            {
                Listener.StackLevel++;
                if(Listener.StackLevel == 1)
                    Trace.WriteLine(value);
                Listener.StackLevel--;
            }
        }

    }
    public class Filter : TraceFilter
    {
        public override bool ShouldTrace(TraceEventCache cache, string source,
            TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
        {
            if (Listener.StackLevel > 1)
                return false;

            return true;
        }
    }

    public class Listener : TraceListener
    {
        public static void RemoveExisting()
        {
#if NET46            
            foreach (var existingListener in Debug.Listeners.OfType<TraceListener>().ToArray())
            {
                if (existingListener is DefaultTraceListener)
                {
                    Debug.Listeners.Remove(existingListener);
                }
            }
#endif
        }

        public static void Register()
        {
            // RemoveExisting(); // maybe not

            var loaded = System.AppDomain.CurrentDomain.GetData("AssertTrace_Loaded") as string;
            if (!"1".Equals(loaded))
                Debug.Listeners.Add(new Listener());

            System.AppDomain.CurrentDomain.SetData("AssertTrace_Loaded", "1");

            new ConsoleRedirector();

            Trace.WriteLine($"Trace Listener registered and alive");
        }

        public Listener()
        {
            Filter = new Filter();
        }

        public override void Fail(string message, string detailMessage)
        {
            if (message.Contains("This is a soft assert - I don't think this can happen"))
            {
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                message = "ASSERT FAILED";
            }

            if (detailMessage == null)
            {
                detailMessage = string.Empty;
            }

            string stackTrace = new StackTrace(true).ToString();

            if (stackTrace.Contains("OverriddenOrHiddenMembersHelpers.FindOverriddenOrHiddenMembersInType"))
            {
                // bug 661370
                return;
            }

            base.Fail(message, detailMessage);
            //  Log.Exception(message + "\r\n" + detailMessage + "\r\n" + stackTrace);
        }

        public static int StackLevel = 0;
        public override void Write(string message)
        {
            StackLevel++;
            if (StackLevel == 1)
                Trace.Write(message);

            if (StackLevel > 0)
                StackLevel--;
        }

        public override void WriteLine(string message)
        {
            //if (Log.WriteWrap == null)
            //    Log.Write(message);
            //else
            //{
            StackLevel++;
            if (StackLevel == 1)  // no recursion with stack overflow:
                Trace.WriteLine(message);

            if (StackLevel > 0)
                StackLevel--;

        }
    }

}
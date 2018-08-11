using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.SourceBrowser.Common
{
#if NETSTANDARD1_6 //  !NET46
    public class AssertTraceListener
    {
        public static void Register() { }
    }
#else
    public class AssertTraceListener : TraceListener
    {
        public static void Register()
        {
#if NET46            
            foreach (var existingListener in Debug.Listeners.OfType<TraceListener>().ToArray())
            {
                if (existingListener is DefaultTraceListener)
                {
                    Debug.Listeners.Remove(existingListener);
                }
            }
            var loaded = System.AppDomain.CurrentDomain.GetData("AssertTrace_Loaded") as string;
            if (!"1".Equals(loaded))
                Debug.Listeners.Add(new AssertTraceListener());
#endif
            System.AppDomain.CurrentDomain.SetData("AssertTrace_Loaded", "1");
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
            Log.Exception(message + "\r\n" + detailMessage + "\r\n" + stackTrace);
        }

        public override void Write(string message)
        {
            if (Log.WriteWrap == null)
                Log.Write(message);
        }

        public override void WriteLine(string message)
        {
            if (Log.WriteWrap == null)
               Log.Write(message);
            else
                Log.Output(message);
        }
    }
#endif
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using SourceBrowser;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TraceOut;

namespace Microsoft.SourceBrowser.HtmlGenerator.Tests
{
    [TestClass]
    public class Sln
    {
        [TestMethod]
        public void Test_SelfSln()
        {
            if (Debugger.IsAttached) {
                ProgramLoad.isDebug = true;
                // DebugConsole.Redirect();
                // ConsoleOut.SetOut();
                Console.WriteLine("Testing VS 2019 console....");
            }

            var sln = @"d:\Beta\OmniX\SourceBrowser\SourceBrowser.sln";
            var baseDir = @"d:\Beta\OmniX\SourceBrowser";

            // E:\Beta\mono64\SourceBrowser\src\HtmlGenerator.Tests\bin\Debug\net461\
            ProgramLoad.isDebug = false;
            ProgramLoad.BasePath = typeof(ProgramLoad).Assembly.Location;

            if (!Debugger.IsAttached) {
                ProgramLoad.isDebug = false;
                ProgramLoad.Main(new string[] { ".exe", sln, $"/out:{baseDir}\\srcWeb" });
            } else
                Program.Run(new string[] { sln, "/debug", $"/out:{baseDir}\\srcWeb" });
        }
    }
}

namespace System
{
    // using Console = global::System.Console;
    // https://developercommunity.visualstudio.com/content/problem/12166/console-output-is-gone-in-vs2017-works-fine-when-d.html

    public class TraceListener : TextWriterTraceListener
    {
        public TraceListener(TextWriter stream, string name) : base(stream, name) { }

        public override void Close() { }
        public override void Flush() { }
        public override void Write(string message)
        {
            Debugger.Log(0, "", message ?? "");
        }

        public override void WriteLine(string message)
        {
            Debugger.Log(0, "", Environment.NewLine);
            Write(message ?? "");
        }
    }

    public class ConsoleOut
    {
        public static int bufferWidth = 100, bufferHeight = 500;
        public static IntPtr hConsole;

        static Stream outStream;
        static Stream errStream;
        public static Exception LastError { get; set; }

        public static void SetOut()
        {
            TraceOut.Listener.Register();

            var check = Debug.Listeners[Listener.DebugName ?? "TraceOut.Listener"];
            if (check != null || System.Console.IsOutputRedirected) {
                Console.Write(": Console.SetOut set to TraceListener\n");
            } else {
                Debugger.Log(0, "", ": Console.SetOut set failed\n");
            }
        }

        public static void Load()
        {
            if (hConsole != IntPtr.Zero) {
                return;
            }

            hConsole = IntPtr.Zero;
            try {
                AllocConsole();
                outStream = Console.OpenStandardOutput() as Stream;
                errStream = Console.OpenStandardError() as Stream;
                Encoding encoding = System.Text.Encoding.GetEncoding(MY_CODE_PAGE);

                Show();
            }
            catch (Exception ex) {
                LastError = ex;
                Console.WriteLine($"ConsoleOut fails: {ex.InnerException ?? ex}");
            }
        }

        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const int MY_CODE_PAGE = 437;

        #region Unsafe

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern IntPtr CreateFile(
              string lpFileName,
              uint dwDesiredAccess,
              uint dwShareMode,
              uint lpSecurityAttributes,
              uint dwCreationDisposition,
              uint dwFlagsAndAttributes,
              uint hTemplateFile
              );

        [DllImport("kernel32.dll",
        EntryPoint = "GetStdHandle",
        SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern int AllocConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();
        public struct COORD
        {
            public short X;
            public short Y;
        };
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleScreenBufferSize(
          IntPtr hConsoleOutput,
          COORD size
        );
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(System.IntPtr hWnd, int cmdShow);
        [DllImport("user32.dll")]
        public static extern bool MoveWindow(
                IntPtr hWnd,
                int X,
                int Y,
                int nWidth,
                int nHeight,
                bool bRepaint
            );
        
        [Flags]
        enum DesiredAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000
        }
        private enum StdHandle : int
        {
            Input = -10,
            Output = -11,
            Error = -12
        }
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        #endregion

        public static void Hide()
        {
            FreeConsole();
        }
        // GENERIC_WRITE, FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, 0);

            /*
        static void UnredirectConsole(out IntPtr stdOut, out IntPtr stdIn) // , out IntPtr stdErr)
        {
            // SetStdHandle(StdHandle.Output, stdOut = GetConsoleStandardOutput());
            // SetStdHandle(StdHandle.Input, stdIn = GetConsoleStandardInput());
            // SetStdHandle(StdHandle.Error, stdErr = GetConsoleStandardError());
        }

        public static IntPtr GetConsoleStandardInput()
        {
            var handle = CreateFile
                ("CONIN$"
                , DesiredAccess.GenericRead | DesiredAccess.GenericWrite
                , FileShare.ReadWrite
                , IntPtr.Zero
                , FileMode.Open
                , FileAttributes.Normal
                , IntPtr.Zero
                );
            if (handle == InvalidHandleValue)
                return InvalidHandleValue;
            return handle;
        }
        public static IntPtr GetConsoleStandardOutput()
        {
            var handle = CreateFile
                ("CONOUT$"
                , (uint)DesiredAccess.GenericWrite | DesiredAccess.GenericWrite
                , (uint)FileShare.ReadWrite
                , (uint)IntPtr.Zero
                , (uint)FileMode.Open
                , (uint)FileAttributes.Normal
                , (uint)IntPtr.Zero
                );
            if (handle == InvalidHandleValue)
                return InvalidHandleValue;
            return handle;
        }
        */

        const uint GENERIC_WRITE = (uint)DesiredAccess.GenericWrite; // 
        const uint FILE_SHARE_WRITE = (uint)FileShare.ReadWrite;
        const uint OPEN_EXISTING = (uint)FileMode.Open;

        public static void Show(
              int _bufferWidth = -1, bool breakRedirection = true, int bufferHeight = 1600, int screenNum = -1 /*-1 = Any but primary*/)
        {
        
            try {
                // AllocConsole();

                // Console.OpenStandardOutput eventually calls into GetStdHandle. 
                // As per MSDN documentation of GetStdHandle: http://msdn.microsoft.com/en-us/library/windows/desktop/ms683231(v=vs.85).aspx will return the redirected handle and not the allocated console:
                // "The standard handles of a process may be redirected by a call to  SetStdHandle, in which case  GetStdHandle returns the redirected handle. If the standard handles have been redirected, you can specify the CONIN$ value in a call to the CreateFile function to get a handle to a console's input buffer. Similarly, you can specify the CONOUT$ value to get a handle to a console's active screen buffer."
                // Get the handle to CONOUT$.    
                IntPtr stdHandle = CreateFile("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, 0);

                SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
                var fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                Encoding encoding = System.Text.Encoding.GetEncoding(1250); // MY_CODE_PAGE);
                StreamWriter standardOutput = new StreamWriter(outStream, encoding);

                standardOutput.AutoFlush = true;

                Console.SetOut(standardOutput);

                hConsole = GetConsoleWindow();
                MoveWindow(hConsole, 0, 100, 1100, 500, true);
        
                Console.Write("kjsadfakjsfdsadf"); // Outputs to the Console window.

                bufferWidth = _bufferWidth;
                Console.SetBufferSize(bufferWidth, bufferHeight);


                if (bufferWidth == -1) {
                    //System.Windows.Forms.Screen screen = null;
                    //if (screen == null)
                        bufferWidth = 180;
                    //else {
                    //    bufferWidth = screen.WorkingArea.Width / 10;
                    //    if (bufferWidth > 15)
                    //        bufferWidth -= 5;
                    //    else
                    //        bufferWidth = 10;
                    //}
                }

                //if (screen != null)
                //{
                //    var workingArea = screen.WorkingArea;
                    //IntPtr hConsole = GetConsoleWindow();
                //    MoveWindow(hConsole, workingArea.Left, workingArea.Top, workingArea.Width, workingArea.Height, true);
                //}
                
            }
            catch (Exception e) // Could be redirected, but where to?
            {
                LastError = e;
            }

        }


    }
}
using System;
using System.IO;
using System.Text;

namespace Microsoft.SourceBrowser.Common
{
    public class Log
    {
        private static readonly object consoleLock = new object();
        public const string ErrorLogFile = "Errors.txt";
        public const string MessageLogFile = "Messages.txt";
        private const string SeparatorBar = "===============================================";

        private static string errorLogFilePath = Path.GetFullPath(ErrorLogFile);
        private static string messageLogFilePath = Path.GetFullPath(MessageLogFile);

        public static void Exception(Exception e, string message, bool isSevere = true)
        {
            var text = message + Environment.NewLine + e.ToString();
            Exception(text, isSevere);
        }

        public static void Exception(string message, bool isSevere = true)
        {
            Write(message, isSevere ? ConsoleColor.Red : ConsoleColor.Yellow);
            WriteToFile(message, ErrorLogFilePath);
        }

        public static void Message(string message)
        {
            Write(message, ConsoleColor.Blue);
            WriteToFile(message, MessageLogFilePath);
        }

        private static void WriteToFile(string message, string filePath)
        {
            lock (consoleLock)
            {
                try
                {
                    File.AppendAllText(filePath, SeparatorBar + Environment.NewLine + message + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Write($"Failed to write to ${filePath}: ${ex}.", ConsoleColor.Red);
                }
            }
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(DateTime.Now.ToString("HH:mm:ss") + " ");
                Console.ForegroundColor = color;
                WriteLine(message);
                if (color != ConsoleColor.Gray)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        public static Action<string> WriteWrap { get; set; }

        public static void WriteLine(string message)
        {
            if (WriteWrap != null)
            {
                WriteWrap(message);
                if (Level == 0)
                    Console.WriteLine(message);
            }
            else
                Console.WriteLine(message);
        }

        private static int Level = 0;
        public static void Output(string message)
        {
            Level++;
            if (Level == 1)
                Console.WriteLine(message);

            if (Level > 0)
                Level--;
        }

        public static string ErrorLogFilePath
        {
            get { return errorLogFilePath; }
            set { errorLogFilePath = value.MustBeAbsolute(); }
        }

        public static string MessageLogFilePath
        {
            get { return messageLogFilePath; }
            set { messageLogFilePath = value.MustBeAbsolute(); }
        }
    }
}

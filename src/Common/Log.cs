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

            if (message.Contains(".xproj"))
            {
            }
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
                    Console.Write("Failed to write to ", ConsoleColor.Red);
                    Console.Write(filePath, ConsoleColor.Red);
                    Console.Write(" ", ConsoleColor.Red);
                    Console.Write(ex.ToString(), ConsoleColor.Red);
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
                Console.WriteLine(message);
                if (color != ConsoleColor.Gray)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
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

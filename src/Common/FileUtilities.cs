using System;
using System.IO;

namespace Microsoft.SourceBrowser.Common
{
    public class FileUtilities
    {
        public static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                throw new ArgumentException("Source directory doesn't exist:" + sourceDirectory);
            }

            var sourceDirectoryTrim = sourceDirectory; // .TrimSlash();
            if (sourceDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                sourceDirectoryTrim = sourceDirectory.Substring(sourceDirectory.Length - 1);

            if (string.IsNullOrEmpty(destinationDirectory))
            {
                throw new ArgumentNullException("destinationDirectory");
            }

            var destinationDirectoryTrim = destinationDirectory; //.TrimSlash();
            if (destinationDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                destinationDirectory = destinationDirectory.Substring(destinationDirectory.Length - 1);

            var files = Directory.GetFiles(sourceDirectoryTrim, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relative = file.Substring(sourceDirectoryTrim.Length + 1);
                var destination = Path.Combine(destinationDirectoryTrim, relative);
                CopyFile(file, destination);
            }
        }

        public static void CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
        {
            if (!File.Exists(sourceFilePath))
            {
                return;
            }

            if (!overwrite && File.Exists(destinationFilePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(destinationFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourceFilePath, destinationFilePath, overwrite);
            File.SetAttributes(destinationFilePath, File.GetAttributes(destinationFilePath) & ~FileAttributes.ReadOnly);
        }
    }
}

using System;
using System.IO;

namespace Hacks.HtmlGenerator.Utilities
{
    public class WorkSpaceXProj
    {
        public static string ParseXProj(string xprojFile)
        {
            // TODO .xproj
            const string msg = "Cannot open project ";
            var idx = xprojFile.IndexOf(msg);
            if (idx >= 0)
            {
                idx += msg.Length;
                if (xprojFile[idx] == '\'') idx++;
                xprojFile = xprojFile.Substring(idx);

                var idx2 = xprojFile.IndexOf('\'');
                if (idx2 > 0)
                    xprojFile = xprojFile.Substring(0, idx2);
            }

            var file = Path.GetFullPath(xprojFile);
            var csproj = Path.ChangeExtension(file, "csproj");
            if (!File.Exists(csproj))
            {
                File.Copy(file, csproj);  // ?????

                return csproj;
            }

            return csproj;
        }
    }
}

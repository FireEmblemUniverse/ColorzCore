using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.IO
{
    static class IOUtility
    {

        /* Modified from Nintenlord's Core's IO.IOHelpers */
        public static Maybe<string> FindFile(string currentFile, string newFile)
        {
            // Reordered so that relative directory is searched first.

            if (!string.IsNullOrEmpty(currentFile))
            {
                // relative to current file directory
                string path = Path.Combine(Path.GetDirectoryName(currentFile), newFile);
                if (File.Exists(path))
                    return new Just<string>(path);
            }

            if (File.Exists(newFile))
            {
                // relative to working directory
                return new Just<string>(newFile);
            }
            else
            {
                // relative to EA distribution directory
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newFile);
                if (File.Exists(path))
                    return new Just<string>(path);
            }
            
            return new Nothing<string>();
        }

        public static string UnescapeString(string param)
        {
            StringBuilder sb = new StringBuilder(param);
            return sb.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\\\", "\\").Replace("\\r", "\r").ToString();
        }
        public static string UnescapePath(string param)
        {
            StringBuilder sb = new StringBuilder(param);
            return sb.Replace("\\ ", " ").Replace("\\\\", "\\").ToString();
        }
        public static string GetToolPath(string toolName)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32Windows: // Who knows, maybe someone runs EA on win 95
                case PlatformID.Win32NT:
                    toolName = toolName + ".exe";
                    break;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", toolName);
        }
    }
}

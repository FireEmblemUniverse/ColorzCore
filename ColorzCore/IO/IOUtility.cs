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
            //Reordered so that relative directory is searched first. 
            if (!string.IsNullOrEmpty(currentFile))
            {
                string path = Path.Combine(Path.GetDirectoryName(currentFile), newFile);
                if (File.Exists(path))
                    return new Just<string>(path);
            }
            if (File.Exists(newFile))
                return new Just<string>(newFile);
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
    }
}

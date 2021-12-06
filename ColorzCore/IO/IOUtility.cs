using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.IO
{
    static class IOUtility
    {
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

        public static string GetToolFileName(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return name + ".exe";
            } else {
                return name;
            }
        }
    }
}

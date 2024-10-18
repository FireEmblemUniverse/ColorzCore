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
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{name}.exe" : name;
        }

        // HACK: this is to make Log not print the entire path every time
        public static string GetPortableBasePathForPrefix(string name)
        {
            string? result = Path.GetDirectoryName(name);

            if (string.IsNullOrEmpty(result))
            {
                return "";
            }
            else
            {
                return (result + '/').Replace('\\', '/');
            }
        }

        private static readonly char[] pathSeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private static readonly char[] invalidFileCharacters = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Convert a user-given path expression to a 'portable' one. That is, it would work on other systems.
        /// This is used for warning emission from '#include' and '#incbin' directives that refer to non-portable paths
        /// </summary>
        /// <param name="fullPath">The full real path corresponding to the given expression</param>
        /// <param name="pathExpression">The user given expression</param>
        /// <returns>The portable version of the expression, possibly the same as the input expression</returns>
        public static string GetPortablePathExpression(string fullPath, string pathExpression)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // this method is only meaningful on Windows, which has case-insensitive paths and '\' path separators.
                // I believe that on other systems (Linux and friends for sure, probably macOS also?), valid paths are necessarily portable.
                return pathExpression;
            }

            if (Path.IsPathRooted(pathExpression))
            {
                // HACK: rooted paths (like absolute paths) don't really make sense to make portable anyway
                // those are most likely generated paths from tools so this doesn't really matter

                return pathExpression;
            }

            IList<string> inputComponents = pathExpression.Split(pathSeparators).ToList();
            IList<string> outputComponents = new List<string>(); // will be in reverse

            int upwind = 0;
            
            foreach (string component in inputComponents.Reverse())
            {
                if (component.IndexOfAny(invalidFileCharacters) != -1)
                {
                    // fallback just in case
                    outputComponents.Add(component);
                }
                if (component == "..")
                {
                    outputComponents.Add(component);
                    upwind++;
                }
                else if (component == ".")
                {
                    outputComponents.Add(component);
                }
                else if (upwind > 0)
                {
                    // this was a "DirName/..", get the real name of this DirName
                    string? correctComponent = Path.GetFileName(Directory.GetFileSystemEntries(fullPath, component).FirstOrDefault());
                    outputComponents.Add(correctComponent ?? component);

                    upwind--;
                }
                else
                {
                    string? reducedPath = Path.GetDirectoryName(fullPath);
                    fullPath = string.IsNullOrEmpty(reducedPath) ? "." : reducedPath;

                    string? correctComponent = Path.GetFileName(Directory.GetFileSystemEntries(fullPath, component).FirstOrDefault());
                    outputComponents.Add(correctComponent ?? component);
                }
            }

            return string.Join("/", outputComponents.Reverse());
        }
    }
}

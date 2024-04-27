using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore
{
    public static class EAOptions
    {
        [Flags]
        public enum Warnings
        {
            None = 0,
            NonPortablePath = 1,
        }

        [Flags]
        public enum Extensions
        {
            None = 0,
            ReadDataMacros = 1,
        }

        public static bool WarningsAreErrors { get; set; }
        public static bool QuietWarnings { get; set; }
        public static bool QuietMessages { get; set; }
        public static bool MonochromeLog { get; set; }
        public static bool BenchmarkBuildTimes { get; set; }
        public static bool ProduceNocashSym { get; set; }
        public static bool TranslateBackslashesInPaths { get; set; } = true;

        public static int BaseAddress { get; set; } = 0x8000000;
        public static int MaximumBinarySize { get; set; } = 0x2000000;

        public static List<string> IncludePaths { get; } = new List<string>();
        public static List<string> ToolsPaths { get; } = new List<string>();
        public static List<Tuple<string, string>> PreDefintions { get; } = new List<Tuple<string, string>>();

        public static Warnings EnabledWarnings { get; set; } = Warnings.NonPortablePath;
        public static Extensions EnabledExtensions { get; set; } = Extensions.ReadDataMacros;

        public static bool IsWarningEnabled(Warnings warning) => (EnabledWarnings & warning) != 0;
        public static bool IsExtensionEnabled(Extensions extension) => (EnabledExtensions & extension) != 0;
    }
}

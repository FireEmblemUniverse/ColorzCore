using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore
{
    public static class EAOptions
    {
        [Flags]
        public enum Warnings : long
        {
            None = 0,

            // warn on non-portable include paths on Windows
            NonPortablePath = 1,

            // warn on #define on an existing definition
            ReDefine = 2,

            // warn on write before ORG
            UninitializedOffset = 4,

            // warn on unintuitive macro expansions (#define A 1 + 2 ... BYTE A * 2 )
            UnintuitiveExpressionMacros = 8,

            // warn on expansion of unguarded expression within macro ()
            UnguardedExpressionMacros = 16,

            // warn on macro expanded into "PUSH ; ORG value ; name : ; POP"
            SetSymbolMacros = 32,

            Extra = UnguardedExpressionMacros,
            All = long.MaxValue & ~Extra,
        }

        [Flags]
        public enum Extensions : long
        {
            None = 0,

            // enable ReadByteAt and friends
            ReadDataMacros = 1,

            // enable incext and inctext/inctevent
            IncludeTools = 2,

            // enable AddToPool and #pool
            AddToPool = 4,

            All = long.MaxValue,
            Default = IncludeTools | AddToPool,
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
        public static List<(string, string)> PreDefintions { get; } = new List<(string, string)>();

        public static Warnings EnabledWarnings { get; set; } = Warnings.All;
        public static Extensions EnabledExtensions { get; set; } = Extensions.Default;

        public static bool IsWarningEnabled(Warnings warning) => EnabledWarnings.HasFlag(warning);
        public static bool IsExtensionEnabled(Extensions extension) => EnabledExtensions.HasFlag(extension);
    }
}

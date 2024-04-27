using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore
{
    class EAOptions
    {
        // TODO: clean up

        public bool werr;
        public bool nowarn, nomess;
        public bool buildTimes;

        // TODO: better warning flags

        public bool warnPortablePath;

        public bool noColoredLog;

        public bool nocashSym;
        public bool readDataMacros;
        public bool translateBackslashesInPath;

        public List<string> includePaths = new List<string>();
        public List<string> toolsPaths = new List<string>();
        public List<Tuple<string, string>> defs = new List<Tuple<string, string>>();
        public static EAOptions Instance { get; } = new EAOptions();

        public int romBaseAddress;
        public int maximumRomSize;

        private EAOptions()
        {
            werr = false;
            nowarn = false;
            nomess = false;

            warnPortablePath = true;

            // this allows some non-portable paths to be made portable automatically
            // also prevents the portable path warning from being emitted for some genereated files
            translateBackslashesInPath = true;

            noColoredLog = false;
            nocashSym = false;
            readDataMacros = true;
            buildTimes = false;
            romBaseAddress = 0x8000000;
            maximumRomSize = 0x2000000;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore
{
    class EAOptions
    {
        public bool werr;
        public bool nowarn, nomess;
        public bool buildTimes;

        public bool noColoredLog;

        public bool nocashSym;
        public bool readDataMacros;

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
            noColoredLog = false;
            nocashSym = false;
            readDataMacros = true;
            buildTimes = false;
            romBaseAddress = 0x8000000;
            maximumRomSize = 0x2000000;
        }
    }
}

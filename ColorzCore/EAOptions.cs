using System;
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

        public bool nocashSym;

        public EAOptions()
        {
            werr = false;
            nowarn = false;
            nomess = false;
            nocashSym = false;
        }
    }
}

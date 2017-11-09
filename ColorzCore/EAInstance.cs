using ColorzCore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore
{
    class EAInstance
    {
        EAProgram AST { get; set; }
        Dictionary<string, IASTNode> Definitions { get; set; }
        Dictionary<string, int> Labels { get; set; }
        IList<string> Messages { get; }
        IList<string> Warnings { get; }
        IList<string> Errors { get; }
    }
}

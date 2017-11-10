using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    class EAProgram
    {

        public string File { get; }
        public int Line { get; }
        public int Column { get; }

        public Token StartToken => throw new NotImplementedException();
    }
}

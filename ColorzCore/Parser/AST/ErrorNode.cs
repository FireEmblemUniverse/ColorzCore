using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class ErrorNode : IASTNode
    {
        public ErrorNode(string file, int lineNumber, int columnNumber, string message = "")
        {
            File = file;
            Line = lineNumber;
            Column = columnNumber;
            Message = message;
        }

        public ASTNodeType Type => ASTNodeType.ERROR;

        public string File { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public string Message { get; private set; }
    }
}

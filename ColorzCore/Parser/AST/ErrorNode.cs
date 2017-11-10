using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Lexer;

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

        public string File { get; }
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }

        public Token startToken => throw new NotImplementedException();
    }
}

using System;
using ColorzCore.Parser.AST;

namespace ColorzCore.Parser.AST
{
    internal class EOSNode : IASTNode
    {
        public EOSNode(string file, int lineNumber, int columnNumber)
        {
            File = file;
            Line = lineNumber;
            Column = columnNumber;
        }

        public ASTNodeType Type => ASTNodeType.EOS;

        public string File { get; private set; }

        public int Line { get; private set; }

        public int Column { get; private set; }
    }
}
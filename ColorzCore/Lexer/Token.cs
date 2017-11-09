using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Lexer
{
    public class Token
    {
        public TokenType Type { get; private set; }
        public int LineNumber { get; private set; }
        public int ColumnNumber { get; private set; }
        public string Content { get; private set; }

        public Token(TokenType type, int lineNum, int colNum, string original = "")
        {
            Type = type;
            LineNumber = lineNum;
            ColumnNumber = colNum + 1;
            Content = original;
        }

        public override string ToString()
        {
            return String.Format("Line {0}, Column {1}, {2}: {3}", LineNumber, ColumnNumber, Type, Content);
        }
    }
}

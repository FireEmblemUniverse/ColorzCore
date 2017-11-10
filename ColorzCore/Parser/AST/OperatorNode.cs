using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    delegate int BinaryIntOp(int, int);
    
    class OperatorNode : IAtomNode
    {
        public static readonly Dictionary<TokenType, BinaryIntOp> Operators = new Dictionary<TokenType, BinaryIntOp> {
                { TokenType.MUL_OP , (x, y) => x*y },
                { TokenType.DIV_OP , (x, y) => x/y },
                { TokenType.ADD_OP , (x, y) => x+y },
                { TokenType.SUB_OP , (x, y) => x-y },
                { TokenType.LSHIFT_OP , (x, y) => x<<y },
                { TokenType.RSHIFT_OP , (x, y) => (int)(((uint)x)>>y) },
                { TokenType.SIGNED_RSHIFT_OP , (x, y) => x>>y },
                { TokenType.AND_OP , (x, y) => x&y },
                { TokenType.XOR_OP , (x, y) => x^y },
                { TokenType.OR_OP , (x, y) => x|y }    
        };
        
        private IAtomNode left, right;
        private Token op;
		public int Precedence { get; }
		
		public OperatorNode(IAtomNode l, Token operator, IAtomNode r, int prec)
		{
            left = l;
            right = r;
            op = operator;
            Precedence = prec;
		}
		
		public int Evaluate()
		{
            Operators[op.Type](l.Evaluate(), r.Evaluate());
		}
    }
}

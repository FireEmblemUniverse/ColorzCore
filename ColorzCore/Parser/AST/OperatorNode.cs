using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    delegate int BinaryIntOp(int a, int b);

    class OperatorNode : AtomNodeKernel
    {
        public static readonly Dictionary<TokenType, BinaryIntOp> Operators = new Dictionary<TokenType, BinaryIntOp> {
                { TokenType.MUL_OP , (x, y) => x*y },
                { TokenType.DIV_OP , (x, y) => x/y },
                { TokenType.MOD_OP , (x, y) => x%y },
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
        public override int Precedence { get; }

        public override Location MyLocation { get { return op.Location; } }

        public OperatorNode(IAtomNode l, Token op, IAtomNode r, int prec)
        {
            left = l;
            right = r;
            this.op = op;
            Precedence = prec;
        }

        public override string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder(left.PrettyPrint());
            switch (op.Type)
            {
                case TokenType.MUL_OP:
                    sb.Append("*");
                    break;
                case TokenType.DIV_OP:
                    sb.Append("/");
                    break;
                case TokenType.ADD_OP:
                    sb.Append("+");
                    break;
                case TokenType.SUB_OP:
                    sb.Append("-");
                    break;
                case TokenType.LSHIFT_OP:
                    sb.Append("<<");
                    break;
                case TokenType.RSHIFT_OP:
                    sb.Append(">>");
                    break;
                case TokenType.SIGNED_RSHIFT_OP:
                    sb.Append(">>>");
                    break;
                case TokenType.AND_OP:
                    sb.Append("&");
                    break;
                case TokenType.XOR_OP:
                    sb.Append("^");
                    break;
                case TokenType.OR_OP:
                    sb.Append("|");
                    break;
                default:
                    break;
            }
            sb.Append(right.PrettyPrint());
            return sb.ToString();
        }
        public override IEnumerable<Token> ToTokens()
        {
            foreach (Token t in left.ToTokens())
            {
                yield return t;
            }
            yield return op;
            foreach (Token t in right.ToTokens())
            {
                yield return t;
            }
        }

        public override int? TryEvaluate(TAction<Exception> handler)
        {
            int? l = left.TryEvaluate(handler);
            l.IfJust(i => left = new NumberNode(left.MyLocation, i));
            int? r = right.TryEvaluate(handler);
            r.IfJust(i => right = new NumberNode(right.MyLocation, i));

            if (l is int li && r is int ri)
                return Operators[op.Type](li, ri);

            return null;
        }
    }
}

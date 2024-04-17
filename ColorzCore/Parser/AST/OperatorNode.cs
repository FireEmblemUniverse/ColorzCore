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
            { TokenType.MUL_OP , (lhs, rhs) => lhs * rhs },
            { TokenType.DIV_OP , (lhs, rhs) => lhs / rhs },
            { TokenType.MOD_OP , (lhs, rhs) => lhs % rhs },
            { TokenType.ADD_OP , (lhs, rhs) => lhs + rhs },
            { TokenType.SUB_OP , (lhs, rhs) => lhs - rhs },
            { TokenType.LSHIFT_OP , (lhs, rhs) => lhs << rhs },
            { TokenType.RSHIFT_OP , (lhs, rhs) => (int)(((uint)lhs) >> rhs) },
            { TokenType.SIGNED_RSHIFT_OP , (lhs, rhs) => lhs >> rhs },
            { TokenType.AND_OP , (lhs, rhs) => lhs & rhs },
            { TokenType.XOR_OP , (lhs, rhs) => lhs ^ rhs },
            { TokenType.OR_OP , (lhs, rhs) => lhs | rhs },
            { TokenType.LOGAND_OP, (lhs, rhs) => lhs != 0 ? rhs : 0 },
            { TokenType.LOGOR_OP, (lhs, rhs) => lhs != 0 ? lhs : rhs },
            { TokenType.COMPARE_EQ, (lhs, rhs) => lhs == rhs ? 1 : 0 },
            { TokenType.COMPARE_NE, (lhs, rhs) => lhs != rhs ? 1 : 0 },
            { TokenType.COMPARE_LT, (lhs, rhs) => lhs < rhs ? 1 : 0 },
            { TokenType.COMPARE_LE, (lhs, rhs) => lhs <= rhs ? 1 : 0 },
            { TokenType.COMPARE_GE, (lhs, rhs) => lhs >= rhs ? 1 : 0 },
            { TokenType.COMPARE_GT, (lhs, rhs) => lhs > rhs ? 1 : 0 },
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
            static string GetOperatorString(TokenType tokenType)
            {
                return tokenType switch
                {
                    TokenType.MUL_OP => "*",
                    TokenType.DIV_OP => "/",
                    TokenType.MOD_OP => "%",
                    TokenType.ADD_OP => "+",
                    TokenType.SUB_OP => "-",
                    TokenType.LSHIFT_OP => "<<",
                    TokenType.RSHIFT_OP => ">>",
                    TokenType.SIGNED_RSHIFT_OP => ">>>",
                    TokenType.AND_OP => "&",
                    TokenType.XOR_OP => "^",
                    TokenType.OR_OP => "|",
                    TokenType.LOGAND_OP => "&&",
                    TokenType.LOGOR_OP => "||",
                    TokenType.COMPARE_EQ => "==",
                    TokenType.COMPARE_NE => "!=",
                    TokenType.COMPARE_LT => "<",
                    TokenType.COMPARE_LE => "<=",
                    TokenType.COMPARE_GT => ">",
                    TokenType.COMPARE_GE => ">=",
                    _ => "<bad operator>"
                };
            }

            return $"({left.PrettyPrint()} {GetOperatorString(op.Type)} {right.PrettyPrint()})";
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

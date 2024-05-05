using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    public class UnaryOperatorNode : AtomNodeKernel
    {
        public IAtomNode Inner { get; private set; }

        public Token OperatorToken { get; }

        public UnaryOperatorNode(Token token, IAtomNode inside)
        {
            OperatorToken = token;
            Inner = inside;
        }

        public override int Precedence => int.MaxValue;
        public override Location MyLocation => OperatorToken.Location;

        public string OperatorString => OperatorToken.Type switch
        {
            TokenType.SUB_OP => "-",
            TokenType.NOT_OP => "~",
            TokenType.LOGNOT_OP => "!",
            _ => "<bad operator>",
        };

        public override string PrettyPrint()
        {
            return OperatorString + Inner.PrettyPrint();
        }

        public override int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            int? inner = Inner.TryEvaluate(handler, evaluationPhase);

            if (inner != null)
            {
                // int? is magic and all of these operations conveniently propagate nulls

                return OperatorToken.Type switch
                {
                    TokenType.SUB_OP => -inner,
                    TokenType.NOT_OP => ~inner,
                    TokenType.LOGNOT_OP => inner != 0 ? 0 : 1,
                    _ => null,
                };
            }
            else
            {
                return null;
            }
        }
    }
}

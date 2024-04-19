using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    class UnaryOperatorNode : AtomNodeKernel
    {
        private readonly IAtomNode interior;

        public Token OperatorToken { get; }

        public UnaryOperatorNode(Token token, IAtomNode inside)
        {
            OperatorToken = token;
            interior = inside;
        }

        public override int Precedence => 11;
        public override Location MyLocation => OperatorToken.Location;

        public override string PrettyPrint()
        {
            string operatorString = OperatorToken.Type switch
            {
                TokenType.SUB_OP => "-",
                TokenType.NOT_OP => "~",
                TokenType.LOGNOT_OP => "!",
                _ => "<bad operator>",
            };

            return operatorString + interior.PrettyPrint();
        }

        public override IEnumerable<Token> ToTokens()
        {
            yield return OperatorToken;
            foreach (Token t in interior.ToTokens())
                yield return t;
        }

        public override int? TryEvaluate(TAction<Exception> handler, EvaluationPhase evaluationPhase)
        {
            int? inner = interior.TryEvaluate(handler, evaluationPhase);

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

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
        private readonly Token myToken;
        private readonly IAtomNode interior;

        public UnaryOperatorNode(Token token, IAtomNode inside)
        {
            myToken = token;
            interior = inside;
        }

        public override int Precedence => 11;
        public override Location MyLocation => myToken.Location;

        public override string PrettyPrint()
        {
            string operatorString = myToken.Type switch
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
            yield return myToken;
            foreach (Token t in interior.ToTokens())
                yield return t;
        }

        public override int? TryEvaluate(TAction<Exception> handler)
        {
            int? inner = interior.TryEvaluate(handler);

            if (inner != null)
            {
                // int? is magic and all of these operations conveniently propagate nulls

                return myToken.Type switch
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

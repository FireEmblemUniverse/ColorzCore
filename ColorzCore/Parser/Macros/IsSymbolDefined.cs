using System;
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.Macros
{
    public class IsSymbolDefined : BuiltInMacro
    {

        public override IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters, ImmutableStack<Closure> scopes)
        {
            if (parameters[0].Count != 1)
            {
                // TODO: err somehow
                yield return MakeFalseToken(head.Location);
            }
            else
            {
                Token token = parameters[0][0];

                if ((token.Type == TokenType.IDENTIFIER) && IsReallyDefined(scopes, token.Content))
                {
                    yield return MakeTrueToken(head.Location);
                }
                else
                {
                    yield return MakeFalseToken(head.Location);
                }
            }
        }

        public override bool ValidNumParams(int num)
        {
            return num == 1;
        }

        protected static bool IsReallyDefined(ImmutableStack<Closure> scopes, string name)
        {
            for (ImmutableStack<Closure> it = scopes; it != ImmutableStack<Closure>.Nil; it = it.Tail)
            {
                if (it.Head.HasLocalSymbol(name))
                {
                    return true;
                }
            }

            return false;
        }

        protected static Token MakeTrueToken(Location location)
        {
            return new Token(TokenType.NUMBER, location, "1");
        }

        protected static Token MakeFalseToken(Location location)
        {
            return new Token(TokenType.NUMBER, location, "0");
        }
    }
}

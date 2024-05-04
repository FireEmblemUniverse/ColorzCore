using System;
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Preprocessor.Macros
{
    public class IsSymbolDefined : BuiltInMacro
    {
        public override IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters)
        {
            if (parameters[0].Count != 1)
            {
                // TODO: err somehow
                yield return MakeFalseToken(head.Location);
            }
            else
            {
                MacroLocation macroLocation = new MacroLocation(head.Content, head.Location);
                Location location = head.Location.MacroClone(macroLocation);

                Token identifierToken = parameters[0][0];

                // This used to be more involved, but now it is just a dummy
                // ((id || 1) ?? 0)

                yield return new Token(TokenType.OPEN_PAREN, location, "(");
                yield return new Token(TokenType.OPEN_PAREN, location, "(");
                yield return identifierToken.MacroClone(macroLocation);
                yield return new Token(TokenType.LOGOR_OP, location, "||");
                yield return new Token(TokenType.NUMBER, location, "1");
                yield return new Token(TokenType.CLOSE_PAREN, location, ")");
                yield return new Token(TokenType.UNDEFINED_COALESCE_OP, location, "??");
                yield return new Token(TokenType.NUMBER, location, "0");
                yield return new Token(TokenType.CLOSE_PAREN, location, ")");
            }
        }

        public override bool ValidNumParams(int num)
        {
            return num == 1;
        }

        protected static Token MakeFalseToken(Location location)
        {
            return new Token(TokenType.NUMBER, location, "0");
        }
    }
}

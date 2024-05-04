using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColorzCore.Preprocessor.Macros
{
    class StringMacro : BuiltInMacro
    {
        public override IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters)
        {
            MacroLocation macroLocation = new MacroLocation(head.Content, head.Location);

            Token token = parameters[0][0];
            Location location = token.Location.MacroClone(macroLocation);

            yield return new Token(TokenType.IDENTIFIER, location, "STRING");
            yield return new Token(TokenType.STRING, location, token.Content);
            yield return new Token(TokenType.STRING, location, "UTF-8");
            yield return new Token(TokenType.SEMICOLON, location, ";");
        }

        public override bool ValidNumParams(int num)
        {
            return num == 1;
        }
    }
}

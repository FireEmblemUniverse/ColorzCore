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

            if (EAOptions.IsWarningEnabled(EAOptions.Warnings.LegacyFeatures))
            {
                yield return new Token(TokenType.IDENTIFIER, location, "WARNING");
                yield return new Token(TokenType.STRING, location, "Consider using the STRING statement rather than the legacy String macro.");
                yield return new Token(TokenType.SEMICOLON, location, ";");
            }

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

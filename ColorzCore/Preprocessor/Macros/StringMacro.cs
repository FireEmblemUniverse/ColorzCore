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
            yield return new Token(TokenType.IDENTIFIER, head.Location, "UTF8");
            yield return new Token(TokenType.STRING, parameters[0][0].Location, parameters[0][0].Content);
        }

        public override bool ValidNumParams(int num)
        {
            return num == 1;
        }
    }
}

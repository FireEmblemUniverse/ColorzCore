using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.Macros
{
    class String : BuiltInMacro
    {
        private static String instance = new String();

        public static String Instance { get { return instance; } }

        private String() { }

        public override IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters)
        {
            yield return new Token(TokenType.IDENTIFIER, head.Location, "BYTE");
            foreach (byte num in Encoding.ASCII.GetBytes(parameters[0][0].Content.ToCharArray())) //TODO: Errors if not adherent?
                yield return new Token(TokenType.NUMBER, head.Location, num.ToString());
        }

        public override bool ValidNumParams(int num)
        {
            return num == 1;
        }
    }
}

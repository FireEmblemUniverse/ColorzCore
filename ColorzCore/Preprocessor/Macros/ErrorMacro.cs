using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Preprocessor.Macros
{
    class ErrorMacro : BuiltInMacro
    {
        public delegate bool ValidateNumParamsIndirect(int num);

        private readonly EAParser parser;
        private readonly string message;
        private readonly ValidateNumParamsIndirect validateNumParamsIndirect;

        public ErrorMacro(EAParser parser, string message, ValidateNumParamsIndirect validateNumParamsIndirect)
        {
            this.parser = parser;
            this.message = message;
            this.validateNumParamsIndirect = validateNumParamsIndirect;
        }

        public override IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters)
        {
            parser.Logger.Error(head.Location, message);
            yield return new Token(TokenType.NUMBER, head.Location, "0");
        }

        public override bool ValidNumParams(int num) => validateNumParamsIndirect(num);
    }
}

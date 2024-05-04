using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;

namespace ColorzCore.Preprocessor.Macros
{
    public abstract class BuiltInMacro : IMacro
    {
        public abstract bool ValidNumParams(int num);
        public abstract IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters);
    }
}

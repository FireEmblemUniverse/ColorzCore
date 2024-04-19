using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.Macros
{
    public abstract class BuiltInMacro : IMacro
    {
        public abstract bool ValidNumParams(int num);
        public abstract IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters, ImmutableStack<Closure> scopes);
    }
}

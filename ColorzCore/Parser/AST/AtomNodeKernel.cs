using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    public abstract class AtomNodeKernel : IAtomNode
    {
        public abstract int Precedence { get; }

        public abstract int Evaluate();

        public ParamType Type { get { return ParamType.ATOM; } }

        public override string ToString()
        {
            return Evaluate().ToString();
        }

        public virtual Maybe<string> GetIdentifier()
        {
            return new Nothing<string>();
        }

        public abstract string PrettyPrint();
        public abstract IEnumerable<Token> ToTokens();
        public abstract Location MyLocation { get; }
    }
}

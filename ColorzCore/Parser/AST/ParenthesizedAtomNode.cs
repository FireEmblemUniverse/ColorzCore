using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;

namespace ColorzCore.Parser.AST
{
    class ParenthesizedAtomNode : AtomNodeKernel
    {
        private IAtomNode inner;
        public override int Precedence => 1;
        public ParamType Type => ParamType.ATOM;

        public ParenthesizedAtomNode(IAtomNode putIn)
        {
            inner = putIn;
        }

        public override int Evaluate()
        {
            return inner.Evaluate();
        }

        public override Maybe<string> GetIdentifier()
        {
            return new Nothing<string>();
        }

        public override string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('(');
            sb.Append(inner.PrettyPrint());
            sb.Append(')');
            return sb.ToString();
        }
    }
}

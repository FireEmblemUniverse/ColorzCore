using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;

namespace ColorzCore.Parser.AST
{
    public abstract class AtomNodeKernel : IAtomNode
    {
        public abstract int Precedence { get; }

        public abstract int Evaluate();

        public ParamType Type { get { return ParamType.ATOM; } }

        public byte[] ToBytes()
        {
            int temp = this.Evaluate();
            return new byte[4] { (byte)temp, (byte)(temp << 8), (byte)(temp << 16), (byte)(temp << 24) };
        }

        public override string ToString()
        {
            return Evaluate().ToString();
        }

        public virtual Maybe<string> GetIdentifier()
        {
            return new Nothing<string>();
        }

        public abstract string PrettyPrint();
    }
}

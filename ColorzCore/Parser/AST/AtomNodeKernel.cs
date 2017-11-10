using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public abstract class AtomNodeKernel : IAtomNode
    {
        public abstract int Precedence { get; }

        public abstract int Evaluate();

        public byte[] ToBytes()
        {
            int temp = this.Evaluate();
            return new byte[4] { (byte)temp, (byte)(temp << 8), (byte)(temp << 16), (byte)(temp << 24) };
        }
    }
}

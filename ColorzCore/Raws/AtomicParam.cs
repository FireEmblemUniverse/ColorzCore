using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Parser.AST;
using ColorzCore.DataTypes;

namespace ColorzCore.Raws
{
    class AtomicParam : IRawParam
    {
        public string Name { get; }

        public int Position { get; }

        public int Length { get; }

        public IEnumerable<byte> Fit(IParamNode input)
        {
            int res = ((IAtomNode)input).Evaluate();
            byte[] resBytes = { (byte)res, (byte)(res << 8), (byte)(res << 16), (byte)(res << 24) };
            return resBytes.Take(Length).PadTo<byte>(Length, 0);
        }

        public bool Fits(IParamNode input)
        {
            return input.Type == ParamType.ATOM;
        }
    }
}

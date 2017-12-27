using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Parser.AST;
using ColorzCore.DataTypes;
using System.Collections;

namespace ColorzCore.Raws
{
    class AtomicParam : IRawParam
    {
        public string Name { get; }

        public int Position { get; }

        public int Length { get; }

        private bool pointer;

        public AtomicParam(string name, int position, int length, bool isPointer)
        {
            Name = name;
            Position = position;
            Length = length;
            pointer = isPointer;
        }

        public void Set(BitArray data, IParamNode input)
        {
            Set(data, ((IAtomNode)input).Evaluate());
        }
        public void Set(BitArray data, int res)
        {
            if (pointer && res != 0)
                res |= 0x08000000;
            byte[] resBytes = { (byte)res, (byte)(res >> 8), (byte)(res >> 16), (byte)(res >> 24) };
            BitArray bits = new BitArray(resBytes);
            for (int i = Position; i < Position + Length; i++)
                data[i] = bits[i - Position];
        }
        public bool Fits(IParamNode input)
        {
            return input.Type == ParamType.ATOM;
        }
    }
}

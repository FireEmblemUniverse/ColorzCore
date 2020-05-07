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

        private readonly bool pointer;

        public AtomicParam(string name, int position, int length, bool isPointer)
        {
            Name = name;
            Position = position;
            Length = length;
            pointer = isPointer;
        }

        public void Set(byte[] data, IParamNode input)
        {
            Set(data, input.AsAtom().FromJust.CoerceInt());
        }

        public void Set(byte[] data, int value)
        {
            if (pointer && value != 0)
                value += 0x08000000; // TODO: better offset-to-address mapping

            data.SetBits(Position, Length, value);
        }

        public bool Fits(IParamNode input)
        {
            return input.Type == ParamType.ATOM;
        }
    }
}

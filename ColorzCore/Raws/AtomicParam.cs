using System;
using System.Collections;
using ColorzCore.Parser.AST;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;

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

        // Precondition: input is an IAtomNode
        public void Set(byte[] data, IParamNode input)
        {
            Set(data, (input as IAtomNode)!.CoerceInt());
        }

        public void Set(byte[] data, int value)
        {
            if (pointer && value != 0)
            {
                value = EAInterpreter.ConvertToAddress(value);
            }

            data.SetBits(Position, Length, value);
        }

        public bool Fits(IParamNode input)
        {
            return input.Type == ParamType.ATOM;
        }
    }
}

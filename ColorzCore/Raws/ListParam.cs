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
    class ListParam : IRawParam
    {
        public string Name { get; }
        public int Position { get; }
        public int Length { get; }

        private int numCoords;

        public ListParam(string name, int position, int length, int numCoords)
        {
            Name = name;
            Position = position;
            Length = length;
            this.numCoords = numCoords;
        }

        public void Set(BitArray data, IParamNode input)
        {
            int count = 0;
            IList<IAtomNode> interior = ((ListNode)input).Interior;
            int bitsPerAtom = Length / numCoords;
            foreach(IAtomNode a in interior)
            {
                int res = a.Evaluate();
                for(int i=0; i<bitsPerAtom; i++, res >>= 1)
                {
                    data[i + Position + count] = (res & 1) == 1;
                }
                count += bitsPerAtom;
            }
            for (; count < Length; count++)
                data[Position + count] = false;
        }

        public bool Fits(IParamNode input)
        {
            if (input.Type == ParamType.LIST)
            {
                ListNode n = (ListNode)input;
                return n.NumCoords <= numCoords;
            }
            else
                return false;
        }
    }
}

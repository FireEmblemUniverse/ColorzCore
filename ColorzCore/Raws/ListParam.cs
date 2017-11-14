using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Parser.AST;
using ColorzCore.DataTypes;

namespace ColorzCore.Raws
{
    class ListParam : IRawParam
    {
        public string Name { get; }
        public int Position { get; }
        public int Length { get; }

        private int minCoords, maxCoords;

        public ListParam(string name, int position, int length, int minCoords, int maxCoords)
        {
            Name = name;
            Position = position;
            Length = length;
            this.minCoords = minCoords;
            this.maxCoords = maxCoords;
        }

        public IEnumerable<byte> Fit(IParamNode input)
        {
            int count = 0;
            IList<IAtomNode> interior = ((ListNode)input).Interior;
            int bytesPerAtom = Length / maxCoords;
            foreach(IAtomNode a in interior)
            {
                int res = a.Evaluate();
                for(int i=0; i<bytesPerAtom; i++, res >>= 8)
                {
                    yield return (byte)res;
                    count++;
                }
            }
            for(;count < Length; count++)
                yield return 0;
        }

        public bool Fits(IParamNode input)
        {
            if (input.Type == ParamType.LIST)
            {
                ListNode n = (ListNode)input;
                return n.NumCoords >= minCoords && n.NumCoords <= maxCoords;
            }
            else
                return false;
        }
    }
}

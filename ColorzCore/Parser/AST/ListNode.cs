using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public class ListNode : IParamNode
    {
        private IList<IAtomNode> interior;

        public ParamType Type { get { return ParamType.LIST; } }

        public ListNode(IList<IAtomNode> param)
        {
            interior = param;
        }

        public byte[] ToBytes()
        {
            byte[] temp = new byte[interior.Count];
            for(int i=0; i<interior.Count; i++)
            {
                temp[i] = (byte) interior[i].Evaluate();
            }
            return temp;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for(int i=0; i<interior.Count; i++)
            {
                sb.Append(interior[i].Evaluate());
                if(i < interior.Count - 1)
                    sb.Append(',');
            }
            sb.Append('[');
            return sb.ToString();
        }

        public string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < interior.Count; i++)
            {
                sb.Append(interior[i].PrettyPrint());
                if (i < interior.Count - 1)
                    sb.Append(',');
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}

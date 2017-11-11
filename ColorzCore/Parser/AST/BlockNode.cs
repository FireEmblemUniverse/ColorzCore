using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class BlockNode : ILineNode
    {
        public List<ILineNode> Children { get; }

        public int Size {
            get
            {
                return Children.Sum((ILineNode n) => n.Size);
            } }

        public string PrettyPrint(int indentation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indentation);
            sb.Append('{');
            sb.Append('\n');
            foreach(ILineNode i in Children)
            {
                sb.Append(i.PrettyPrint(indentation + 4));
                sb.Append('\n');
            }
            sb.Append(' ', indentation);
            sb.Append('}');
            return sb.ToString();
        }
    }
}

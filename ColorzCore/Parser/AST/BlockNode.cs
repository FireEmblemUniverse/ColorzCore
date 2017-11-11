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
    }
}

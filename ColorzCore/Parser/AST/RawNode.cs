using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class RawNode : StatementNode
    {
        private Raw myRaw;

        public RawNode(Raw raw, IList<IParamNode> paramList) : base(paramList)
        {
            myRaw = raw;
        }

        public override int Size => myRaw.Length;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class StatementListNode : ILineNode
    {
        public StatementListNode(IList<StatementNode> list)
        {
            Statements = list;
        }

        public IList<StatementNode> Statements { get; }
    }
}

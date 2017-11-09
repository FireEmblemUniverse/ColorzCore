using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class StatementListNode : ILineNode
    {
        public StatementListNode(StatementNode statementNode)
        {
            Statements = new List<StatementNode>();
            Statements.Add(statementNode);
        }

        public IList<StatementNode> Statements { get; private set; }
    }
}

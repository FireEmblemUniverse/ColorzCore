using ColorzCore.IO;
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

        public int Size { get
            {
                return Statements.Sum((StatementNode n) => n.Size);
            } }
        public string PrettyPrint(int indentation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indentation);
            foreach (StatementNode n in Statements)
            {
                sb.Append(n.PrettyPrint(0));
                sb.Append(';');
            }
            return sb.ToString();
        }
        public void WriteData(ROM rom)
        {
            foreach (StatementNode child in Statements)
            {
                child.WriteData(rom);
            }
        }
    }
}

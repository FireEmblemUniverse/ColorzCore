using ColorzCore.Lexer;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    abstract class StatementNode : ILineNode
    {
        public Token Raw { get; }
        public IList<IParamNode> Parameters { get; }

        protected StatementNode(IList<IParamNode> parameters)
        {
            Parameters = parameters;
        }

        public abstract int Size { get; }

        /*
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Raw.Content);
            sb.Append(' ');
            foreach(IParamNode n in Parameters)
            {
                sb.Append(n.ToString());
            }
            return sb.ToString();
        }
        */
    }
}

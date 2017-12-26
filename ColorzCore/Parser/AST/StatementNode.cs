using ColorzCore.IO;
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
        public IList<IParamNode> Parameters { get; }

        protected StatementNode(IList<IParamNode> parameters)
        {
            Parameters = parameters;
        }

        public abstract int Size { get; }

        public abstract string PrettyPrint(int indentation);
        public abstract void WriteData(ROM rom);
    }
}

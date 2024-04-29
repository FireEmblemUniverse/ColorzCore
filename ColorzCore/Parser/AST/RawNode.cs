using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColorzCore.Parser.AST
{
    public class RawNode : StatementNode
    {
        public Raw Raw { get; }
        private Token myToken;
        private int Offset { get; }

        public RawNode(Raw raw, Token t, int offset, IList<IParamNode> paramList) : base(paramList)
        {
            myToken = t;
            Raw = raw;
            Offset = offset;
        }

        public override int Size => Raw.LengthBytes(Parameters.Count);

        public override string PrettyPrint(int indentation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indentation);
            sb.Append(myToken.Content);
            foreach (IParamNode n in Parameters)
            {
                sb.Append(' ');
                sb.Append(n.PrettyPrint());
            }
            return sb.ToString();
        }

        public override void WriteData(IOutput output)
        {
            output.WriteTo(Offset, Raw.GetBytes(Parameters));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public class StringNode : IParamNode
    {
        private string myString;

        public ParamType Type { get { return ParamType.STRING; } }

        public StringNode(string value)
        {
            myString = value;
        }

        public byte[] ToBytes()
        {
            return Encoding.ASCII.GetBytes(myString);
        }

        public override string ToString()
        {
            return myString;
        }
        public string PrettyPrint()
        {
            return '"' + ToString() + '"';
        }
    }
}

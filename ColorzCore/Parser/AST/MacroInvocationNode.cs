using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class MacroInvocationNode : IParamNode
    {
        private EAParser p;
        private Token invokeToken;
        private IList<IList<Token>> parameters;

        public MacroInvocationNode(EAParser p, Token invokeTok, IList<IList<Token>> parameters)
        {
            this.invokeToken = invokeTok;
            this.parameters = parameters;
        }

        public ParamType Type => ParamType.MACRO;

        //TODO: Uh..... yeah.... I'll figure out a way to have this, later.
        public byte[] ToBytes()
        {
            return new byte[0]{ }; //This should be OK?
        }

        public string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(invokeToken.Content);
            sb.Append('(');
            for(int i=0; i<parameters.Count; i++)
            {
                foreach (Token t in parameters[i])
                {
                    sb.Append(t.Content);
                }
                if (i < parameters.Count - 1)
                    sb.Append(',');
            }
            sb.Append(')');
            return sb.ToString();
        }

        public IEnumerator<Token> ExpandMacro()
        {
            return p.Macros[invokeToken.Content][parameters.Count].ApplyMacro(invokeToken, parameters);
        }
    }
}

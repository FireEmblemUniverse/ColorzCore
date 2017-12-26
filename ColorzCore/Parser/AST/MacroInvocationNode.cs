using ColorzCore.DataTypes;
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
        public IList<IList<Token>> Parameters { get; }

        public MacroInvocationNode(EAParser p, Token invokeTok, IList<IList<Token>> parameters)
        {
            this.p = p;
            this.invokeToken = invokeTok;
            this.Parameters = parameters;
        }

        public ParamType Type => ParamType.MACRO;

        public string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(invokeToken.Content);
            sb.Append('(');
            for(int i=0; i<Parameters.Count; i++)
            {
                foreach (Token t in Parameters[i])
                {
                    sb.Append(t.Content);
                }
                if (i < Parameters.Count - 1)
                    sb.Append(',');
            }
            sb.Append(')');
            return sb.ToString();
        }

        public IEnumerable<Token> ExpandMacro()
        {
            return p.Macros[invokeToken.Content][Parameters.Count].ApplyMacro(invokeToken, Parameters);
        }

        public Either<int, string> TryEvaluate()
        {
            return new Right<int, string>("Expected atomic parameter.");
        }

        public string Name { get { return invokeToken.Content; } }

        public Location MyLocation { get { return invokeToken.Location; } }
    }
}

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
        private IList<IParamNode> parameters;

        public MacroInvocationNode(EAParser p, Token invokeTok, IList<IParamNode> parameters)
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
    }
}

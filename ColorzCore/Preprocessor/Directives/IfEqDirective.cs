using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class IfEqDirective : IDirective
    {
        public int MinParams => 2;

        public int? MaxParams => 2;

        public bool RequireInclusion => false;
        
        public bool ExpandFirstParam => true;

        public Maybe<ILineNode> Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            Maybe<string> identifier;
            bool flag = parameters[0].Equals(parameters[1]);
            p.Inclusion = new ImmutableStack<bool?>(flag, p.Inclusion);
            return new Nothing<ILineNode>();
        }
    }
}

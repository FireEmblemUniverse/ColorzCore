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
    class ElIfEqDirective : IDirective
    {
        public int MinParams => 0;

        public int? MaxParams => 0;

        public bool RequireInclusion => false;
        
        public bool ExpandFirstParam => false;

        public Maybe<ILineNode> Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            if (p.Inclusion.IsEmpty)
                p.Error(self.Location, "No matching if.");
            else {
                if(p.Inclusion.Head.HasValue && p.Inclusion.Head.Value) {
                    // Include no more of the block.
                    p.Inclusion = new ImmutableStack<bool?>(null, p.Inclusion.Tail);
                } else if(p.Inclusion.Head.HasValue) {
                    p.Inclusion = new ImmutableStack<bool?>(parameters[0].Equals(parameters[1]), p.Inclusion.Tail);
                }
            }
            return new Nothing<ILineNode>();
        }
    }
}

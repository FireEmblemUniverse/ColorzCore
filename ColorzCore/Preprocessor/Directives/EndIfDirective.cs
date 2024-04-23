using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class EndIfDirective : SimpleDirective
    {
        public override int MinParams => 0;

        public override int? MaxParams => 0;

        public override bool RequireInclusion => false;

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            if (p.Inclusion.IsEmpty)
                p.Error(self.Location, "No matching if[n]def.");
            else
                p.Inclusion = p.Inclusion.Tail;

            return null;
        }
    }
}

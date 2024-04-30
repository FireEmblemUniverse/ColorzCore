using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class ElseDirective : SimpleDirective
    {
        public override int MinParams => 0;

        public override int? MaxParams => 0;

        public override bool RequireInclusion => false;

        public override void Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            if (p.Inclusion.IsEmpty)
            {
                p.Logger.Error(self.Location, "No matching conditional (if, ifdef, ifndef).");
            }
            else
            {
                p.Inclusion = new ImmutableStack<bool>(!p.Inclusion.Head, p.Inclusion.Tail);
            }
        }
    }
}

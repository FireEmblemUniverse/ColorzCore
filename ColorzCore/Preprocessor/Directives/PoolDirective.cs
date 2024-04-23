using System;
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class PoolDirective : SimpleDirective
    {
        public override int MinParams => 0;
        public override int? MaxParams => 0;
        public override bool RequireInclusion => true;

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            BlockNode result = new BlockNode();

            // Iterating indices (and not values via foreach)
            // to avoid crashes occuring with AddToPool within AddToPool

            for (int i = 0; i < p.Pool.Lines.Count; ++i)
            {
                Pool.PooledLine line = p.Pool.Lines[i];

                MergeableGenerator<Token> tempGenerator = new MergeableGenerator<Token>(line.Tokens);
                tempGenerator.MoveNext();

                while (!tempGenerator.EOS)
                {
                    p.ParseLine(tempGenerator, line.Scope).IfJust(
                        (lineNode) => result.Children.Add(lineNode));
                }
            }

            p.Pool.Lines.Clear();

            return result;
        }
    }
}

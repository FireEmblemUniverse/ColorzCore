using System;
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class PoolDirective : IDirective
    {
        public int MinParams => 0;
        public int? MaxParams => 0;
        public bool RequireInclusion => true;

        public Maybe<ILineNode> Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            BlockNode result = new BlockNode();

            foreach (List<Token> line in p.PooledLines)
            {
                MergeableGenerator<Token> tempGenerator = new MergeableGenerator<Token>(line);
                tempGenerator.MoveNext();

                while (!tempGenerator.EOS)
                {
                    p.ParseLine(tempGenerator, p.GlobalScope).IfJust(
                        (lineNode) => result.Children.Add(lineNode));
                }
            }

            p.PooledLines.Clear();

            return new Just<ILineNode>(result);
        }
    }
}

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
            foreach (List<Token> line in p.PooledLines)
            {
                tokens.PrependEnumerator(line.GetEnumerator());
            }

            p.PooledLines.Clear();

            return new Nothing<ILineNode>();
        }
    }
}

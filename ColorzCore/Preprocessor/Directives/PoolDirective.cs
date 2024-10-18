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

        private readonly Pool pool;

        public PoolDirective(Pool pool)
        {
            this.pool = pool;
        }

        public override void Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            // Iterating indices (and not values via foreach)
            // to avoid crashes occuring with AddToPool within AddToPool

            for (int i = 0; i < pool.Lines.Count; ++i)
            {
                Pool.PooledLine line = pool.Lines[i];

                MergeableGenerator<Token> tempGenerator = new MergeableGenerator<Token>(line.Tokens);
                tempGenerator.MoveNext();

                while (!tempGenerator.EOS)
                {
                    p.ParseLine(tempGenerator);
                }
            }

            pool.Lines.Clear();
        }
    }
}

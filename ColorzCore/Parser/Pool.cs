using System;
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser
{
    public class Pool
    {
        public struct PooledLine
        {
            public List<Token> Tokens { get; private set; }

            public PooledLine(List<Token> tokens)
            {
                Tokens = tokens;
            }
        }

        public static readonly string pooledLabelPrefix = "__POOLED$";

        public List<PooledLine> Lines { get; private set; }

        private long poolLabelCounter;

        public Pool()
        {
            Lines = new List<PooledLine>();
            poolLabelCounter = 0;
        }

        public string MakePoolLabelName()
        {
            // The presence of $ in the label name guarantees that it can't be a user label
            return $"{pooledLabelPrefix}{poolLabelCounter++}";
        }
    }
}

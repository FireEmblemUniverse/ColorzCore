using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Parser.AST
{
    class DataNode : ILineNode
    {
        private readonly int offset;
        private readonly byte[] data;

        public DataNode(int offset, byte[] data)
        {
            this.offset = offset;
            this.data = data;
        }

        public int Size => data.Length;

        public string PrettyPrint(int indentation)
        {
            return $"Raw Data Block of Length {Size}";
        }

        public void WriteData(IOutput output)
        {
            output.WriteTo(offset, data);
        }

        public void EvaluateExpressions(ICollection<(Location, Exception)> evaluationErrors, EvaluationPhase evaluationPhase)
        {
            // Nothing to be done because we contain no expressions.
        }
    }
}

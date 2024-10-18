using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Parser.AST
{
    public interface ILineNode
    {
        int Size { get; }
        string PrettyPrint(int indentation);
        void WriteData(IOutput output);
        void EvaluateExpressions(ICollection<(Location, Exception)> evaluationErrors, EvaluationPhase evaluationPhase); // outputs errors into evaluationErrors
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;

namespace ColorzCore.Parser.AST
{
    public interface IParamNode
    {
        string? ToString(); //For use in other programs.
        ParamType Type { get; }
        string PrettyPrint();
        Location MyLocation { get; }

        // TODO: Abstract this into a general traverse method.
        IParamNode SimplifyExpressions(Action<Exception> handler, EvaluationPhase evaluationPhase);
    }

    public static class ParamExtensions
    {
        public static IParamNode Simplify(this IParamNode n, EvaluationPhase evaluationPhase)
        {
            return n.SimplifyExpressions(e => { }, evaluationPhase);
        }
    }
}

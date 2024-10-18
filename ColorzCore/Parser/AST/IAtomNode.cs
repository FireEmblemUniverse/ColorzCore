using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Parser.AST
{
    public interface IAtomNode : IParamNode
    {
        //TODO: Simplify() partial evaluation as much as is defined, to save on memory space.
        int Precedence { get; }
        int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase); //Simplifies the AST as much as possible.
    }

    public static class AtomExtensions
    {
        public static int CoerceInt(this IAtomNode n)
        {
            return n.TryEvaluate(e => throw e, EvaluationPhase.Final)!.Value;
        }

        public static IAtomNode Simplify(this IAtomNode self, Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            return self.TryEvaluate(handler, evaluationPhase).IfJust(intValue => FromInt(self.MyLocation, intValue), () => self);
        }

        public static IAtomNode FromInt(Location location, int intValue)
        {
            return new NumberNode(location, intValue);
        }
    }
}

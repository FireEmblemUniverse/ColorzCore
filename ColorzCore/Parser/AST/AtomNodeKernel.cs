using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    public abstract class AtomNodeKernel : IAtomNode
    {
        public abstract int Precedence { get; }

        public ParamType Type => ParamType.ATOM;

        public abstract string PrettyPrint();
        public abstract Location MyLocation { get; }

        public abstract int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase);

        public IParamNode SimplifyExpressions(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            return this.Simplify(handler, evaluationPhase);
        }
    }
}

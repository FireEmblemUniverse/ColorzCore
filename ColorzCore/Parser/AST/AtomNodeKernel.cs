using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    public abstract class AtomNodeKernel : IAtomNode
    {
        public abstract int Precedence { get; }

        public ParamType Type => ParamType.ATOM;

        public virtual string? GetIdentifier()
        {
            return null;
        }

        public abstract string PrettyPrint();
        public abstract IEnumerable<Token> ToTokens();
        public abstract Location MyLocation { get; }

        public abstract int? TryEvaluate(TAction<Exception> handler, EvaluationPhase evaluationPhase);

        public IParamNode SimplifyExpressions(TAction<Exception> handler, EvaluationPhase evaluationPhase)
        {
            return this.Simplify(handler, evaluationPhase);
        }

        public IAtomNode? AsAtom()
        {
            return this;
        }
    }
}

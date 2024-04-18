using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    abstract class StatementNode : ILineNode
    {
        public IList<IParamNode> Parameters { get; }

        protected StatementNode(IList<IParamNode> parameters)
        {
            Parameters = parameters;
        }

        public abstract int Size { get; }

        public abstract string PrettyPrint(int indentation);
        public abstract void WriteData(IOutput output);

        public void EvaluateExpressions(ICollection<(Location, Exception)> evaluationErrors)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                Parameters[i] = Parameters[i].SimplifyExpressions(e => evaluationErrors.Add(e switch
                {
                    IdentifierNode.UndefinedIdentifierException uie => (uie.CausedError.Location, uie),
                    Closure.SymbolComputeException sce => (sce.Expression.MyLocation, sce),
                    _ => (Parameters[i].MyLocation, e),
                }));
            }
        }
    }
}

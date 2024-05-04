using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColorzCore.Parser.AST
{
    public class RawNode : ILineNode
    {
        public IList<IParamNode> Parameters { get; }
        public Raw Raw { get; }
        private int Offset { get; }

        public RawNode(Raw raw, int offset, IList<IParamNode> parameters)
        {
            Parameters = parameters;
            Raw = raw;
            Offset = offset;
        }

        public void EvaluateExpressions(ICollection<(Location, Exception)> evaluationErrors, EvaluationPhase evaluationPhase)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                Parameters[i] = Parameters[i].SimplifyExpressions(e => evaluationErrors.Add(e switch
                {
                    IdentifierNode.UndefinedIdentifierException uie => (uie.CausedError.Location, uie),
                    Closure.SymbolComputeException sce => (sce.Expression.MyLocation, sce),
                    _ => (Parameters[i].MyLocation, e),
                }), evaluationPhase);
            }
        }

        public int Size => Raw.LengthBytes(Parameters.Count);

        public string PrettyPrint(int indentation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(' ', indentation);
            sb.Append(Raw.Name);
            foreach (IParamNode n in Parameters)
            {
                sb.Append(' ');
                sb.Append(n.PrettyPrint());
            }
            return sb.ToString();
        }

        public void WriteData(IOutput output)
        {
            output.WriteTo(Offset, Raw.GetBytes(Parameters));
        }
    }
}

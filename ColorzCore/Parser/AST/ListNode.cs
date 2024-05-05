using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColorzCore.Parser.AST
{
    public class ListNode : IParamNode
    {
        public Location MyLocation { get; }
        public IList<IAtomNode> Interior { get; }

        public ParamType Type { get { return ParamType.LIST; } }

        public ListNode(Location startLocation, IList<IAtomNode> param)
        {
            MyLocation = startLocation;
            Interior = param;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < Interior.Count; i++)
            {
                sb.Append(Interior[i].CoerceInt());
                if (i < Interior.Count - 1)
                    sb.Append(',');
            }
            sb.Append(']');
            return sb.ToString();
        }

        public string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < Interior.Count; i++)
            {
                sb.Append(Interior[i].PrettyPrint());
                if (i < Interior.Count - 1)
                    sb.Append(',');
            }
            sb.Append(']');
            return sb.ToString();
        }

        public IParamNode SimplifyExpressions(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            IEnumerable<Token> acc = new List<Token>();
            for (int i = 0; i < Interior.Count; i++)
            {
                Interior[i] = Interior[i].Simplify(handler, evaluationPhase);
            }
            return this;
        }

        public int NumCoords { get { return Interior.Count; } }
    }
}

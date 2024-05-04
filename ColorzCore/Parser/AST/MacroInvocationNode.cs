using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class MacroInvocationNode : IParamNode
    {
        public class MacroException : Exception
        {
            public MacroInvocationNode CausedError { get; private set; }
            public MacroException(MacroInvocationNode min) : base(min.invokeToken.Content)
            {
                CausedError = min;
            }
        }

        private readonly EAParser p;
        private readonly Token invokeToken;
        public IList<IList<Token>> Parameters { get; }

        public MacroInvocationNode(EAParser p, Token invokeTok, IList<IList<Token>> parameters)
        {
            this.p = p;
            invokeToken = invokeTok;
            Parameters = parameters;
        }

        public ParamType Type => ParamType.MACRO;

        public string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(invokeToken.Content);
            sb.Append('(');
            for (int i = 0; i < Parameters.Count; i++)
            {
                foreach (Token t in Parameters[i])
                {
                    sb.Append(t.Content);
                }
                if (i < Parameters.Count - 1)
                    sb.Append(',');
            }
            sb.Append(')');
            return sb.ToString();
        }

        public IEnumerable<Token> ExpandMacro()
        {
            return p.Macros.GetMacro(invokeToken.Content, Parameters.Count).ApplyMacro(invokeToken, Parameters);
        }

        public Either<int, string> TryEvaluate()
        {
            return new Right<int, string>("Expected atomic parameter.");
        }

        public string Name { get { return invokeToken.Content; } }

        public Location MyLocation { get { return invokeToken.Location; } }

        public IAtomNode? AsAtom() { return null; }

        public IParamNode SimplifyExpressions(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            handler(new MacroException(this));
            return this;
        }
    }
}

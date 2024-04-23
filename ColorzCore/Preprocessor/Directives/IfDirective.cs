using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class IfDirective : SimpleDirective
    {
        public override int MinParams => 1;

        public override int? MaxParams => 1;

        public override bool RequireInclusion => false;

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            bool flag = true;

            foreach (IParamNode parameter in parameters)
            {
                if (parameter is IAtomNode atomNode)
                {
                    if (atomNode.TryEvaluate(e => p.Error(self.Location, $"Error while evaluating expression: {e.Message}"), EvaluationPhase.Immediate) is int value)
                    {
                        flag = value != 0;
                    }
                }
                else
                {
                    p.Error(self.Location, "Expected an expression.");
                }
            }

            p.Inclusion = new ImmutableStack<bool>(flag, p.Inclusion);
            return null;
        }
    }
}

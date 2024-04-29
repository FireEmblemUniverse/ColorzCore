using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class IfDefinedDirective : SimpleDirective
    {
        public override int MinParams => 1;

        public override int? MaxParams => 1;

        public override bool RequireInclusion => false;

        public bool Inverted { get; }

        public IfDefinedDirective(bool invert)
        {
            Inverted = invert;
        }

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            bool flag = true;
            string? identifier;
            foreach (IParamNode parameter in parameters)
            {
                if (parameter.Type == ParamType.ATOM && (identifier = ((IAtomNode)parameter).GetIdentifier()) != null)
                {
                    // TODO: Built in definitions?
                    bool isDefined = p.Macros.ContainsName(identifier) || p.Definitions.ContainsKey(identifier);
                    flag &= Inverted ? !isDefined : isDefined;
                }
                else
                {
                    p.Logger.Error(parameter.MyLocation, "Definition name must be an identifier.");
                }
            }
            p.Inclusion = new ImmutableStack<bool>(flag, p.Inclusion);
            return null;
        }
    }
}

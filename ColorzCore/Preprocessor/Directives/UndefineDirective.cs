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
    class UndefineDirective : SimpleDirective
    {
        public override int MinParams => 1;

        public override int? MaxParams => null;

        public override bool RequireInclusion => true;

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            foreach (IParamNode parm in parameters)
            {
                string s = parm.ToString()!;
                if (p.Definitions.ContainsKey(s))
                    p.Definitions.Remove(s);
                else
                    p.Logger.Warning(parm.MyLocation, "Undefining non-existant definition: " + s);
            }
            return null;
        }
    }
}

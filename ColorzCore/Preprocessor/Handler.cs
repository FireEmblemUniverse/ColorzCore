using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor.Directives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Preprocessor
{
    class Handler
    {
        private static readonly Dictionary<string, IDirective> DIRECTIVE_DICT = new Dictionary<string, IDirective>
        {
            { "include", new IncludeDirective() },
            { "incbin", new IncludeBinaryDirective() },
            { "incext", new IncludeExternalDirective() },
            { "inctext", new IncludeToolEventDirective() },
            { "inctevent", new IncludeToolEventDirective() },
            { "ifdef", new IfDefinedDirective() },
            { "ifndef", new IfNotDefinedDirective() },
            { "else", new ElseDirective() },
            { "endif", new EndIfDirective() }
        };

        public static Either<Maybe<ILineNode>, string> HandleDirective(EAParser p, Token directive, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            string directiveName = directive.Content.Substring(1);
            if(DIRECTIVE_DICT.ContainsKey(directiveName))
            {
                IDirective toExec = DIRECTIVE_DICT[directiveName];
                if (!toExec.RequireInclusion || p.IsIncluding)
                {
                    if (toExec.MinParams <= parameters.Count && parameters.Count <= toExec.MaxParams)
                    {
                        return toExec.Execute(p, directive, parameters, tokens);
                    }
                    else
                    {
                        return new Right<Maybe<ILineNode>, string>("Invalid number of parameters (" + parameters.Count + ") to directive " + directiveName + ".");
                    }
                }
                else
                {
                    return new Left<Maybe<ILineNode>,string>(new Nothing<ILineNode>());
                }
            }
            else
            {
                return new Right<Maybe<ILineNode>, string>("Directive not recognized: " + directiveName);
            }
        }
    }
}

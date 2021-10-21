using ColorzCore.DataTypes;
using ColorzCore.IO;
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
    class DirectiveHandler
    {
        private Dictionary<string, IDirective> directives;

        public DirectiveHandler(IncludeFileSearcher includeSearcher, IncludeFileSearcher toolSearcher)
        {
            directives = new Dictionary<string, IDirective>
            {
                { "include", new IncludeDirective { FileSearcher = includeSearcher } },
                { "incbin", new IncludeBinaryDirective { FileSearcher = includeSearcher } },
                { "incext", new IncludeExternalDirective { FileSearcher = toolSearcher } },
                { "inctext", new IncludeToolEventDirective { FileSearcher = toolSearcher } },
                { "inctevent", new IncludeToolEventDirective { FileSearcher = toolSearcher } },
                { "ifdef", new IfDefinedDirective() },
                { "ifndef", new IfNotDefinedDirective() },
                { "else", new ElseDirective() },
                { "endif", new EndIfDirective() },
                { "define", new DefineDirective() },
                { "pool", new PoolDirective() },
                { "undef", new UndefineDirective() },
                { "ifeq", new IfEqDirective() },
                { "elifeq", new ElIfEqDirective() },
            };
        }
        
        private Maybe<IDirective> GetDirective(Token directive) {
            string directiveName = directive.Content.Substring(1);
            if (directives.TryGetValue(directiveName, out IDirective found))
            {
                return new Just<IDirective>(found);
            } else {
                return new Nothing<IDirective>();
            }
        }
        
        public bool ExpandFirstParam(Token directive) { return GetDirective(directive).IfJust((IDirective dir) => dir.ExpandFirstParam, () => false); }

        public Maybe<ILineNode> HandleDirective(EAParser p, Token directive, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            return GetDirective(directive).IfJust(
                (IDirective toExec) => { 
                    if (!toExec.RequireInclusion || p.IsIncluding)
                    {
                        if (toExec.MinParams <= parameters.Count && (!toExec.MaxParams.HasValue || parameters.Count <= toExec.MaxParams))
                        {
                            return toExec.Execute(p, directive, parameters, tokens);
                        }
                        else
                        {
                            p.Error(directive.Location, "Invalid number of parameters (" + parameters.Count + ") to directive " + directiveName + ".");
                            return new Nothing<ILineNode>();
                        }
                    }
                },
                () => { p.Error(directive.Location, "Directive not recognized: " + directiveName); return new Nothing<ILineNode>(); }
            );
        }
    }
}

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
    public class DirectiveHandler
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
                { "ifdef", new IfDefinedDirective(false) },
                { "ifndef", new IfDefinedDirective(true) },
                { "if", new IfDirective() },
                { "else", new ElseDirective() },
                { "endif", new EndIfDirective() },
                { "define", new DefineDirective() },
                { "pool", new PoolDirective() },
                { "undef", new UndefineDirective() },
            };
        }

        public ILineNode? HandleDirective(EAParser p, Token directive, MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            string directiveName = directive.Content.Substring(1);

            if (directives.TryGetValue(directiveName, out IDirective? toExec))
            {
                if (!toExec.RequireInclusion || p.IsIncluding)
                {
                    return toExec.Execute(p, directive, tokens, scopes);
                }
            }
            else
            {
                p.Error(directive.Location, $"Directive not recognized: {directiveName}");
            }

            return null;
        }
    }
}

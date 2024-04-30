using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor.Directives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Preprocessor
{
    public class DirectiveHandler
    {
        public Dictionary<string, IDirective> Directives { get; }

        public DirectiveHandler(IncludeFileSearcher includeSearcher)
        {
            // TODO: move out from this directives that need external context
            // (already done for pool, but could be done for includes as well)

            Directives = new Dictionary<string, IDirective>
            {
                { "include", new IncludeDirective { FileSearcher = includeSearcher } },
                { "incbin", new IncludeBinaryDirective { FileSearcher = includeSearcher } },
                { "ifdef", new IfDefinedDirective(false) },
                { "ifndef", new IfDefinedDirective(true) },
                { "if", new IfDirective() },
                { "else", new ElseDirective() },
                { "endif", new EndIfDirective() },
                { "define", new DefineDirective() },
                { "undef", new UndefineDirective() },
            };
        }

        public void HandleDirective(EAParser p, Token directive, MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            string directiveName = directive.Content.Substring(1);

            if (Directives.TryGetValue(directiveName, out IDirective? toExec))
            {
                if (!toExec.RequireInclusion || p.IsIncluding)
                {
                    toExec.Execute(p, directive, tokens, scopes);
                }
            }
            else
            {
                p.Logger.Error(directive.Location, $"Directive not recognized: {directiveName}");
                p.IgnoreRestOfLine(tokens);
            }
        }
    }
}

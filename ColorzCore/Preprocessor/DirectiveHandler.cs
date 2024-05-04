using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Preprocessor.Directives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Preprocessor
{
    public class DirectiveHandler
    {
        // TODO: do we need this class? Could we not just have this part of EAParser?

        public Dictionary<string, IDirective> Directives { get; }

        public DirectiveHandler()
        {
            Directives = new Dictionary<string, IDirective>
            {
                { "ifdef", new IfDefinedDirective(false) },
                { "ifndef", new IfDefinedDirective(true) },
                { "if", new IfDirective() },
                { "else", new ElseDirective() },
                { "endif", new EndIfDirective() },
                { "define", new DefineDirective() },
                { "undef", new UndefineDirective() },
            };
        }

        public void HandleDirective(EAParser p, Token directive, MergeableGenerator<Token> tokens)
        {
            string directiveName = directive.Content.Substring(1);

            if (Directives.TryGetValue(directiveName, out IDirective? toExec))
            {
                if (!toExec.RequireInclusion || p.IsIncluding)
                {
                    toExec.Execute(p, directive, tokens);
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

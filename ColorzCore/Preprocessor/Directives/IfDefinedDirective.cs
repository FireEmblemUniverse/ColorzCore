using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;

namespace ColorzCore.Preprocessor.Directives
{
    class IfDefinedDirective : IDirective
    {
        // This directive does not inherit SimpleDirective so as to avoid having its parameter expanded

        public bool RequireInclusion => false;

        public bool Inverted { get; }

        public IfDefinedDirective(bool invert)
        {
            Inverted = invert;
        }

        public void Execute(EAParser p, Token self, MergeableGenerator<Token> tokens)
        {
            if (tokens.Current.Type != TokenType.IDENTIFIER)
            {
                p.Logger.Error(self.Location, $"Invalid use of directive '{self.Content}': expected macro name, got {tokens.Current}.");
                p.IgnoreRestOfLine(tokens);
            }
            else
            {
                string identifier = tokens.Current.Content;
                tokens.MoveNext();

                // here we could parse a potential parameter list/specifier

                bool isDefined = p.Macros.ContainsName(identifier) || p.Definitions.ContainsKey(identifier);
                bool flag = Inverted ? !isDefined : isDefined;

                p.Inclusion = new ImmutableStack<bool>(flag, p.Inclusion);

                if (tokens.Current.Type != TokenType.NEWLINE)
                {
                    p.Logger.Error(self.Location, $"Garbage at the end of directive '{self.Content}' (got {tokens.Current}).");
                    p.IgnoreRestOfLine(tokens);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;

namespace ColorzCore.Preprocessor.Directives
{
    class UndefineDirective : IDirective
    {
        public bool RequireInclusion => true;

        public void Execute(EAParser p, Token self, MergeableGenerator<Token> tokens)
        {
            if (tokens.Current.Type == TokenType.NEWLINE)
            {
                p.Logger.Error(self.Location, $"Invalid use of directive '{self.Content}': expected at least one macro name.");
            }

            while (tokens.Current.Type != TokenType.NEWLINE)
            {
                Token current = tokens.Current;
                tokens.MoveNext();

                switch (current.Type)
                {
                    case TokenType.IDENTIFIER:
                        ApplyUndefine(p, current);
                        break;

                    default:
                        p.Logger.Error(self.Location, $"Invalid use of directive '{self.Content}': expected macro name, got {current}.");
                        p.IgnoreRestOfLine(tokens);
                        return;
                }
            }
        }

        private static void ApplyUndefine(EAParser parser, Token token)
        {
            string name = token.Content;

            if (!parser.Definitions.Remove(name))
            {
                parser.Logger.Warning(token.Location, $"Attempted to purge non existant definition '{name}'");
            }
        }
    }
}

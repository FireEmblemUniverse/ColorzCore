using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Preprocessor
{
    public class Definition
    {
        private readonly IList<Token>? replacement;

        /// <summary>
        /// A non-productive definition is a definition that doesn't participate in macro expansion.
        /// (it is still visible by ifdef and other such constructs).
        /// </summary>
        public bool NonProductive => replacement == null;

        public Definition()
        {
            replacement = null;
        }

        public Definition(Token token)
        {
            replacement = new List<Token> { token };
        }

        public Definition(IList<Token> defn)
        {
            replacement = defn;
        }

        public IEnumerable<Token> ApplyDefinition(Token token)
        {
            // assumes !NonProductive

            IList<Token> replacement = this.replacement!;
            MacroLocation macroLocation = new MacroLocation(token.Content, token.Location);

            for (int i = 0; i < replacement.Count; i++)
            {
                yield return replacement[i].MacroClone(macroLocation);
            }
        }
    }
}

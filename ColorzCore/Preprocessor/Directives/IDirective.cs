using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Preprocessor.Directives
{
    public interface IDirective
    {
        /***
         * Perform the directive's action, be it altering tokens, for just emitting a special ILineNode.
         * Precondition: MinParams <= parameters.Count <= MaxParams
         * 
         * Return: If a string is returned, it is interpreted as an error.
         */
        void Execute(EAParser p, Token self, MergeableGenerator<Token> tokens);

        /***
         * Whether requires the parser to be taking in tokens.
         * This may not hold when the parser is skipping, e.g. from an #ifdef.
         */
        bool RequireInclusion { get; }
    }
}

using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
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
        ILineNode? Execute(EAParser p, Token self, MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes);

        /***
         * Whether requires the parser to be taking in tokens.
         * This may not hold when the parser is skipping, e.g. from an #ifdef.
         */
        bool RequireInclusion { get; }
    }
}

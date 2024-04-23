using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    /***
     * Simple abstract base class for directives that don't care about the details of their parameters
     */
    public abstract class SimpleDirective : IDirective
    {
        public abstract bool RequireInclusion { get; }

        /***
         * Minimum number of parameters, inclusive. 
         */
        public abstract int MinParams { get; }

        /***
         * Maximum number of parameters, inclusive. Null for no limit.
         */
        public abstract int? MaxParams { get; }

        public ILineNode? Execute(EAParser p, Token self, MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            // Note: Not a ParseParamList because no commas.
            // HACK: #if wants its parameters to be expanded, but other directives (define, ifdef, undef, etc) do not
            IList<IParamNode> parameters = p.ParsePreprocParamList(tokens, scopes, self.Content == "#if");

            if (MinParams <= parameters.Count && (!MaxParams.HasValue || parameters.Count <= MaxParams))
            {
                return Execute(p, self, parameters, tokens);
            }
            else
            {
                p.Error(self.Location, $"Invalid number of parameters ({parameters.Count}) to directive {self}.");
                return null;
            }
        }

        public abstract ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens);
    }
}

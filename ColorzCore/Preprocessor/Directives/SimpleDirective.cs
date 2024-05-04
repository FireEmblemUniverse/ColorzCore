using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
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

        public void Execute(EAParser p, Token self, MergeableGenerator<Token> tokens)
        {
            // Note: Not a ParseParamList because no commas.
            // HACK: #if wants its parameters to be expanded, but other directives (define, ifdef, undef, etc) do not
            IList<IParamNode> parameters = p.ParsePreprocParamList(tokens);

            if (MinParams <= parameters.Count && (!MaxParams.HasValue || parameters.Count <= MaxParams))
            {
                Execute(p, self, parameters, tokens);
            }
            else
            {
                p.Logger.Error(self.Location, $"Invalid number of parameters ({parameters.Count}) to directive {self.Content}.");
            }
        }

        public abstract void Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens);
    }
}

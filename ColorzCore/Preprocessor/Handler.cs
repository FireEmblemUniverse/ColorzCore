using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Preprocessor
{
    class Handler
    {
        public static ILineNode HandleDirective(Token directive, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {




            return new EmptyNode();
            //throw new NotImplementedException();
        }
    }
}

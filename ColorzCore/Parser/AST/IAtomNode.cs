using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public interface IAtomNode : IParamNode
    {
        //TODO: Simplify() partial evaluation as much as is defined, to save on memory space.
		int Precedence { get; }
		int Evaluate(); //May throw errors. TODO: Remove and only do calls through TryEvaluate?
        Maybe<string> GetIdentifier();
        IEnumerable<Token> ToTokens();
        bool CanEvaluate();
        IAtomNode Simplify();
        bool DependsOnSymbol(string name);
    }
}

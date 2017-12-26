using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    public interface IParamNode
    {
        string ToString(); //For use in other programs.
        ParamType Type { get; }
        string PrettyPrint();
        Location MyLocation { get; }

        Either<int, string> TryEvaluate();
    }
}

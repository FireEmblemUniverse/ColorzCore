using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public interface IParamNode
    {
        string ToString(); //For use in other programs.
        ParamType Type { get; }
        byte[] ToBytes();
        string PrettyPrint();
    }
}

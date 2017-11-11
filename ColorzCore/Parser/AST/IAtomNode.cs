using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public interface IAtomNode : IParamNode
    {
		int Precedence { get; }
		int Evaluate();
        Maybe<string> GetIdentifier();
    }
}

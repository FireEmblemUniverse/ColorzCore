using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public interface IASTNode
    {
		ASTNodeType Type { get; }
		string File { get; }
		int Line { get; }
		int Column { get; }
    }
}

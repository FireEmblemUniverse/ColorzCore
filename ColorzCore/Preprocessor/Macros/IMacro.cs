using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Preprocessor.Macros
{
    public interface IMacro
    {
        IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters);
    }
}

using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Preprocessor.Macros
{
    class UserMacro : IMacro
    {
        readonly Dictionary<string, int> idToParamNum;
        readonly IList<Token> body;

        public UserMacro(IList<string> parameters, IList<Token> macroBody)
        {
            idToParamNum = new Dictionary<string, int>();

            for (int i = 0; i < parameters.Count; i++)
            {
                idToParamNum[parameters[i]] = i;
            }

            body = macroBody;
        }

        /***
         *   Precondition: parameters.Count = max(keys(idToParamNum))
         */
        public IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters, ImmutableStack<Closure> scopes)
        {
            foreach (Token t in body)
            {
                if (t.Type == TokenType.IDENTIFIER && idToParamNum.TryGetValue(t.Content, out int paramNum))
                {
                    foreach (Token t2 in parameters[paramNum])
                    {
                        yield return t2;
                    }
                }
                else
                {
                    yield return new Token(t.Type, head.Location, t.Content);
                }
            }
        }
    }
}

﻿using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser
{
    class Macro
    {
        Dictionary<string, int> idToParamNum; 
        IList<Token> body;

        public Macro(IList<Token> parameters, IList<Token> body)
        {
            idToParamNum = new Dictionary<string, int>();
            for(int i=0; i<parameters.Count; i++)
            {
                idToParamNum[parameters[i].Content] = i;
            }
            this.body = body;
        }

        /***
         *   Precondition: parameters.Count = max(keys(idToParamNum))
         */
        public IEnumerator<Token> ApplyMacro(Token head, IList<IList<Token>> parameters)
        {
            foreach(Token t in body)
            {
                if(t.Type == TokenType.IDENTIFIER && idToParamNum.ContainsKey(t.Content))
                {
                    foreach(Token t2 in parameters[idToParamNum[t.Content]])
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

using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser
{
    public class Definition
    {
        private IList<Token> replacement;

        public Definition()
        {
            replacement = new List<Token>();
        }

        public Definition(IList<Token> defn)
        {
            replacement = defn;
        }

        public bool ApplyDefinition(MergeableGenerator<Token> tokens)
        {
            if(replacement.Count == 0)
            {
                return tokens.MoveNext();
            }
            else
            {
                IList<Token> toPrepend = new List<Token>();
                Token defLoc = tokens.Current;
                for(int i=0; i<replacement.Count; i++)
                {
                    Location newLoc = new Location(defLoc.FileName, defLoc.LineNumber, defLoc.ColumnNumber + replacement[i].ColumnNumber - replacement[0].ColumnNumber);
                    toPrepend.Add(new Token(replacement[i].Type, newLoc, replacement[i].Content));
                }
                tokens.MoveNext();
                tokens.PrependEnumerator(toPrepend.GetEnumerator());
                return true;
            }
        }
    }
}

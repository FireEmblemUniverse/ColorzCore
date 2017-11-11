using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public class ListNode : IParamNode
    {
        public Location MyLocation { get; }
        private IList<IAtomNode> interior;

        public ParamType Type { get { return ParamType.LIST; } }

        public ListNode(Location startLocation, IList<IAtomNode> param)
        {
            MyLocation = startLocation;
            interior = param;
        }

        public byte[] ToBytes()
        {
            byte[] temp = new byte[interior.Count];
            for(int i=0; i<interior.Count; i++)
            {
                temp[i] = (byte) interior[i].Evaluate();
            }
            return temp;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for(int i=0; i<interior.Count; i++)
            {
                sb.Append(interior[i].Evaluate());
                if(i < interior.Count - 1)
                    sb.Append(',');
            }
            sb.Append('[');
            return sb.ToString();
        }

        public string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < interior.Count; i++)
            {
                sb.Append(interior[i].PrettyPrint());
                if (i < interior.Count - 1)
                    sb.Append(',');
            }
            sb.Append(']');
            return sb.ToString();
        }
        public IEnumerable<Token> ToTokens()
        {
            //Similar code to ParenthesizedAtom
            IList<IList<Token>> temp = new List<IList<Token>>();
            foreach(IAtomNode n in interior)
            {
                temp.Add(new List<Token>(n.ToTokens()));
            }
            Location myStart = temp[0][0].Location;
            Location myEnd = temp.Last().Last().Location;
            yield return new Token(TokenType.OPEN_BRACKET, new Location(myStart.file, myStart.lineNum, myStart.colNum - 1), "[");
            for (int i = 0; i < temp.Count; i++)
            {
                foreach (Token t in temp[i])
                {
                    yield return t;
                }
                if (i < temp.Count - 1)
                {
                    Location tempEnd = temp[i].Last().Location;
                    yield return new Token(TokenType.COMMA, new Location(tempEnd.file, tempEnd.lineNum, tempEnd.colNum + temp[i].Last().Content.Length), ",");
                }
            }
            yield return new Token(TokenType.CLOSE_BRACKET, new Location(myEnd.file, myEnd.lineNum, myEnd.colNum + temp.Last().Last().Content.Length), "]");
        }
    }
}

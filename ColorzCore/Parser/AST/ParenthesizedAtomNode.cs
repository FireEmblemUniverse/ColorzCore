using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    class ParenthesizedAtomNode : AtomNodeKernel
    {
        public override Location MyLocation { get; }
        private IAtomNode inner;
        public override int Precedence => 1;

        public int Tokens { get; private set; }

        public ParenthesizedAtomNode(Location startLocation, IAtomNode putIn)
        {
            MyLocation = startLocation;
            inner = putIn;
        }

        public override int Evaluate()
        {
            return inner.Evaluate();
        }

        public override Maybe<string> GetIdentifier()
        {
            return new Nothing<string>();
        }

        public override string PrettyPrint()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('(');
            sb.Append(inner.PrettyPrint());
            sb.Append(')');
            return sb.ToString();
        }
        public override IEnumerable<Token> ToTokens()
        {
            IList<Token> temp = new List<Token>(inner.ToTokens());
            Location myStart = temp[0].Location;
            Location myEnd = temp.Last().Location;
            yield return new Token(TokenType.OPEN_PAREN, new Location(myStart.file, myStart.lineNum, myStart.colNum - 1), "(");
            foreach(Token t in temp)
            {
                yield return t;
            }
            yield return new Token(TokenType.CLOSE_PAREN, new Location(myEnd.file, myEnd.lineNum, myEnd.colNum + temp.Last().Content.Length), "(");
        }

        public override bool CanEvaluate()
        {
            return inner.CanEvaluate();
        }

        public override IAtomNode Simplify()
        {
            inner = inner.Simplify();
            if(CanEvaluate())
            {
                return new NumberNode(MyLocation, Evaluate());
            } else
            {
                return this;
            }
        }

        public override bool EvaluationRequiresName(string name)
        {
            return inner.EvaluationRequiresName(name);
        }
    }
}

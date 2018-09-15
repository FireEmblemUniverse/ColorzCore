using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;

namespace ColorzCore.Parser.AST
{
    public class NumberNode : AtomNodeKernel
    {
        private int value;

        public override Location MyLocation { get; }
        public override int Precedence { get { return 11; } }

		public NumberNode(Token num)
		{
            MyLocation = num.Location;
            value = num.Content.ToInt(); 
        }
        public NumberNode(Token text, int value)
        {
            MyLocation = text.Location;
            this.value = value;
        }
        public NumberNode(Location loc, int value)
        {
            MyLocation = loc;
            this.value = value;
        }

        public override int Evaluate()
        {
            return value;
        }
        
        public override IEnumerable<Token> ToTokens () { yield return new Token(TokenType.NUMBER, MyLocation, value.ToString()); }

        public override bool CanEvaluate()
        {
            return true;
        }

        public override IAtomNode Simplify()
        {
            return this;
        }

        public override bool EvaluationRequiresName(string name)
        {
            return false;
        }
    }
}

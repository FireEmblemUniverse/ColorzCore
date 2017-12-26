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
        private Token number;
        private int value;

        public override Location MyLocation => number.Location;
        public override int Precedence { get { return 11; } }

		public NumberNode(Token num)
		{
            number = num;
            value = number.Content.ToInt(); 
        }
        public NumberNode(Token text, int value)
        {
            number = text;
            this.value = value;
        }
		
		public override int Evaluate()
        {
            return value;
        }
        
        public override string PrettyPrint()
        {
            return number.Content;
        }
        public override IEnumerable<Token> ToTokens () { yield return number; }
    }
}

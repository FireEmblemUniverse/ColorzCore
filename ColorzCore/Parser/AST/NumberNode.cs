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

        public override Location MyLocation => number.Location;
        public override int Precedence { get { return 11; } }

		public NumberNode(Token num)
		{
            number = num;
		}
		
		public override int Evaluate()
        {
            return number.Content.ToInt();
        }
        
        public override string PrettyPrint()
        {
            return number.Content;
        }
        public override IEnumerable<Token> ToTokens () { yield return number; }
    }
}

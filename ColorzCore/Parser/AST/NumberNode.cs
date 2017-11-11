using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public class NumberNode : AtomNodeKernel
    {
        private Token number;

		public override int Precedence { get { return 11; } }

		public NumberNode(Token num)
		{
            number = num;
		}
		
		public override int Evaluate()
        {
            string numString = number.Content;
            if(numString.StartsWith("$"))
            {
                return Convert.ToInt32(numString.Substring(1), 16);
            }
            else if(numString.StartsWith("0x"))
            {
                return Convert.ToInt32(numString.Substring(2), 16);
            }
            else if(numString.EndsWith("b"))
            {
                return Convert.ToInt32(numString.Substring(0, numString.Length-1), 2);
            }
            else
            {
                return Convert.ToInt32(numString);
            }
        }
        
        public override string PrettyPrint()
        {
            return number.Content;
        }
    }
}

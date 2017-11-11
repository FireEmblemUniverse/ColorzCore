using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public class IdentifierNode : AtomNodeKernel
    {
        private Token identifier;
        ImmutableStack<Closure> scope;

		public override int Precedence { get { return 11; } }

		public IdentifierNode(Token id, ImmutableStack<Closure> scopes)
		{
            identifier = id;
            scope = scopes;
		}
		
		public override int Evaluate()
        {
            ImmutableStack<Closure> temp = scope;
            while(!temp.IsEmpty)
            {
                if(temp.Head.Labels.ContainsKey(identifier.Content))
                    return temp.Head.Labels[identifier.Content];
                else
                    temp = temp.Tail;
            }
            throw new UndefinedIdentifierException(identifier);
        }
        
        public override Maybe<string> GetIdentifier()
        {
            return new Just<string>(identifier.Content);
        }

        public override string PrettyPrint()
        {
            return identifier.Content;
        }

        public class UndefinedIdentifierException : Exception
        {
            public Token CausedError { get; set; }
            public UndefinedIdentifierException(Token causedError) 
            {
                this.CausedError = causedError;
            }
        }
    }
}

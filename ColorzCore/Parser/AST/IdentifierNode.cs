using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;

namespace ColorzCore.Parser.AST
{
    public class IdentifierNode : AtomNodeKernel
    {
        private Token identifier;
        ImmutableStack<Closure> scope;

		public override int Precedence { get { return 11; } }
        public override Location MyLocation { get { return identifier.Location; } }

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
                if(temp.Head.HasLocalLabel(identifier.Content))
                    return temp.Head.GetLabel(identifier.Content);
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
            try
            {
                return "0x"+Evaluate().ToString("X");
            }
            catch (UndefinedIdentifierException)
            {
                return identifier.Content;
            }
        }

        public override IEnumerable<Token> ToTokens() { yield return identifier; }

        public class UndefinedIdentifierException : Exception
        {
            public Token CausedError { get; set; }
            public UndefinedIdentifierException(Token causedError) 
            {
                this.CausedError = causedError;
            }
        }

        public override string ToString()
        {
            return identifier.Content;
        }
    }
}

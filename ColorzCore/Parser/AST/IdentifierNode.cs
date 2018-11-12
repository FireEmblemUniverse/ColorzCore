using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Parser.AST
{
    public class IdentifierNode : AtomNodeKernel
    {
        private Token identifier;
        readonly ImmutableStack<Closure> scope;

		public override int Precedence { get { return 11; } }
        public override Location MyLocation { get { return identifier.Location; } }

        public IdentifierNode(Token id, ImmutableStack<Closure> scopes)
		{
            identifier = id;
            scope = scopes;
		}
		
		public override int ToInt()
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

        public override Maybe<int> Evaluate(ICollection<Token> undefinedIdentifiers)
        {
            try
            {
                return new Just<int>(ToInt());
            } catch(UndefinedIdentifierException e)
            {
                undefinedIdentifiers.Add(e.CausedError);
                return new Nothing<int>();
            }
        }
        
        public override Maybe<string> GetIdentifier()
        {
            return new Just<string>(identifier.Content);
        }

        public override string PrettyPrint()
        {
            try
            {
                return "0x"+ToInt().ToString("X");
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

        public override bool CanEvaluate()
        {
            return Enumerable.Any(scope, (Closure c) => c.HasLocalLabel(identifier.Content));
        }

        public override IAtomNode Simplify()
        {
            if (!CanEvaluate())
                return this;
            else
                return new NumberNode(identifier, ToInt());
        }

    }
}

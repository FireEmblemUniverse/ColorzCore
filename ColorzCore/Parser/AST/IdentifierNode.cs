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

        private int ToInt(EvaluationPhase evaluationPhase)
        {
            ImmutableStack<Closure> temp = scope;
            while (!temp.IsEmpty)
            {
                if (temp.Head.HasLocalSymbol(identifier.Content))
                    return temp.Head.GetSymbol(identifier.Content, evaluationPhase);
                else
                    temp = temp.Tail;
            }
            throw new UndefinedIdentifierException(identifier);
        }

        public override int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            try
            {
                return ToInt(evaluationPhase);
            }
            catch (UndefinedIdentifierException e)
            {
                handler(e);
                return null;
            }
            catch (Closure.SymbolComputeException e)
            {
                handler(e);
                return null;
            }
        }

        public override string? GetIdentifier()
        {
            return identifier.Content;
        }

        public override string PrettyPrint()
        {
            try
            {
                return $"0x{ToInt(EvaluationPhase.Immediate):X}";
            }
            catch (UndefinedIdentifierException)
            {
                return identifier.Content;
            }
            catch (Closure.SymbolComputeException)
            {
                return identifier.Content;
            }
        }

        public override IEnumerable<Token> ToTokens() { yield return identifier; }

        public class UndefinedIdentifierException : Exception
        {
            public Token CausedError { get; set; }
            public UndefinedIdentifierException(Token causedError) : base($"Undefined identifier `{causedError.Content}`")
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

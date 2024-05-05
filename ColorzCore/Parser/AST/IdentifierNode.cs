using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Parser.AST
{
    public class IdentifierNode : AtomNodeKernel
    {
        private readonly ImmutableStack<Closure> boundScope;

        public Token IdentifierToken { get; }

        public override int Precedence => int.MaxValue;
        public override Location MyLocation => IdentifierToken.Location;

        public IdentifierNode(Token identifierToken, ImmutableStack<Closure> scope)
        {
            IdentifierToken = identifierToken;
            boundScope = scope;
        }

        private int ToInt(EvaluationPhase evaluationPhase)
        {
            if (boundScope != null)
            {
                if (evaluationPhase == EvaluationPhase.Early)
                {
                    /* We only check the immediate scope.
                     * As an outer scope symbol may get shadowed by an upcoming inner scope symbol. */

                    if (boundScope.Head.HasLocalSymbol(IdentifierToken.Content))
                    {
                        return boundScope.Head.GetSymbol(IdentifierToken.Content, evaluationPhase);
                    }
                }
                else
                {
                    for (ImmutableStack<Closure> it = boundScope; !it.IsEmpty; it = it.Tail)
                    {
                        if (it.Head.HasLocalSymbol(IdentifierToken.Content))
                        {
                            return it.Head.GetSymbol(IdentifierToken.Content, evaluationPhase);
                        }
                    }
                }
            }

            throw new UndefinedIdentifierException(IdentifierToken);
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

        public override string PrettyPrint()
        {
            try
            {
                return $"0x{ToInt(EvaluationPhase.Immediate):X}";
            }
            catch (UndefinedIdentifierException)
            {
                return IdentifierToken.Content;
            }
            catch (Closure.SymbolComputeException)
            {
                return IdentifierToken.Content;
            }
        }

        public override string ToString()
        {
            return IdentifierToken.Content;
        }

        // TODO: move this outside of this class
        public class UndefinedIdentifierException : Exception
        {
            public Token CausedError { get; set; }

            public UndefinedIdentifierException(Token causedError) : base($"Undefined identifier `{causedError.Content}`")
            {
                CausedError = causedError;
            }
        }
    }
}

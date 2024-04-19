using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class OperatorNode : AtomNodeKernel
    {
        private IAtomNode left, right;

        public Token OperatorToken { get; }

        public override int Precedence { get; }
        public override Location MyLocation => OperatorToken.Location;

        public OperatorNode(IAtomNode l, Token op, IAtomNode r, int prec)
        {
            left = l;
            right = r;
            OperatorToken = op;
            Precedence = prec;
        }

        public override string PrettyPrint()
        {
            static string GetOperatorString(TokenType tokenType)
            {
                return tokenType switch
                {
                    TokenType.MUL_OP => "*",
                    TokenType.DIV_OP => "/",
                    TokenType.MOD_OP => "%",
                    TokenType.ADD_OP => "+",
                    TokenType.SUB_OP => "-",
                    TokenType.LSHIFT_OP => "<<",
                    TokenType.RSHIFT_OP => ">>",
                    TokenType.SIGNED_RSHIFT_OP => ">>>",
                    TokenType.UNDEFINED_COALESCE_OP => "??",
                    TokenType.AND_OP => "&",
                    TokenType.XOR_OP => "^",
                    TokenType.OR_OP => "|",
                    TokenType.LOGAND_OP => "&&",
                    TokenType.LOGOR_OP => "||",
                    TokenType.COMPARE_EQ => "==",
                    TokenType.COMPARE_NE => "!=",
                    TokenType.COMPARE_LT => "<",
                    TokenType.COMPARE_LE => "<=",
                    TokenType.COMPARE_GT => ">",
                    TokenType.COMPARE_GE => ">=",
                    _ => "<bad operator>"
                };
            }

            return $"({left.PrettyPrint()} {GetOperatorString(OperatorToken.Type)} {right.PrettyPrint()})";
        }

        public override IEnumerable<Token> ToTokens()
        {
            foreach (Token t in left.ToTokens())
            {
                yield return t;
            }
            yield return OperatorToken;
            foreach (Token t in right.ToTokens())
            {
                yield return t;
            }
        }

        private int? TryCoalesceUndefined(Action<Exception> handler)
        {
            List<Exception>? leftExceptions = null;

            // the left side of an undefined coalescing operation is allowed to raise exactly UndefinedIdentifierException
            // we need to catch that, so don't forward all exceptions raised by left just yet

            int? leftValue = left.TryEvaluate(e => (leftExceptions ??= new List<Exception>()).Add(e), EvaluationPhase.Final);

            if (leftExceptions == null)
            {
                // left evaluated properly => result is left
                return leftValue;
            }
            else if (leftExceptions.All(e => e is IdentifierNode.UndefinedIdentifierException))
            {
                // left did not evalute due to undefined identifier => result is right
                return right.TryEvaluate(handler, EvaluationPhase.Final);
            }
            else
            {
                // left failed to evaluate for some other reason
                foreach (Exception e in leftExceptions.Where(e => e is not IdentifierNode.UndefinedIdentifierException))
                {
                    handler(e);
                }

                return null;
            }
        }

        public override int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            /* undefined-coalescing operator is special because
             * 1. it should only be evaluated at final evaluation.
             * 2. it is legal for its left operand to fail evaluation. */
            if (OperatorToken.Type == TokenType.UNDEFINED_COALESCE_OP)
            {
                // TODO: better exception types here?

                switch (evaluationPhase)
                {
                    case EvaluationPhase.Immediate:
                        handler(new Exception("Invalid use of '??'."));
                        return null;
                    case EvaluationPhase.Early:
                        /* NOTE: you'd think one could optimize this by reducing this if left can be evaluated early
                         * but that would allow simplifying expressions even in contexts where '??' makes no sense
                         * (for example: 'ORG SomePossiblyUndefinedLabel ?? SomeOtherLabel')
                         * I don't think that's desirable */
                        handler(new Exception("The value of a '??' expression cannot be resolved early."));
                        return null;
                    case EvaluationPhase.Final:
                        return TryCoalesceUndefined(handler);
                }
            }

            int? leftValue = left.TryEvaluate(handler, evaluationPhase);
            leftValue.IfJust(i => left = new NumberNode(left.MyLocation, i));

            int? rightValue = right.TryEvaluate(handler, evaluationPhase);
            rightValue.IfJust(i => right = new NumberNode(right.MyLocation, i));

            if (leftValue is int lhs && rightValue is int rhs)
            {
                return OperatorToken.Type switch
                {
                    TokenType.MUL_OP => lhs * rhs,
                    TokenType.DIV_OP => lhs / rhs,
                    TokenType.MOD_OP => lhs % rhs,
                    TokenType.ADD_OP => lhs + rhs,
                    TokenType.SUB_OP => lhs - rhs,
                    TokenType.LSHIFT_OP => lhs << rhs,
                    TokenType.RSHIFT_OP => (int)(((uint)lhs) >> rhs),
                    TokenType.SIGNED_RSHIFT_OP => lhs >> rhs,
                    TokenType.AND_OP => lhs & rhs,
                    TokenType.XOR_OP => lhs ^ rhs,
                    TokenType.OR_OP => lhs | rhs,
                    TokenType.LOGAND_OP => lhs != 0 ? rhs : 0,
                    TokenType.LOGOR_OP => lhs != 0 ? lhs : rhs,
                    TokenType.COMPARE_EQ => lhs == rhs ? 1 : 0,
                    TokenType.COMPARE_NE => lhs != rhs ? 1 : 0,
                    TokenType.COMPARE_LT => lhs < rhs ? 1 : 0,
                    TokenType.COMPARE_LE => lhs <= rhs ? 1 : 0,
                    TokenType.COMPARE_GE => lhs >= rhs ? 1 : 0,
                    TokenType.COMPARE_GT => lhs > rhs ? 1 : 0,
                    _ => null,
                };
            }

            return null;
        }
    }
}

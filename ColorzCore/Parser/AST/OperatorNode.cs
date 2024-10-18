using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorzCore.Parser.AST
{
    public class OperatorNode : AtomNodeKernel
    {
        public IAtomNode Left { get; private set; }
        public IAtomNode Right { get; private set; }
        public Token OperatorToken { get; }

        public override int Precedence { get; }
        public override Location MyLocation => OperatorToken.Location;

        public OperatorNode(IAtomNode l, Token op, IAtomNode r, int prec)
        {
            Left = l;
            Right = r;
            OperatorToken = op;
            Precedence = prec;
        }

        public string OperatorString => OperatorToken.Type switch
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

        public override string PrettyPrint()
        {
            return $"({Left.PrettyPrint()} {OperatorString} {Right.PrettyPrint()})";
        }

        private int? TryCoalesceUndefined(Action<Exception> handler)
        {
            List<Exception>? leftExceptions = null;

            // the left side of an undefined coalescing operation is allowed to raise exactly UndefinedIdentifierException
            // we need to catch that, so don't forward all exceptions raised by left just yet

            int? leftValue = Left.TryEvaluate(e => (leftExceptions ??= new List<Exception>()).Add(e), EvaluationPhase.Final);

            if (leftExceptions == null)
            {
                // left evaluated properly => result is left
                return leftValue;
            }
            else if (leftExceptions.All(e => e is IdentifierNode.UndefinedIdentifierException))
            {
                // left did not evalute due to undefined identifier => result is right
                return Right.TryEvaluate(handler, EvaluationPhase.Final);
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
             * 1. it should not be evaluated early.
             * 2. it is legal for its left operand to fail evaluation. */
            if (OperatorToken.Type == TokenType.UNDEFINED_COALESCE_OP)
            {
                // TODO: better exception types here?

                switch (evaluationPhase)
                {
                    case EvaluationPhase.Early:
                        /* NOTE: maybe one could optimize this by reducing this if left can be evaluated early? */
                        handler(new Exception("The value of a '??' expression cannot be resolved early."));
                        return null;

                    default:
                        return TryCoalesceUndefined(handler);
                }
            }

            int? leftValue = Left.TryEvaluate(handler, evaluationPhase);
            leftValue.IfJust(i => Left = new NumberNode(Left.MyLocation, i));

            int? rightValue = Right.TryEvaluate(handler, evaluationPhase);
            rightValue.IfJust(i => Right = new NumberNode(Right.MyLocation, i));

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

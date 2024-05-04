using System;
using System.Collections.Generic;
using System.Text;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Interpreter.Diagnostics
{
    public static class DiagnosticsHelpers
    {
        // Gets string representation of expression with emphasis on locations for which the predicate is true
        public static string GetEmphasizedExpression(IAtomNode node, Func<Location, bool> emphasisPredicate)
        {
            return new EmphasisExpressionPrinter(emphasisPredicate).PrintExpression(node);
        }

        /*
        // Print expression (unused)
        public static string PrettyPrintExpression(IAtomNode node)
        {
            return GetEmphasizedExpression(node, _ => false);
        }
        */

        // visits operators that aren't around parenthesises or brackets
        public static void VisitUnguardedOperators(IList<Token> tokens, Action<Token> action)
        {
            if (tokens.Count > 1)
            {
                int paren = 0;
                int bracket = 0;

                foreach (Token token in tokens)
                {
                    switch (token.Type)
                    {
                        case TokenType.OPEN_PAREN:
                            paren++;
                            break;

                        case TokenType.CLOSE_PAREN:
                            paren--;
                            break;

                        case TokenType.OPEN_BRACKET:
                            bracket++;
                            break;

                        case TokenType.CLOSE_BRACKET:
                            bracket--;
                            break;

                        default:
                            if (paren == 0 && bracket == 0 && AtomParser.IsInfixOperator(token))
                            {
                                action.Invoke(token);
                            }

                            break;
                    }
                }
            }
        }

        // helper for DoesOperationSpanMultipleMacrosUnintuitively
        private static IAtomNode GetLeftmostInnerNode(IAtomNode node)
        {
            if (node is OperatorNode operatorNode)
            {
                return GetLeftmostInnerNode(operatorNode.Left);
            }

            return node;
        }

        // helper for DoesOperationSpanMultipleMacrosUnintuitively
        private static IAtomNode GetRightmostInnerNode(IAtomNode node)
        {
            if (node is OperatorNode operatorNode)
            {
                return GetRightmostInnerNode(operatorNode.Right);
            }

            if (node is UnaryOperatorNode unaryOperatorNode)
            {
                return GetRightmostInnerNode(unaryOperatorNode.Inner);
            }

            return node;
        }

        // returns true if node spans multiple macros unintuitively
        public static bool DoesOperationSpanMultipleMacrosUnintuitively(OperatorNode operatorNode)
        {
            /* The condition for this diagnostic are as follows:
             * 1. The operator node is from the same macro expansion as the closest node on either side
             * 2. The operator node is not from the same macro expansion as the operator token on that same side */

            MacroLocation? macroLocation = operatorNode.MyLocation.macroLocation;

            IAtomNode left = operatorNode.Left;
            IAtomNode right = operatorNode.Right;

            if (left is OperatorNode leftNode)
            {
                if (macroLocation == GetRightmostInnerNode(left).MyLocation.macroLocation)
                {
                    if (macroLocation != leftNode.OperatorToken.Location.macroLocation)
                    {
                        return true;
                    }
                }
            }

            if (right is OperatorNode rightNode)
            {
                if (macroLocation == GetLeftmostInnerNode(right).MyLocation.macroLocation)
                {
                    if (macroLocation != rightNode.OperatorToken.Location.macroLocation)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string PrettyParamType(ParamType paramType) => paramType switch
        {
            ParamType.ATOM => "Atom",
            ParamType.LIST => "List",
            ParamType.STRING => "String",
            ParamType.MACRO => "Macro",
            _ => "<internal error: bad ParamType>",
        };

        // absolute as in "not relative to the value of a symbol"
        // NOTE: currently unused (I considered using this for SetSymbol detection)
        public static bool IsAbsoluteAtom(IAtomNode node) => node switch
        {
            IdentifierNode => false,

            OperatorNode operatorNode => operatorNode.OperatorToken.Type switch
            {
                // A + B is not absolute if either one is relative, but not both
                TokenType.ADD_OP => IsAbsoluteAtom(operatorNode.Left) == IsAbsoluteAtom(operatorNode.Right),

                // A - B is not absolute if A is relative and not B
                TokenType.SUB_OP => IsAbsoluteAtom(operatorNode.Left) || !IsAbsoluteAtom(operatorNode.Right),

                // A ?? B is not absolute if A and B aren't absolute
                TokenType.UNDEFINED_COALESCE_OP => IsAbsoluteAtom(operatorNode.Left) || IsAbsoluteAtom(operatorNode.Right),

                _ => true,
            },

            _ => true,
        };

        public static bool IsSubtractionOfCurrentOffset(IAtomNode node)
        {
            if (node is OperatorNode operatorNode && operatorNode.OperatorToken.Type == TokenType.SUB_OP)
            {
                // the "CURRENTOFFSET" node is a number node whose source token is an identifier
                if (operatorNode.Right is NumberNode numberNode)
                {
                    Token token = numberNode.SourceToken;
                    return token.Type == TokenType.IDENTIFIER && token.Content.ToUpperInvariant() == "CURRENTOFFSET";
                }
            }

            return false;
        }
    }
}

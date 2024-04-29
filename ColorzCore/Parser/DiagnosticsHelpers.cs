using System;
using System.Collections.Generic;
using System.Text;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;

namespace ColorzCore.Parser
{
    public static class DiagnosticsHelpers
    {
        // Helper class for printing expressions (IAtomNode) with some bits emphasized
        private class EmphasisExpressionPrinter : AtomVisitor
        {
            readonly StringBuilder stringBuilder = new StringBuilder();
            readonly StringBuilder underlineBuilder = new StringBuilder();

            readonly Func<Location, bool> emphasisPredicate;
            bool wasEmphasized = false;

            public EmphasisExpressionPrinter(Func<Location, bool> predicate)
            {
                emphasisPredicate = predicate;
                // targetMacroLocation = macroLocation;
            }

            public string PrintExpression(IAtomNode expression)
            {
                stringBuilder.Clear();
                underlineBuilder.Clear();

                Visit(expression);

                return $"{stringBuilder}\n{underlineBuilder}";
            }

            private void AppendString(bool strong, string value)
            {
                if (strong)
                {
                    stringBuilder.Append("\x1B[1;37m");
                    stringBuilder.Append(value);
                    stringBuilder.Append("\x1B[0m");
                }
                else
                {
                    stringBuilder.Append(value);
                }

                underlineBuilder.Append(strong ? '~' : ' ', value.Length);

                wasEmphasized = strong;
            }

            protected override void VisitNode(OperatorNode node)
            {
                AppendString(emphasisPredicate(node.Left.MyLocation), "(");

                Visit(node.Left);

                AppendString(emphasisPredicate(node.OperatorToken.Location), $" {node.OperatorString} ");

                Visit(node.Right);

                AppendString(wasEmphasized, ")");
            }

            protected override void VisitNode(UnaryOperatorNode node)
            {
                AppendString(emphasisPredicate(node.OperatorToken.Location), node.OperatorString);

                Visit(node.Inner);
            }

            protected override void VisitNode(IdentifierNode node)
            {
                AppendString(emphasisPredicate(node.MyLocation), node.GetIdentifier()!);
            }

            protected override void VisitNode(NumberNode node)
            {
                AppendString(emphasisPredicate(node.MyLocation), node.PrettyPrint());
            }
        }

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
    }
}

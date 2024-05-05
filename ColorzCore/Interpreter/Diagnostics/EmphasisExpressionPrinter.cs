using System;
using System.Runtime.InteropServices;
using System.Text;
using ColorzCore.DataTypes;
using ColorzCore.Parser.AST;

namespace ColorzCore.Interpreter.Diagnostics
{
    // Helper class for printing expressions (IAtomNode) with some bits emphasized
    public class EmphasisExpressionPrinter : AtomVisitor
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
            // HACK: on Windows, ANSI terminal escape codes don't (always?) work
            if (strong && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !EAOptions.MonochromeLog)
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
            AppendString(emphasisPredicate(node.MyLocation), node.IdentifierToken.Content);
        }

        protected override void VisitNode(NumberNode node)
        {
            AppendString(emphasisPredicate(node.MyLocation), node.PrettyPrint());
        }
    }
}

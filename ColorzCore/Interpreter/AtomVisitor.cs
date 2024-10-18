
using ColorzCore.Parser.AST;

namespace ColorzCore.Interpreter
{
    public abstract class AtomVisitor
    {
        protected void Visit(IAtomNode node)
        {
            switch (node)
            {
                case OperatorNode operatorNode:
                    VisitNode(operatorNode);
                    break;
                case UnaryOperatorNode unaryOperatorNode:
                    VisitNode(unaryOperatorNode);
                    break;
                case IdentifierNode identifierNode:
                    VisitNode(identifierNode);
                    break;
                case NumberNode numberNode:
                    VisitNode(numberNode);
                    break;
            }
        }

        protected abstract void VisitNode(UnaryOperatorNode node);
        protected abstract void VisitNode(IdentifierNode node);
        protected abstract void VisitNode(NumberNode node);
        protected abstract void VisitNode(OperatorNode node);
    }
}

using System.Collections.Generic;
using ColorzCore.Parser.AST;

namespace ColorzCore.Parser
{
    public class Closure
    {
        private Dictionary<string, int> Symbols { get; }
        private Dictionary<string, IAtomNode> NonComputedSymbols { get; }

        public Closure()
        {
            Symbols = new Dictionary<string, int>();
            NonComputedSymbols = new Dictionary<string, IAtomNode>();
        }
        public virtual bool HasLocalLabel(string label)
        {
            if (Symbols.ContainsKey(label))
                return true;

            IAtomNode node;

            if (!NonComputedSymbols.TryGetValue(label, out node))
                return false;

            return node.CanEvaluate();
        }
        public virtual int GetLabel(string label)
        {
            int value;

            if (Symbols.TryGetValue(label, out value))
                return value;
            
            // To allow for better performance, we only ever evaluate assigned symbols once

            IAtomNode node = NonComputedSymbols[label];
            NonComputedSymbols.Remove(label);

            value = node.Evaluate();
            Symbols[label] = value;

            return value;
        }
        public void AddLabel(string label, int value)
        {
            Symbols[label] = value;
        }
        public void AddSymbol(string label, IAtomNode node)
        {
            if (node.CanEvaluate())
                AddLabel(label, node.Evaluate());

            NonComputedSymbols[label] = node;
        }
    }
}
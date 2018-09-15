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
        public virtual bool HasLocalSymbol(string label)
        {
            return Symbols.ContainsKey(label) || NonComputedSymbols.ContainsKey(label);
        }
        public virtual bool HasLocalSymbolValue(string label)
        {
            if (Symbols.ContainsKey(label))
                return true;

            IAtomNode node;

            if (!NonComputedSymbols.TryGetValue(label, out node))
                return false;

            return node.CanEvaluate();
        }
        public virtual bool EvaluationRequiresName(string label, string other)
        {
            if (Symbols.ContainsKey(label))
                return true;

            IAtomNode node;

            if (!NonComputedSymbols.TryGetValue(label, out node))
                return false;

            return node.EvaluationRequiresName(other);
        }
        public virtual int GetSymbolValue(string label)
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
        public void AddSymbol(string label, int value)
        {
            Symbols[label] = value;
        }
        public void AddSymbol(string label, IAtomNode node)
        {
            NonComputedSymbols[label] = node.Simplify();
        }
    }
}
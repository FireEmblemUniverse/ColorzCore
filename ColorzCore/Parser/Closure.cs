using System;
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

        public virtual int GetSymbol(string label)
        {
            if (Symbols.TryGetValue(label, out int value))
            {
                return value;
            }

            // Try to evaluate assigned symbol

            IAtomNode node = NonComputedSymbols[label];
            NonComputedSymbols.Remove(label);

            return node.TryEvaluate(e =>
            {
                NonComputedSymbols.Add(label, node);
                throw new SymbolComputeException(label, node, e);
            })!.Value;
        }

        public void AddSymbol(string label, int value) => Symbols[label] = value;
        public void AddSymbol(string label, IAtomNode node) => NonComputedSymbols[label] = node.Simplify(e => { });

        public IEnumerable<KeyValuePair<string, int>> LocalSymbols()
        {
            foreach (KeyValuePair<string, int> label in Symbols)
            {
                yield return label;
            }
        }

        public class SymbolComputeException : Exception
        {
            public string SymbolName { get; }
            public IAtomNode Expression { get; }

            public SymbolComputeException(string name, IAtomNode node, Exception e)
                : base($"Couldn't evaluate value of symbol `{name}` ({e.Message})")
            {
                SymbolName = name;
                Expression = node;
            }
        }
    }
}
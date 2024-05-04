
using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Parser.AST;
using ColorzCore.Raws;

namespace ColorzCore.Parser
{
    public class ParseConsumerChain : List<IParseConsumer>, IParseConsumer
    {
        public void OnAlignStatement(Location location, IAtomNode alignNode, IAtomNode? offsetNode)
        {
            ForEach(pc => pc.OnAlignStatement(location, alignNode, offsetNode));
        }

        public void OnAssertStatement(Location location, IAtomNode node)
        {
            ForEach(pc => pc.OnAssertStatement(location, node));
        }

        public void OnCloseScope(Location location)
        {
            ForEach(pc => pc.OnCloseScope(location));
        }

        public void OnData(Location location, byte[] data)
        {
            ForEach(pc => pc.OnData(location, data));
        }

        public void OnFillStatement(Location location, IAtomNode amountNode, IAtomNode? valueNode)
        {
            ForEach(pc => pc.OnFillStatement(location, amountNode, valueNode));
        }

        public void OnLabel(Location location, string name)
        {
            ForEach(pc => pc.OnLabel(location, name));
        }

        public void OnOpenScope(Location location)
        {
            ForEach(pc => pc.OnOpenScope(location));
        }

        public void OnOrgStatement(Location location, IAtomNode offsetNode)
        {
            ForEach(pc => pc.OnOrgStatement(location, offsetNode));
        }

        public void OnPopStatement(Location location)
        {
            ForEach(pc => pc.OnPopStatement(location));
        }

        public void OnProtectStatement(Location location, IAtomNode beginAtom, IAtomNode? endAtom)
        {
            ForEach(pc => pc.OnProtectStatement(location, beginAtom, endAtom));
        }

        public void OnPushStatement(Location location)
        {
            ForEach(pc => pc.OnPushStatement(location));
        }

        public void OnRawStatement(Location location, Raw raw, IList<IParamNode> parameters)
        {
            ForEach(pc => pc.OnRawStatement(location, raw, parameters));
        }

        public void OnSymbolAssignment(Location location, string name, IAtomNode atom)
        {
            ForEach(pc => pc.OnSymbolAssignment(location, name, atom));
        }
    }
}
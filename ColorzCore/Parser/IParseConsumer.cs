
using ColorzCore.DataTypes;
using ColorzCore.Parser.AST;

namespace ColorzCore.Parser
{
    public interface IParseConsumer
    {
        void OnOpenScope(Location location);
        void OnCloseScope(Location location);
        void OnRawStatement(Location location, RawNode node);
        void OnOrgStatement(Location location, IAtomNode offsetNode);
        void OnPushStatement(Location location);
        void OnPopStatement(Location location);
        void OnAssertStatement(Location location, IAtomNode node);
        void OnProtectStatement(Location location, IAtomNode beginAtom, IAtomNode? endAtom);
        void OnAlignStatement(Location location, IAtomNode alignNode, IAtomNode? offsetNode);
        void OnFillStatement(Location location, IAtomNode amountNode, IAtomNode? valueNode);
        void OnSymbolAssignment(Location location, string name, IAtomNode atom);
        void OnLabel(Location location, string name);
        void OnPreprocessorData(Location location, ILineNode node);
    }
}

using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public interface IAtomNode : IParamNode
    {
        //TODO: Simplify() partial evaluation as much as is defined, to save on memory space.
        int Precedence { get; }
        string? GetIdentifier();
        IEnumerable<Token> ToTokens();
        int? TryEvaluate(TAction<Exception> handler); //Simplifies the AST as much as possible.
    }

    public static class AtomExtensions
    {
        public static int CoerceInt(this IAtomNode n)
        {
            return n.TryEvaluate(e => throw e)!.Value;
        }

        public static IAtomNode Simplify(this IAtomNode self, TAction<Exception> handler)
        {
            return self.TryEvaluate(handler).IfJust(intValue => FromInt(self.MyLocation, intValue), () => self);
        }

        public static IAtomNode FromInt(Location location, int intValue)
        {
            return new NumberNode(location, intValue);
        }
    }
}

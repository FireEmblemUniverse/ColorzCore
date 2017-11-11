using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class EmptyNode : StatementNode, ILineNode, IAtomNode
    {
        public EmptyNode() : base(new List<IParamNode>())
        { }

        public int Precedence => throw new NotImplementedException();

        public ParamType Type => throw new NotImplementedException();

        public override int Size => 0;

        public int Evaluate()
        {
            throw new NotImplementedException();
        }

        public byte[] ToBytes()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "Empty node.";
        }
        public string PrettyPrint()
        {
            return ToString();
        }
        
        public override string PrettyPrint(int indentation)
        {
            return new StringBuilder().Append(' ', indentation).Append(ToString()).ToString();
        }
        
        public Maybe<string> GetIdentifier()
        {
            return new Nothing<string>();
        }
        public IEnumerable<Token> ToTokens() { yield break; }
        public Location MyLocation { get { return new Location(); } }
    }
}

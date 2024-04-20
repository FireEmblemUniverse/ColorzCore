using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    public class StringNode : IParamNode
    {
        public static readonly Regex idRegex = new Regex("^([a-zA-Z_][a-zA-Z0-9_]*)$");
        public Token MyToken { get; }

        public Location MyLocation => MyToken.Location;
        public ParamType Type => ParamType.STRING;

        public string Value => MyToken.Content;

        public StringNode(Token value)
        {
            MyToken = value;
        }

        public IEnumerable<byte> ToBytes()
        {
            return Encoding.ASCII.GetBytes(ToString());
        }

        public override string ToString()
        {
            return Value;
        }

        public string PrettyPrint()
        {
            return $"\"{Value}\"";
        }

        public IEnumerable<Token> ToTokens() { yield return MyToken; }

        public bool IsValidIdentifier()
        {
            return idRegex.IsMatch(Value);
        }

        public IdentifierNode ToIdentifier(ImmutableStack<Closure> scope)
        {
            return new IdentifierNode(MyToken, scope);
        }

        public IAtomNode? AsAtom() => null;
        public IParamNode SimplifyExpressions(Action<Exception> handler, EvaluationPhase evaluationPhase) => this;
    }
}

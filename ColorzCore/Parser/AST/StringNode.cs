using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
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

        public Token SourceToken { get; }

        public Location MyLocation => SourceToken.Location;
        public ParamType Type => ParamType.STRING;

        public string Value => SourceToken.Content;

        public StringNode(Token value)
        {
            SourceToken = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public string PrettyPrint()
        {
            return $"\"{Value}\"";
        }

        public IEnumerable<Token> ToTokens() { yield return SourceToken; }

        public bool IsValidIdentifier()
        {
            return idRegex.IsMatch(Value);
        }

        public IAtomNode? AsAtom() => null;
        public IParamNode SimplifyExpressions(Action<Exception> handler, EvaluationPhase evaluationPhase) => this;
    }
}

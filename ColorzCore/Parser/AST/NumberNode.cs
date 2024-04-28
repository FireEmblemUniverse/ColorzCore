using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;

namespace ColorzCore.Parser.AST
{
    public class NumberNode : AtomNodeKernel
    {
        public int Value { get; }

        public override Location MyLocation { get; }
        public override int Precedence { get { return 11; } }

        public NumberNode(Token num)
        {
            MyLocation = num.Location;
            Value = num.Content.ToInt();
        }
        public NumberNode(Token text, int value)
        {
            MyLocation = text.Location;
            Value = value;
        }
        public NumberNode(Location loc, int value)
        {
            MyLocation = loc;
            Value = value;
        }

        public override IEnumerable<Token> ToTokens()
        {
            yield return new Token(TokenType.NUMBER, MyLocation, Value.ToString());
        }

        public override int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            return Value;
        }

        public override string PrettyPrint() => Value >= 16 ? $"0x{Value:X}" : $"{Value}";
    }
}

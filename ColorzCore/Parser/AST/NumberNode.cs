using ColorzCore.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;

namespace ColorzCore.Parser.AST
{
    public class NumberNode : AtomNodeKernel
    {
        public Token SourceToken { get; }
        public int Value { get; }

        public override Location MyLocation => SourceToken.Location;
        public override int Precedence => int.MaxValue;

        public NumberNode(Token num)
        {
            SourceToken = num;
            Value = num.Content.ToInt();
        }

        public NumberNode(Token text, int value)
        {
            SourceToken = text;
            Value = value;
        }

        public NumberNode(Location loc, int value)
        {
            SourceToken = new Token(TokenType.NUMBER, loc, value.ToString());
            Value = value;
        }

        public override int? TryEvaluate(Action<Exception> handler, EvaluationPhase evaluationPhase)
        {
            return Value;
        }

        public override string PrettyPrint() => Value >= 16 ? $"0x{Value:X}" : $"{Value}";
    }
}

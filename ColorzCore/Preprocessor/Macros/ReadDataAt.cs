using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Macros
{
    public class ReadDataAt : BuiltInMacro
    {
        private readonly EAParser parser;
        private readonly ROM rom;
        private readonly int readLength;

        public ReadDataAt(EAParser parser, ROM rom, int readLength)
        {
            this.parser = parser;
            this.rom = rom;
            this.readLength = readLength;
        }

        public override IEnumerable<Token> ApplyMacro(Token head, IList<IList<Token>> parameters)
        {
            // HACK: hack
            MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(
                Enumerable.Repeat(new Token(TokenType.NEWLINE, head.Location, "\n"), 1));
            tokens.PrependEnumerator(parameters[0].GetEnumerator());

            IAtomNode? atom = parser.ParseAtom(tokens);

            if (tokens.Current.Type != TokenType.NEWLINE)
            {
                parser.Logger.Error(head.Location, "Garbage at the end of macro parameter.");
                yield return new Token(TokenType.NUMBER, head.Location, "0");
            }
            else if (atom?.TryEvaluate(e => parser.Logger.Error(atom.MyLocation, e.Message), EvaluationPhase.Immediate) is int offset)
            {
                offset = EAInterpreter.ConvertToOffset(offset);

                if (offset >= 0 && offset <= EAOptions.MaximumBinarySize - readLength)
                {
                    int data = 0;

                    // little endian!!!
                    for (int i = 0; i < readLength; i++)
                    {
                        data |= rom[offset + i] << (i * 8);
                    }

                    yield return new Token(TokenType.NUMBER, head.Location, $"0x{data:X}");
                }
                else
                {
                    parser.Logger.Error(head.Location, $"Read offset out of bounds: {offset:08X}");
                    yield return new Token(TokenType.NUMBER, head.Location, "0");
                }
            }
            else
            {
                parser.Logger.Error(head.Location, "Could not read data from base binary.");
                yield return new Token(TokenType.NUMBER, head.Location, "0");
            }
        }

        public override bool ValidNumParams(int num)
        {
            return num == 1;
        }
    }
}

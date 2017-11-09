using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ColorzCore.Lexer
{
    static class Tokenizer
    {
        public const int MAX_ID_LENGTH = 64;
        private static readonly Regex numRegex = new Regex("[01]+b|0x[\\da-fA-F]+|\\$[\\da-fA-F]+|\\d+");
        private static readonly Regex idRegex = new Regex("\\G([a-zA-Z_][a-zA-Z0-9_]*)");
        private static readonly Regex stringRegex = new Regex("\\G(([^\\\\\\\"]|\\\\[rnt\\\\\\\"])*)");

        //private static readonly IDictionary<TokenType, Regex> parseRegex = initializeDictionary();
        public static IEnumerable<Token> TokenizePhrase(string line, int lineNum, int startOffs, int endOffs)
        {

            int curCol = startOffs;
            while (curCol < endOffs)
            {
                char nextChar = line[curCol];
                if (Char.IsWhiteSpace(nextChar))
                {
                    curCol++;
                    continue;
                }
                switch (nextChar)
                {
                    case ';':
                        yield return new Token(TokenType.SEMICOLON, lineNum, curCol);
                        break;
                    case ':':
                        yield return new Token(TokenType.COLON, lineNum, curCol);
                        break;
                    case '#':
                        yield return new Token(TokenType.HASH, lineNum, curCol);
                        break;
                    case '{':
                        yield return new Token(TokenType.OPEN_BRACE, lineNum, curCol);
                        break;
                    case '}':
                        yield return new Token(TokenType.CLOSE_BRACE, lineNum, curCol);
                        break;
                    case '[':
                        yield return new Token(TokenType.OPEN_BRACKET, lineNum, curCol);
                        break;
                    case ']':
                        yield return new Token(TokenType.CLOSE_BRACKET, lineNum, curCol);
                        break;
                    case '(':
                        yield return new Token(TokenType.OPEN_PAREN, lineNum, curCol);
                        break;
                    case ')':
                        yield return new Token(TokenType.CLOSE_PAREN, lineNum, curCol);
                        break;
                    case '*':
                        yield return new Token(TokenType.MUL_OP, lineNum, curCol);
                        break;
                    case ',':
                        yield return new Token(TokenType.COMMA, lineNum, curCol);
                        break;
                    case '/':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '/')
                        {
                            //Is a comment, ignore rest of line
                            curCol = endOffs;
                        }
                        else
                        {
                            yield return new Token(TokenType.DIV_OP, lineNum, curCol);
                        }
                        break;
                    case '+':
                        yield return new Token(TokenType.ADD_OP, lineNum, curCol);
                        break;
                    case '-':
                        yield return new Token(TokenType.SUB_OP, lineNum, curCol);
                        break;
                    case '&':
                        yield return new Token(TokenType.AND_OP, lineNum, curCol);
                        break;
                    case '^':
                        yield return new Token(TokenType.XOR_OP, lineNum, curCol);
                        break;
                    case '|':
                        yield return new Token(TokenType.OR_OP, lineNum, curCol);
                        break;
                    case '\"':
                        {
                            curCol++;
                            Match quoteInterior = stringRegex.Match(line, curCol);
                            string match = quoteInterior.Value;
                            yield return new Token(TokenType.STRING, lineNum, curCol, match);
                            curCol += match.Length;
                            if (curCol == endOffs || line[curCol] != '\"')
                            {
                                yield return new Token(TokenType.ERROR, lineNum, curCol, "Unclosed string.");
                            }
                            continue;
                        }
                    case '<':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '<')
                        {
                            yield return new Token(TokenType.LSHIFT_OP, lineNum, curCol);
                            curCol += 2;
                            continue;
                        }
                        else
                        {
                            yield return new Token(TokenType.ERROR, lineNum, curCol, "<");
                            break;
                        }
                    case '>':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '>')
                        {
                            if (curCol + 2 < endOffs && line[curCol + 2] == '>')
                            {
                                yield return new Token(TokenType.SIGNED_RSHIFT_OP, lineNum, curCol);
                                curCol += 3;
                            }
                            else
                            {
                                yield return new Token(TokenType.RSHIFT_OP, lineNum, curCol);
                                curCol += 2;
                            }
                            continue;
                        }
                        else
                        {
                            yield return new Token(TokenType.ERROR, lineNum, curCol, ">");
                            break;
                        }
                    default:
                        //Try matching to identifier, then to number
                        Match idMatch = idRegex.Match(line, curCol, Math.Min(MAX_ID_LENGTH, endOffs - curCol));
                        if (idMatch.Success)
                        {
                            string match = idMatch.Value;
                            yield return new Token(TokenType.IDENTIFIER, lineNum, curCol, match);
                            curCol += match.Length;
                            if (curCol < endOffs && (Char.IsLetterOrDigit(line[curCol]) | line[curCol] == '_'))
                            {
                                Match idMatch2 = new Regex("[a-zA-Z0-9_]+").Match(line, curCol);
                                match = idMatch2.Value;
                                yield return new Token(TokenType.ERROR, lineNum, curCol, "Identifier longer than 64 characters.");
                                curCol += match.Length;
                            }
                            continue;
                        }
                        Match numMatch = numRegex.Match(line, curCol);
                        if (numMatch.Success)
                        {
                            string match = numMatch.Value;
                            yield return new Token(TokenType.NUMBER, lineNum, curCol, match);
                            curCol += match.Length;
                            continue;
                        }
                        string restOfWord = new Regex("\\S+").Match(line, curCol).Value;
                        yield return new Token(TokenType.ERROR, lineNum, curCol, restOfWord);
                        curCol += restOfWord.Length;
                        continue;
                }
                curCol++;
            }
        }
        public static IEnumerable<Token> TokenizeLine(string line, int lineNum)
        {
            return TokenizePhrase(line, lineNum, 0, line.Length);
        }
        public static IEnumerable<Token> Tokenize(BufferedStream input)
        {

            StreamReader sr = new StreamReader(input);
            int curLine = 1;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                foreach (Token t in TokenizeLine(line, curLine))
                {
                    yield return t;
                }
                if (!sr.EndOfStream)
                {
                    yield return new Token(TokenType.NEWLINE, curLine, line.Length);
                    curLine++;
                }
            }
        }
    }
}

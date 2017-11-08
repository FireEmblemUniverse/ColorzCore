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

        public static IEnumerable<Token> Tokenize(BufferedStream input)
        {

            StreamReader sr = new StreamReader(input);
            int nextChar, curLine = 1, curCol = 1;
            string nextLine;
            while (!sr.EndOfStream)
            {
                nextLine = sr.ReadLine();
                curCol = 0;
                while(curCol < nextLine.Length)
                {
                    nextChar = nextLine[curCol];
                    if (Char.IsWhiteSpace((char)nextChar))
                    {
                        curCol++;
                        continue;
                    }
                    switch (nextChar)
                    {
                        case ';':
                            yield return new Token(TokenType.SEMICOLON, curLine, curCol);
                            break;
                        case ':':
                            yield return new Token(TokenType.COLON, curLine, curCol);
                            break;
                        case '#':
                            yield return new Token(TokenType.HASH, curLine, curCol);
                            break;
                        case '{':
                            yield return new Token(TokenType.OPEN_BRACE, curLine, curCol);
                            break;
                        case '}':
                            yield return new Token(TokenType.CLOSE_BRACE, curLine, curCol);
                            break;
                        case '[':
                            yield return new Token(TokenType.OPEN_BRACKET, curLine, curCol);
                            break;
                        case ']':
                            yield return new Token(TokenType.CLOSE_BRACKET, curLine, curCol);
                            break;
                        case '(':
                            yield return new Token(TokenType.OPEN_PAREN, curLine, curCol);
                            break;
                        case ')':
                            yield return new Token(TokenType.CLOSE_PAREN, curLine, curCol);
                            break;
                        case '*':
                            yield return new Token(TokenType.MUL_OP, curLine, curCol);
                            break;
                        case ',':
                            yield return new Token(TokenType.COMMA, curLine, curCol);
                            break;
                        case '/':
                            if(curCol + 1 < nextLine.Length && nextLine[curCol+1] == '/')
                            {
                                //Is a comment, ignore rest of line
                                curCol = nextLine.Length;
                            }
                            else
                            {
                                yield return new Token(TokenType.DIV_OP, curLine, curCol);
                            }
                            break;
                        case '+':
                            yield return new Token(TokenType.ADD_OP, curLine, curCol);
                            break;
                        case '-':
                            yield return new Token(TokenType.SUB_OP, curLine, curCol);
                            break;
                        case '&':
                            yield return new Token(TokenType.AND_OP, curLine, curCol);
                            break;
                        case '^':
                            yield return new Token(TokenType.XOR_OP, curLine, curCol);
                            break;
                        case '|':
                            yield return new Token(TokenType.OR_OP, curLine, curCol);
                            break;
                        case '\"':
                            {
                                curCol++;
                                Match quoteInterior = stringRegex.Match(nextLine, curCol);
                                string match = quoteInterior.Value;
                                yield return new Token(TokenType.STRING, curLine, curCol, match);
                                curCol += match.Length;
                                if (curCol == nextLine.Length || nextLine[curCol] != '\"')
                                {
                                    yield return new Token(TokenType.ERROR, curLine, curCol, "Unclosed string.");
                                }
                                continue;
                            }
                        case '<':
                            if (curCol + 1 < nextLine.Length && nextLine[curCol + 1] == '<')
                            {
                                yield return new Token(TokenType.LSHIFT_OP, curLine, curCol);
                                curCol += 2;
                                continue;
                            }
                            else
                            {
                                yield return new Token(TokenType.ERROR, curLine, curCol, "<");
                                break;
                            }
                        case '>':
                            if (curCol + 1 < nextLine.Length && nextLine[curCol + 1] == '>')
                            {
                                if(curCol + 2 < nextLine.Length && nextLine[curCol + 2] == '>')
                                {
                                    yield return new Token(TokenType.SIGNED_RSHIFT_OP, curLine, curCol);
                                    curCol += 3;
                                }
                                else
                                {
                                    yield return new Token(TokenType.RSHIFT_OP, curLine, curCol);
                                    curCol += 2;
                                }
                                continue;
                            }
                            else
                            {
                                yield return new Token(TokenType.ERROR, curLine, curCol, ">");
                                break;
                            }
                        default:
                            //Try matching to identifier, then to number
                            Match idMatch = idRegex.Match(nextLine, curCol, Math.Min(MAX_ID_LENGTH, nextLine.Length - curCol));
                            if(idMatch.Success)
                            {
                                string match = idMatch.Value;
                                yield return new Token(TokenType.IDENTIFIER, curLine, curCol, match);
                                curCol += match.Length;
                                if(curCol < nextLine.Length && (Char.IsLetterOrDigit(nextLine[curCol]) | nextLine[curCol] == '_'))
                                {
                                    Match idMatch2 = new Regex("[a-zA-Z0-9_]+").Match(nextLine, curCol);
                                    match = idMatch2.Value;
                                    yield return new Token(TokenType.ERROR, curLine, curCol, "Identifier longer than 64 characters.");
                                    curCol += match.Length;
                                }
                                continue;
                            }
                            Match numMatch = numRegex.Match(nextLine, curCol);
                            if(numMatch.Success)
                            {
                                string match = numMatch.Value;
                                yield return new Token(TokenType.NUMBER, curLine, curCol, match);
                                curCol += match.Length;
                                continue;
                            }
                            string restOfWord = new Regex("\\S+").Match(nextLine, curCol).Value;
                            yield return new Token(TokenType.ERROR, curLine, curCol, restOfWord);
                            curCol += restOfWord.Length;
                            continue;
                    }
                    curCol++;
                }
                if(!sr.EndOfStream)
                {
                    yield return new Token(TokenType.NEWLINE, curLine, curCol);
                    curLine++;
                }
            }
        }
    }
}

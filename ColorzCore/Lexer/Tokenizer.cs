using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.IO;

namespace ColorzCore.Lexer
{
    class Tokenizer
    {
        public const int MAX_ID_LENGTH = 64;
        public static readonly Regex numRegex = new Regex("\\G([01]+b|0x[\\da-fA-F]+|\\$[\\da-fA-F]+|\\d+)");
        public static readonly Regex idRegex = new Regex("\\G([a-zA-Z_][a-zA-Z0-9_]*)");
        public static readonly Regex stringRegex = new Regex(@"\G(([^\""]|\\\"")*)"); //"\\G(([^\\\\\\\"]|\\\\[rnt\\\\\\\"])*)");
        public static readonly Regex winPathnameRegex = new Regex(string.Format("\\G([^ \\{0}]|\\ |\\\\)+", Process(Path.GetInvalidPathChars())));
        public static readonly Regex preprocDirectiveRegex = new Regex("\\G(#[a-zA-Z_][a-zA-Z0-9_]*)");
        public static readonly Regex wordRegex = new Regex("\\G([^\\s]+)");

        private static string Process(char[] chars)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in chars)
            {
                switch (c)
                {
                    case '.':
                    case '\\':
                    case '+':
                    case '*':
                    case '?':
                    case '^':
                    case '$':
                    case '[':
                    case ']':
                    case '{':
                    case '}':
                    case '(':
                    case ')':
                    case '|':
                    case '/':
                        sb.Append('\\');
                        break;
                    default:
                        break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private int multilineCommentNesting;

        public Tokenizer()
        {
            multilineCommentNesting = 0;
        }

        public IEnumerable<Token> TokenizePhrase(string line, int startOffs, int endOffs, Location location)
        {
            bool afterInclude = false, afterDirective = false, afterWhitespace = false;

            int curCol = startOffs;
            while (curCol < endOffs)
            {
                char nextChar = line[curCol];
                if (multilineCommentNesting > 0)
                {
                    if (nextChar == '*' && curCol + 1 < endOffs && line[curCol + 1] == '/')
                    {
                        multilineCommentNesting -= 1;
                        curCol += 2;
                        continue;
                    }
                    else if (nextChar == '/' && curCol + 1 < endOffs && line[curCol + 1] == '*')
                    {
                        multilineCommentNesting += 1;
                        curCol += 2;
                        continue;
                    }
                    else
                    {
                        curCol++;
                        continue;
                    }
                }
                if (char.IsWhiteSpace(nextChar) && nextChar != '\n')
                {
                    curCol++;
                    afterWhitespace = true;
                    continue;
                }

                switch (nextChar)
                {
                    case ';':
                        yield return new Token(TokenType.SEMICOLON, location.OffsetBy(curCol));
                        break;
                    case ':':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '=') // ':='
                        {
                            yield return new Token(TokenType.ASSIGN, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }

                        yield return new Token(TokenType.COLON, location.OffsetBy(curCol));
                        break;
                    case '{':
                        yield return new Token(TokenType.OPEN_BRACE, location.OffsetBy(curCol));
                        break;
                    case '}':
                        yield return new Token(TokenType.CLOSE_BRACE, location.OffsetBy(curCol));
                        break;
                    case '[':
                        yield return new Token(TokenType.OPEN_BRACKET, location.OffsetBy(curCol));
                        break;
                    case ']':
                        yield return new Token(TokenType.CLOSE_BRACKET, location.OffsetBy(curCol));
                        break;
                    case '(':
                        yield return new Token(TokenType.OPEN_PAREN, location.OffsetBy(curCol));
                        break;
                    case ')':
                        yield return new Token(TokenType.CLOSE_PAREN, location.OffsetBy(curCol));
                        break;
                    case '*':
                        yield return new Token(TokenType.MUL_OP, location.OffsetBy(curCol));
                        break;
                    case '%':
                        yield return new Token(TokenType.MOD_OP, location.OffsetBy(curCol));
                        break;
                    case ',':
                        yield return new Token(TokenType.COMMA, location.OffsetBy(curCol));
                        break;
                    case '/':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '/')
                        {
                            //Is a comment, ignore rest of line
                            curCol = endOffs;
                        }
                        else if (curCol + 1 < endOffs && line[curCol + 1] == '*')
                        {
                            multilineCommentNesting += 1;
                            curCol += 2;
                            continue;
                        }
                        else
                        {
                            yield return new Token(TokenType.DIV_OP, location.OffsetBy(curCol));
                        }
                        break;
                    case '+':
                        yield return new Token(TokenType.ADD_OP, location.OffsetBy(curCol));
                        break;
                    case '-':
                        if (afterWhitespace && afterDirective)
                        {
                            Match wsDelimited = wordRegex.Match(line, curCol, Math.Min(260, endOffs - curCol));
                            if (wsDelimited.Success)
                            {
                                string match = wsDelimited.Value;
                                yield return new Token(TokenType.STRING, location.OffsetBy(curCol), IOUtility.UnescapePath(match));
                                curCol += match.Length;
                                continue;
                            }
                        }
                        yield return new Token(TokenType.SUB_OP, location.OffsetBy(curCol));
                        break;
                    case '&':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '&')
                        {
                            yield return new Token(TokenType.LOGAND_OP, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }

                        yield return new Token(TokenType.AND_OP, location.OffsetBy(curCol));
                        break;
                    case '^':
                        yield return new Token(TokenType.XOR_OP, location.OffsetBy(curCol));
                        break;
                    case '|':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '|')
                        {
                            yield return new Token(TokenType.LOGOR_OP, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }

                        yield return new Token(TokenType.OR_OP, location.OffsetBy(curCol));
                        break;
                    case '\"':
                        {
                            curCol++;
                            Match quoteInterior = stringRegex.Match(line, curCol, endOffs - curCol);
                            string match = quoteInterior.Value;
                            yield return new Token(TokenType.STRING, location.OffsetBy(curCol), /*IOUtility.UnescapeString(*/match/*)*/);
                            curCol += match.Length;
                            if (curCol == endOffs || line[curCol] != '\"')
                            {
                                yield return new Token(TokenType.ERROR, location.OffsetBy(curCol), "Unclosed string.");
                            }
                            break;
                        }
                    case '<':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '<')
                        {
                            yield return new Token(TokenType.LSHIFT_OP, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }
                        else if (curCol + 1 < endOffs && line[curCol + 1] == '=')
                        {
                            yield return new Token(TokenType.COMPARE_LE, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }
                        else
                        {
                            yield return new Token(TokenType.COMPARE_LT, location.OffsetBy(curCol));
                            break;
                        }
                    case '>':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '>')
                        {
                            if (curCol + 2 < endOffs && line[curCol + 2] == '>')
                            {
                                yield return new Token(TokenType.SIGNED_RSHIFT_OP, location.OffsetBy(curCol));
                                curCol += 2;
                            }
                            else
                            {
                                yield return new Token(TokenType.RSHIFT_OP, location.OffsetBy(curCol));
                                curCol++;
                            }
                            break;
                        }
                        else if (curCol + 1 < endOffs && line[curCol + 1] == '=')
                        {
                            yield return new Token(TokenType.COMPARE_GE, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }
                        else
                        {
                            yield return new Token(TokenType.COMPARE_GT, location.OffsetBy(curCol));
                            break;
                        }
                    case '=':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '=')
                        {
                            yield return new Token(TokenType.COMPARE_EQ, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }
                        else
                        {
                            yield return new Token(TokenType.ERROR, location.OffsetBy(curCol), "=");
                            break;
                        }
                    case '!':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '=')
                        {
                            yield return new Token(TokenType.COMPARE_NE, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }
                        else
                        {
                            yield return new Token(TokenType.LOGNOT_OP, location.OffsetBy(curCol));
                            break;
                        }
                    case '~':
                        yield return new Token(TokenType.NOT_OP, location.OffsetBy(curCol));
                        break;
                    case '?':
                        if (curCol + 1 < endOffs && line[curCol + 1] == '?')
                        {
                            yield return new Token(TokenType.UNDEFINED_COALESCE_OP, location.OffsetBy(curCol));
                            curCol++;
                            break;
                        }
                        else
                        {
                            yield return new Token(TokenType.ERROR, location.OffsetBy(curCol), "?");
                            break;
                        }
                    case '\n':
                        yield return new Token(TokenType.NEWLINE, location.OffsetBy(curCol));
                        break;
                    default:
                        if (afterInclude)
                        {
                            Match winPath = winPathnameRegex.Match(line, curCol, Math.Min(260, endOffs - curCol));
                            if (winPath.Success)
                            {
                                string match = winPath.Value;
                                yield return new Token(TokenType.STRING, location.OffsetBy(curCol), IOUtility.UnescapePath(match));
                                curCol += match.Length;
                                afterInclude = false;
                                continue;
                            }
                        }
                        else
                        {
                            //Try matching to identifier, then to number
                            //TODO: Restrict Macro invocations to a MAYBE_MACRO that must preceed a (, with no whitespace.
                            Match idMatch = idRegex.Match(line, curCol, Math.Min(MAX_ID_LENGTH, endOffs - curCol));
                            if (idMatch.Success)
                            {
                                string match = idMatch.Value;
                                int idCol = curCol;
                                curCol += match.Length;
                                if (curCol < endOffs && line[curCol] == '(')
                                    yield return new Token(TokenType.MAYBE_MACRO, location.OffsetBy(idCol), match);
                                else
                                    yield return new Token(TokenType.IDENTIFIER, location.OffsetBy(idCol), match);
                                if (curCol < endOffs && (char.IsLetterOrDigit(line[curCol]) | line[curCol] == '_'))
                                {
                                    Match idMatch2 = new Regex("[a-zA-Z0-9_]+").Match(line, curCol, endOffs - curCol);
                                    match = idMatch2.Value;
                                    yield return new Token(TokenType.ERROR, location.OffsetBy(curCol), $"Identifier longer than {MAX_ID_LENGTH} characters.");
                                    curCol += match.Length;
                                }
                                continue;
                            }
                            Match numMatch = numRegex.Match(line, curCol, endOffs - curCol);
                            if (numMatch.Success)
                            {
                                string match = numMatch.Value;
                                //Verify that next token isn't start of an identifier
                                if (curCol + match.Length >= endOffs || (!char.IsLetter(line[curCol + match.Length]) && line[curCol + match.Length] != '_'))
                                {
                                    yield return new Token(TokenType.NUMBER, location.OffsetBy(curCol), match.TrimEnd());
                                    curCol += match.Length;
                                    continue;
                                }
                            }
                            Match directiveMatch = preprocDirectiveRegex.Match(line, curCol, Math.Min(MAX_ID_LENGTH + 1, endOffs - curCol));
                            if (directiveMatch.Success)
                            {
                                string match = directiveMatch.Value;
                                yield return new Token(TokenType.PREPROCESSOR_DIRECTIVE, location.OffsetBy(curCol), match);
                                curCol += match.Length;
                                if (match.Substring(1).Equals("include") || match.Substring(1).Equals("incbin"))
                                {
                                    afterInclude = true;
                                }
                                afterDirective = true;
                                continue;
                            }
                        }
                        string restOfWord = new Regex("\\G\\S+").Match(line, curCol, endOffs - curCol).Value;
                        yield return new Token(TokenType.ERROR, location.OffsetBy(curCol), restOfWord);
                        curCol += restOfWord.Length;
                        continue;
                }
                curCol++;
                afterInclude = false;
                afterWhitespace = false;
            }
        }

        public IEnumerable<Token> TokenizePhrase(string line, Location location)
        {
            return TokenizePhrase(line, 0, line.Length, location);
        }

        public static IEnumerable<Token> TokenizeLine(string line, Location location)
        {
            return new Tokenizer().TokenizePhrase(line, 0, line.Length, location);
        }

        /***
         *   All Token streams end in a NEWLINE.
         * 
         */
        public IEnumerable<Token> Tokenize(Stream input, string fileName)
        {
            StreamReader sin = new StreamReader(input);
            int curLine = 1;
            while (!sin.EndOfStream)
            {
                string line = sin.ReadLine()!;

                //allow escaping newlines
                while (line.Length > 0 && line[line.Length - 1] == '\\')
                {
                    curLine++;
                    line = line.Substring(0, line.Length - 1) + " " + sin.ReadLine();
                }

                Location location = new Location(fileName, curLine, 1);
                foreach (Token t in TokenizePhrase(line, location))
                {
                    yield return t;
                }
                yield return new Token(TokenType.NEWLINE, location.OffsetBy(line.Length));
                curLine++;
            }
        }

        public IEnumerable<Token> TokenizeFile(FileStream fs, string filename)
        {
            foreach (Token t in Tokenize(fs, filename))
                yield return t;
            fs.Close();
        }

        public IEnumerable<Token> Tokenize(FileStream fs)
        {
            return TokenizeFile(fs, fs.Name);
        }
    }
}

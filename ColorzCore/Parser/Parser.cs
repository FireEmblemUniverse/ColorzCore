using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser
{
    class Parser
    {
        public Dictionary<string, Macro> Definitions { get; set; }
        public string File { get; private set; }
        public Closure MyClosure { get; set; }
        private Stack<int> pastOffsets;

        public Parser(string includedBy = "")
        {
            MyClosure = new Closure(includedBy);
            pastOffsets = new Stack<int>();
        }

        public IASTNode ParseNextLine(IEnumerator<Token> tokens, int startOffset, Closure context)
        {
            while (tokens.Current.Type == TokenType.NEWLINE)
            {
                if (!tokens.MoveNext())
                    return new EOSNode(File, tokens.Current.LineNumber, tokens.Current.ColumnNumber);
            }
            Token nextToken = tokens.Current;
            switch (nextToken.Type)
            {
                case TokenType.IDENTIFIER:
                    Token identifier = nextToken;
                    tokens.MoveNext();
                    switch(tokens.Current.Type)
                    {
                        case TokenType.OPEN_PAREN:
                            return ParseNextLine(ExpandMacro(identifier, tokens), startOffset, MyClosure);
                        default:
                            return ParseRaw(identifier, tokens);
                    }
                case TokenType.OPEN_BRACE:
                    return ParseBlock(tokens);
                case TokenType.OPEN_BRACKET:
                    ParseList(tokens);
                    return new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, "Unexpected list literal.");
                case TokenType.NUMBER:
                case TokenType.OPEN_PAREN:
                    ParseMathExpression(tokens);
                    return new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, "Unexpected mathematical expression.");
                default:
                    tokens.MoveNext();
                    return new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, String.Format("Unexpected token: {0}", nextToken.Type));
            }
        }

        private IASTNode ParseBlock(IEnumerator<Token> tokens)
        {
            throw new NotImplementedException();
        }

        private void ParseList(IEnumerator<Token> tokens)
        {
            throw new NotImplementedException();
        }

        private void ParseMathExpression(IEnumerator<Token> tokens)
        {
            throw new NotImplementedException();
        }
    }
}

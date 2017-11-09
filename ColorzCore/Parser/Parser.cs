using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor;
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
        public Closure GlobalClosure { get; set; }
        private Stack<int> pastOffsets;

        public Parser(string includedBy = "")
        {
            GlobalClosure = new Closure(includedBy);
            pastOffsets = new Stack<int>();
        }

        public IEnumerator<Either<DataBlock,ErrorNode>> ParseAll(IEnumerator<Token> tokens, int startOffset, Closure context)
        {
            Stack<Closure> scopes = new Stack<Closure>();
            scopes.Push(GlobalClosure);
            while (tokens.Current.Type == TokenType.NEWLINE)
            {
                if (!tokens.MoveNext())
                    yield break;
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
                            return ParseNext(ExpandMacro(identifier, tokens), startOffset, MyClosure);
                        default:
                            return ParseRaw(identifier, tokens);
                    }
                case TokenType.OPEN_BRACE:
                    return ParseBlock(tokens);
                case TokenType.HASH:
                    tokens.MoveNext();
                    if(tokens.Current.Type != TokenType.IDENTIFIER)
                    {
                        return new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, "Expected preprocessor directive identifier after #.");
                    }
                    return Handler.HandleDirective(tokens);
                case TokenType.OPEN_BRACKET:
                    ParseList(tokens);
                    yield return new Either<DataBlock, ErrorNode>(new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, "Unexpected list literal."));
                    break;
                case TokenType.NUMBER:
                case TokenType.OPEN_PAREN:
                    ParseMathExpression(tokens);
                    yield return new Either<DataBlock, ErrorNode>(new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, "Unexpected mathematical expression."));
                    break;
                default:
                    tokens.MoveNext();
                    yield return new Either<DataBlock, ErrorNode>(new ErrorNode(File, nextToken.LineNumber, nextToken.ColumnNumber, String.Format("Unexpected token: {0}", nextToken.Type)));
                    break;
            }
        }
    }
}

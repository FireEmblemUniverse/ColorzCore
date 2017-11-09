using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor;
using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Raws;

namespace ColorzCore.Parser
{
    class Parser
    {
        public Dictionary<string, Macro> Definitions { get; set; }
        public Dictionary<string, Raw> Raws { get; set; }
        public string File { get; private set; }
        public Closure GlobalClosure { get; set; }
        int currentOffset;
        private Stack<int> pastOffsets;
        IList<string> Messages { get; }
        IList<string> Warnings { get; }
        IList<string> Errors { get; }

        public Parser(string includedBy = "")
        {
            GlobalClosure = new Closure(includedBy);
            pastOffsets = new Stack<int>();
        }
        public BlockNode ParseBlock(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            BlockNode temp = new BlockNode();
            while(tokens.Current.Type != TokenType.CLOSE_BRACE)
            {
                temp.Children.Add(ParseLine(tokens, scopes));
            }
            tokens.MoveNext();
            return temp;
        }
        public StatementNode ParseStatement(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            while(Definitions.ContainsKey(tokens.Current.Content))
            {
                ExpandMacro(tokens);
            }
            Token head = tokens.Current;
            tokens.MoveNext();
            return ParseRaw(head, tokens, scopes);
        }

        public StatementNode ParseRaw(Token raw, MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            StatementNode temp = new StatementNode();
            temp.Raw = raw;
            if (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON)
            {
                IList<IParamNode> parameters = ParseParams(tokens, scopes);
                temp.Parameters = parameters;
            }
            currentOffset += Raws[raw.Content].Length;
            return temp;
        }

        private IList<IParamNode> ParseParams(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            throw new NotImplementedException();
        }

        public ILineNode ParseLine(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            if (tokens.Current.Type == TokenType.NEWLINE)
            {
                tokens.MoveNext();
                return new EmptyNode();
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
                            ExpandMacro(identifier, tokens);
                            return ParseStatementList(tokens, currentOffset, scopes);
                        case TokenType.COLON:
                            tokens.MoveNext();
                            if(scopes.Head.Labels.ContainsKey(identifier.Content))
                            {
                                Log(Errors, File, identifier.LineNumber, identifier.ColumnNumber, "Label already in scope: " + identifier.Content);
                            }
                            else
                            {
                                scopes.Head.Labels.Add(identifier.Content, currentOffset);
                            }
                            if (tokens.Current.Type != TokenType.NEWLINE)
                            {
                                Log(Errors, File, tokens.Current.LineNumber, tokens.Current.ColumnNumber, "Unexpected token " + tokens.Current.Type);
                                IgnoreRestOfLine(tokens);
                            }
                            return new EmptyNode();
                        default:
                            return new StatementListNode(ParseRaw(identifier, tokens, scopes));
                    }
                case TokenType.OPEN_BRACE:
                    tokens.MoveNext();
                    return ParseBlock(tokens, new ImmutableStack<Closure>(new Closure(), scopes));
                case TokenType.HASH:
                    tokens.MoveNext();
                    if(tokens.Current.Type != TokenType.IDENTIFIER)
                    {
                        Log(Errors, File, nextToken.LineNumber, nextToken.ColumnNumber, "Expected preprocessor directive identifier after #.");
                    }
                    //return Handler.HandleDirective(tokens);
                    //List < IParamNode > = ParseParamList(tokens, scopes);
                    break;
                case TokenType.OPEN_BRACKET:
                    Log(Errors, File, nextToken.LineNumber, nextToken.ColumnNumber, "Unexpected list literal.");
                    IgnoreRestOfLine(tokens);
                    break;
                case TokenType.NUMBER:
                case TokenType.OPEN_PAREN:
                    Log(Errors, File, nextToken.LineNumber, nextToken.ColumnNumber, "Unexpected mathematical expression.");
                    IgnoreRestOfLine(tokens);
                    break;
                default:
                    tokens.MoveNext();
                    Log(Errors, File, nextToken.LineNumber, nextToken.ColumnNumber, String.Format("Unexpected token: {0}", nextToken.Type));
                    break;
            }
            IgnoreRestOfLine(tokens);
            return new EmptyNode();
        }

        private ILineNode ParseStatementList(MergeableGenerator<Token> tokens, int currentOffset, ImmutableStack<Closure> scopes)
        {
            throw new NotImplementedException();
        }

        public void ExpandMacro(Token macro, MergeableGenerator<Token> tokens)
        {
            IList<IList<Token>> parameters = new List<IList<Token>>();
            do
            {
                tokens.MoveNext();
                List<Token> currentParam = new List<Token>();
                while (tokens.Current.Type != TokenType.COMMA && tokens.Current.Type != TokenType.CLOSE_PAREN && tokens.Current.Type != TokenType.NEWLINE)
                {
                    currentParam.Add(tokens.Current);
                    tokens.MoveNext();
                }
            } while (tokens.Current.Type != TokenType.CLOSE_PAREN && tokens.Current.Type != TokenType.NEWLINE);
            if (tokens.Current.Type == TokenType.CLOSE_PAREN)
                tokens.PrependEnumerator(Definitions[macro.Content].ApplyMacro(parameters));
            else
            {
                Log(Errors, File, tokens.Current.LineNumber, tokens.Current.ColumnNumber, "Unmatched open parenthesis.");
            }
        }

        public void ExpandMacro(MergeableGenerator<Token> tokens)
        {
            Token macro = tokens.Current;
            tokens.MoveNext();
            ExpandMacro(macro, tokens);
        }

        private void Log(IList<string> record, string filename, int lineNum, int colNum, string message)
        {
            record.Add(String.Format("In File {0}, Line {1}, Column {2}: {3}", filename, lineNum, colNum, message));
        }
        private void IgnoreRestOfLine(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.MoveNext()) ;
            tokens.MoveNext();
        }
    }
}

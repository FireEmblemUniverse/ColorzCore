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
        public Dictionary<string, Dictionary<int, Macro>> Macros { get; set; }
        public Dictionary<string, Definition> Definitions { get; set; }
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
            Token head = tokens.Current;
            tokens.MoveNext();
            StatementNode temp = new StatementNode();
            //TODO: Replace with real raw information, and error if not valid.
            temp.Raw = head;
            if (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON)
            {
                IList<IParamNode> parameters = ParseParamList(tokens, scopes);
                temp.Parameters = parameters;
            }
            else
            {   
                temp.Parameters = new List<IParamNode>();
            }
            currentOffset += Raws[head.Content].Length;
            return temp;
        }

        private IList<IParamNode> ParseParamList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            IList<IParamNode> parameters = new List<IParamNode>();
            do
            {
                parameters.Add(ParseParam(tokens, scopes));
                if(tokens.Current.Type == TokenType.COMMA)
                    tokens.MoveNext();
            } while(tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_PAREN);
            tokens.MoveNext();
            return parameters;
        }
        
        IParamNode ParseParam(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Token head = tokens.Current;
            switch(tokens.Current.Type)
            {
                case TokenType.OPEN_BRACKET:
                    return new ListNode(ParseAtomList(tokens, scopes));
                case TokenType.STRING:
                    tokens.MoveNext();
                    return new StringNode(head);
                default:
                    return ParseAtom(tokens, scopes);
            }
        }
        
        private static readonly Dictionary<TokenType, int> precedences = new Dictionary<TokenType, int> {
            { TokenType.MUL_OP , 3 },
            { TokenType.DIV_OP , 3 },
            { TokenType.ADD_OP , 4 },
            { TokenType.SUB_OP , 4 },
            { TokenType.LSHIFT_OP , 5 },
            { TokenType.RSHIFT_OP , 5 },
            { TokenType.SIGNED_RSHIFT_OP , 5 },
            { TokenType.AND_OP , 8 },
            { TokenType.XOR_OP , 9 },
            { TokenType.OR_OP , 10 }        
        };
        
        
        
        IAtomNode ParseAtom(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            //Use Shift Reduce Parsing
            Token head = tokens.Current;
            Stack<Either<IAtomNode,Token>> grammarSymbols = new Stack<Either<IAtomNode,Token>>();
            bool ended = false;
            while(!ended)
            {
                bool shift = false;
                Token lookAhead = tokens.Current;
                
                if(grammarSymbols.Count == 0)
                {
                    //Shift
                    grammarSymbols.Push(new Either<IAtomNode,Token>(lookAhead));
                    tokens.MoveNext();
                    continue;
                }
                
                Either<IAtomNode,Token> top = grammarSymbols.Peek();

                if (!ended && top.IsLeft) //Is already a complete node. Needs an operator of matching precedence and a node of matching prec to reduce.
                {
                    IAtomNode node = top.GetLeft;
                    int treePrec = node.Precedence;
                    if (precedences.ContainsKey(lookAhead.Type) && precedences[lookAhead.Type] >= treePrec)
                    {
                        Reduce(grammarSymbols, precedences[lookAhead.Type]);
                    }
                    else
                    {
                        //Verify next symbol to be an operator.
                        switch (lookAhead.Type)
                        {
                            case TokenType.MUL_OP:
                            case TokenType.DIV_OP:
                            case TokenType.ADD_OP:
                            case TokenType.SUB_OP:
                            case TokenType.LSHIFT_OP:
                            case TokenType.RSHIFT_OP:
                            case TokenType.SIGNED_RSHIFT_OP:
                            case TokenType.AND_OP:
                            case TokenType.XOR_OP:
                            case TokenType.OR_OP:
                                shift = true;
                                break;
                            default:
                                ended = true;
                                break;
                        }
                    }
                }
                else if (!ended) //Is just an operator. Error if two operators in a row.
                {
                    //Error if two operators in a row.
                    switch (lookAhead.Type)
                    {
                        case TokenType.MUL_OP:
                        case TokenType.DIV_OP:
                        case TokenType.ADD_OP:
                        case TokenType.SUB_OP:
                        case TokenType.LSHIFT_OP:
                        case TokenType.RSHIFT_OP:
                        case TokenType.SIGNED_RSHIFT_OP:
                        case TokenType.AND_OP:
                        case TokenType.XOR_OP:
                        case TokenType.OR_OP:
                            //TODO: Log error
                            IgnoreRestOfStatement(tokens);
                            return new EmptyNode();
                        case TokenType.IDENTIFIER:
                            if (ExpandIdentifier(tokens))
                                continue;
                            else
                            {
                                //Modied shift -- Shift but stick it in a node.
                                grammarSymbols.Push(new Either<IAtomNode, Token>(new IdentifierNode(lookAhead, scopes)));
                                tokens.MoveNext();
                            }
                            break;
                        case TokenType.NUMBER:
                            grammarSymbols.Push(new Either<IAtomNode, Token>(new NumberNode(lookAhead)));
                            tokens.MoveNext();
                            break;
                        default:
                            //TODO: Unexpected token
                            break;
                    }
                }
                
                if(shift)
                {
                    grammarSymbols.Push(new Either<IAtomNode,Token>(lookAhead));
                    tokens.MoveNext();
                    continue;
                }
            }
            while(grammarSymbols.Count > 1)
            {
                Reduce(grammarSymbols, 1);
            }

            return grammarSymbols.Peek().GetLeft;
        }

        /***
         *   Precondition: grammarSymbols alternates between IAtomNodes, operator Tokens, .Count is odd
         *                 the precedences of the IAtomNodes is increasing.
         *   Postcondition: Either grammarSymbols.Count == 1, or everything in grammarSymbols will have precedence <= targetPrecedence.
         *
         */
        private void Reduce(Stack<Either<IAtomNode, Token>> grammarSymbols, int targetPrecedence)
        {
            while(grammarSymbols.Count > 1 || grammarSymbols.Peek().GetLeft.Precedence > targetPrecedence)
            {
                //These shouldn't error...
                IAtomNode r = grammarSymbols.Pop().GetLeft;
                Token op = grammarSymbols.Pop().GetRight;
                IAtomNode l = grammarSymbols.Pop().GetLeft;
                
                grammarSymbols.Push(new Either<IAtomNode, Token>(new OperatorNode(l, op, r, l.Precedence)));
            }
        }
        
        IList<IAtomNode> ParseAtomList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            tokens.MoveNext();
            IList<IAtomNode> atoms = new List<IAtomNode>();
            do
            {
                atoms.Add(ParseAtom(tokens, scopes));
                if(tokens.Current.Type == TokenType.COMMA)
                    tokens.MoveNext();
            } while(tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_PAREN);
            tokens.MoveNext();
            return atoms;
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
                    if (ExpandIdentifier(tokens))
                    {
                        return ParseLine(tokens, scopes);
                    }
                    if(Raws.ContainsKey(nextToken.Content))
                    {
                        return new StatementListNode(ParseStatementList(tokens, scopes));
                    }
                    else
                    {
                        tokens.MoveNext();
                        if(tokens.Current.Type == TokenType.COLON)
                        {
                            tokens.MoveNext();
                            if(scopes.Head.Labels.ContainsKey(nextToken.Content))
                            {
                                Log(Errors, nextToken.Location, "Label already in scope: " + nextToken.Content);
                            }
                            else
                            {
                                scopes.Head.Labels.Add(nextToken.Content, currentOffset);
                            }
                            
                            if (tokens.Current.Type != TokenType.NEWLINE)
                            {
                                Log(Errors, nextToken.Location, "Unexpected token " + tokens.Current.Type);
                                IgnoreRestOfLine(tokens);
                            }
                            return new EmptyNode();
                        }
                        else
                        {
                            Log(Errors, nextToken.Location, "Unrecognized raw code: " + nextToken.Content);
                            IgnoreRestOfStatement(tokens);
                            return new EmptyNode();
                        }
                    }
                case TokenType.OPEN_BRACE:
                    tokens.MoveNext();
                    return ParseBlock(tokens, new ImmutableStack<Closure>(new Closure(), scopes));
                case TokenType.HASH:
                    tokens.MoveNext();
                    if(tokens.Current.Type != TokenType.IDENTIFIER)
                    {
                        Log(Errors, nextToken.Location, "Expected preprocessor directive identifier after #.");
                    }
                    //return Handler.HandleDirective(tokens);
                    //List < IParamNode > = ParseParamList(tokens, scopes);
                    break;
                case TokenType.OPEN_BRACKET:
                    Log(Errors, nextToken.Location, "Unexpected list literal.");
                    break;
                case TokenType.NUMBER:
                case TokenType.OPEN_PAREN:
                    Log(Errors, nextToken.Location, "Unexpected mathematical expression.");
                    break;
                default:
                    tokens.MoveNext();
                    Log(Errors, nextToken.Location, String.Format("Unexpected token: {0}", nextToken.Type));
                    break;
            }
            IgnoreRestOfLine(tokens);
            return new EmptyNode();
        }

        private IList<StatementNode> ParseStatementList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            IList<StatementNode> stmts = new List<StatementNode>();
            do
            {
                stmts.Add(ParseStatement(tokens, scopes));
                if(tokens.Current.Type == TokenType.SEMICOLON)
                    tokens.MoveNext();
            } while(tokens.Current.Type != TokenType.NEWLINE);
            tokens.MoveNext();
            return stmts;
        }
        
        /***
         *   Precondition: tokens.Current.Type == TokenType.IDENTIFIER
         *   Postcondition: tokens.Current is fully reduced (i.e. not a macro, and not a definition)
         *   Returns: true iff tokens was actually expanded.
         */
        public bool ExpandIdentifier(MergeableGenerator<Token> tokens)
        {
            bool ret = false;
            //Macros and Definitions.
            if(Macros.ContainsKey(tokens.Current.Content))
            {
                ExpandMacro(tokens);
                ret = true;
                if(tokens.Current.Type == TokenType.IDENTIFIER)
                    ExpandIdentifier(tokens);
            }
            else if(Definitions.ContainsKey(tokens.Current.Content))
            {
                bool noteos = Definitions[tokens.Current.Content].ApplyDefinition(tokens);
                if(noteos && tokens.Current.Type == TokenType.IDENTIFIER)
                {
                    ExpandIdentifier(tokens);
                }
            }
            
            return ret;
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
                Log(Errors, tokens.Current.Location, "Unmatched open parenthesis.");
            }
        }

        public void ExpandMacro(MergeableGenerator<Token> tokens)
        {
            Token macro = tokens.Current;
            tokens.MoveNext();
            ExpandMacro(macro, tokens);
        }

        private void Log(IList<string> record, Location? causedError, string message)
        {
            if (causedError.HasValue)
                record.Add(String.Format("In File {0}, Line {1}, Column {2}: {3}", causedError.Value.file, causedError.Value.lineNum, causedError.Value.colNum, message));
            else
                record.Add(message);
        }
        private void IgnoreRestOfStatement(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON && tokens.MoveNext()) ;
            tokens.MoveNext();
        }
        private void IgnoreRestOfLine(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.MoveNext()) ;
            tokens.MoveNext();
        }
    }
}

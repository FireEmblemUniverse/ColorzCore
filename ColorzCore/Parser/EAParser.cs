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
    class EAParser
    {
        public Dictionary<string, Dictionary<int, Macro>> Macros { get; }
        public Dictionary<string, Definition> Definitions { get; }
        public Dictionary<string, Raw> Raws { get; }
        public static readonly HashSet<string> SpecialCodes = new HashSet<string> { "ORG", "PUSH", "POP", "MESSAGE", "WARNING", "ERROR", "ASSERT", "PROTECT" }; // TODO
        public ImmutableStack<Closure> GlobalScope { get; }
        //public Closure GlobalClosure { get; }
        

        private int currentOffset;
        private Stack<int> pastOffsets;

        public IList<string> Messages { get; }
        public IList<string> Warnings { get; }
        public IList<string> Errors { get; }

        public EAParser()
        {
            GlobalScope = new ImmutableStack<Closure>(new Closure(""), ImmutableStack<Closure>.Nil);
            pastOffsets = new Stack<int>();
            Messages = new List<string>();
            Warnings = new List<string>();
            Errors = new List<string>();
            currentOffset = 0;
            Macros = new Dictionary<string, Dictionary<int, Macro>>();
            Definitions = new Dictionary<string, Definition>();
            Raws = new Dictionary<string, Raw>();
        }

        public IEnumerable<ILineNode> ParseAll(IEnumerable<Token> tokenStream)
        {
            MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(tokenStream);
            while(!tokens.EOS)
            {
                yield return ParseLine(tokens, GlobalScope);
            }
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
            StatementNode temp = new StatementNode() { Raw = head };
            //TODO: Replace with real raw information, and error if not valid.
            if(SpecialCodes.Contains(head.Content.ToUpper()))
            {
                //TODO: Handle this case
                //return new SpecialActionNode(); ???
                return temp;
            }
            else if(Raws.ContainsKey(head.Content))
            {
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
            else
            {
                Log(Errors, head.Location, "Unrecognized code: " + head.Content);
                return temp; //TODO - Nullable?
            }
        }

        private IList<IParamNode> ParseParamList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            IList<IParamNode> parameters = new List<IParamNode>();
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_PAREN)
            {
                parameters.Add(ParseParam(tokens, scopes));
                if(tokens.Current.Type == TokenType.COMMA)
                    tokens.MoveNext();
            }
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
                    return new StringNode(head.Content);
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

                if (grammarSymbols.Count == 0)
                {
                    shift = true;
                }
                else
                {
                    Either<IAtomNode, Token> top = grammarSymbols.Peek();

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
                            case TokenType.NUMBER:
                                shift = true;
                                break;
                            default:
                                ended = true;
                                break;
                        }
                    }
                }
                
                if(shift)
                {
                    if(lookAhead.Type == TokenType.IDENTIFIER)
                    {
                        if (ExpandIdentifier(tokens))
                            continue;
                        grammarSymbols.Push(new Either<IAtomNode, Token>(new IdentifierNode(lookAhead, scopes)));
                    }
                    else if(lookAhead.Type == TokenType.NUMBER)
                    {
                        grammarSymbols.Push(new Either<IAtomNode, Token>(new NumberNode(lookAhead)));
                    }
                    else if(lookAhead.Type == TokenType.ERROR)
                    {
                        Log(Errors, lookAhead.Location, String.Format("Unexpected token: {0}", lookAhead.Content));
                        tokens.MoveNext();
                        return new EmptyNode();
                    }
                    else
                    {
                        grammarSymbols.Push(new Either<IAtomNode, Token>(lookAhead));
                    }
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
                            Log(Errors, nextToken.Location, "Unrecognized code: " + nextToken.Content);
                            IgnoreRestOfStatement(tokens);
                            return new EmptyNode();
                        }
                    }
                case TokenType.OPEN_BRACE:
                    tokens.MoveNext();
                    return ParseBlock(tokens, new ImmutableStack<Closure>(new Closure(scopes.Head.IncludedBy), scopes));
                case TokenType.PREPROCESSOR_DIRECTIVE:
                    Token directiveName = tokens.Current;
                    IList<IParamNode> paramList;
                    if (tokens.MoveNext())
                    {
                        paramList = ParseParamList(tokens, scopes);
                    }
                    else
                        paramList = new List<IParamNode>();
                    return Handler.HandleDirective(directiveName, paramList, tokens);
                case TokenType.OPEN_BRACKET:
                    Log(Errors, nextToken.Location, "Unexpected list literal.");
                    break;
                case TokenType.NUMBER:
                case TokenType.OPEN_PAREN:
                    Log(Errors, nextToken.Location, "Unexpected mathematical expression.");
                    break;
                default:
                    tokens.MoveNext();
                    Log(Errors, nextToken.Location, String.Format("Unexpected token: {0}: {1}", nextToken.Type, nextToken.Content));
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
            if(Macros[macro.Content].ContainsKey(parameters.Count) && tokens.Current.Type == TokenType.CLOSE_PAREN)
                tokens.PrependEnumerator(Macros[macro.Content][parameters.Count].ApplyMacro(parameters));
            else if (tokens.Current.Type != TokenType.CLOSE_PAREN)
            {
                Log(Errors, tokens.Current.Location, "Unmatched open parenthesis.");
            }
            else
            {
                Log(Errors, macro.Location, "Incorrect number of parameters: " + parameters.Count);
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

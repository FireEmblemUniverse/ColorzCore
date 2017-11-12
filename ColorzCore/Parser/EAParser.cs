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
using static ColorzCore.Preprocessor.Handler;
using System.IO;

//TODO: Use Maybe to cut down on the use of EmptyNode

namespace ColorzCore.Parser
{
    class EAParser
    {
        public Dictionary<string, Dictionary<int, Macro>> Macros { get; }
        public Dictionary<string, Definition> Definitions { get; }
        public Dictionary<string, Raw> Raws { get; }
        public static readonly HashSet<string> SpecialCodes = new HashSet<string> { "ORG", "PUSH", "POP", "MESSAGE", "WARNING", "ERROR", "ASSERT", "PROTECT" }; // TODO
        public ImmutableStack<Closure> GlobalScope { get; }
        public ImmutableStack<bool> Inclusion { get; set; }
        //public Closure GlobalClosure { get; }


        private int currentOffset;
        private Stack<int> pastOffsets;

        public IList<string> Messages { get; }
        public IList<string> Warnings { get; }
        public IList<string> Errors { get; }
        public bool IsIncluding { get
            {
                bool acc = true;
                for (ImmutableStack<bool> temp = Inclusion; !temp.IsEmpty && acc; temp = temp.Tail)
                    acc &= temp.Head;
                return acc;
            } }

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
            Inclusion = ImmutableStack<bool>.Nil;
        }

        public bool IsRawName(string name)
        {
            return Raws.ContainsKey(name) || SpecialCodes.Contains(name);
        }
        public bool IsValidDefinitionName(string name)
        {
            return !(Definitions.ContainsKey(name) || Raws.ContainsKey(name) || SpecialCodes.Contains(name));
        }
        public bool IsValidMacroName(string name, int paramNum)
        {
            return !(Macros.ContainsKey(name) && Macros[name].ContainsKey(paramNum)) && !IsRawName(name);
        }
        public IEnumerable<ILineNode> ParseAll(IEnumerable<Token> tokenStream)
        {
            MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(tokenStream);
            tokens.MoveNext();
            while (!tokens.EOS)
            {
                if (tokens.Current.Type != TokenType.NEWLINE || tokens.MoveNext())
                    yield return ParseLine(tokens, GlobalScope);
            }
        }

        private BlockNode ParseBlock(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Location start = tokens.Current.Location;
            tokens.MoveNext();
            BlockNode temp = new BlockNode();
            while (tokens.Current.Type != TokenType.CLOSE_BRACE && !tokens.EOS)
            {
                temp.Children.Add(ParseLine(tokens, scopes));
            }
            if (!tokens.EOS)
                tokens.MoveNext();
            else
                Log(Errors, start, "Unmatched brace.");
            return temp;
        }
        private StatementNode ParseStatement(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Token head = tokens.Current;
            tokens.MoveNext();
            //TODO: Replace with real raw information, and error if not valid.
            IList<IParamNode> parameters;
            //TODO: Make intelligent to reject malformed parameters.
            if (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON)
            {
                parameters = ParseParamList(tokens, scopes);
            }
            else
            {
                parameters = new List<IParamNode>();
            }

            if (SpecialCodes.Contains(head.Content.ToUpper()))
            {
                //TODO: Handle this case
                //return new SpecialActionNode(); ???
                return new RawNode(null, head, parameters);
            }
            else if (Raws.ContainsKey(head.Content))
            {
                StatementNode temp = new RawNode(Raws[head.Content], head, parameters);
                currentOffset += temp.Size;
                return temp;
            }
            else //TODO: Move outside of this else.
            {
                Log(Errors, head.Location, "Unrecognized code: " + head.Content);
                return new RawNode(null, head, parameters); //TODO - Return Empty later, but for now, return this to ensure correct AST generation
            }
        }

        public IList<IList<Token>> ParseMacroParamList(MergeableGenerator<Token> tokens)
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
                parameters.Add(currentParam);
            } while (tokens.Current.Type != TokenType.CLOSE_PAREN && tokens.Current.Type != TokenType.NEWLINE);
            if(tokens.Current.Type != TokenType.CLOSE_PAREN)
            {
                Log(Errors, tokens.Current.Location, "Unmatched open parenthesis.");
            }
            else
            {
                tokens.MoveNext();
            }
            return parameters;
        }

        private IList<IParamNode> ParseParamList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool expandMacros = true)
        {
            IList<IParamNode> paramList = new List<IParamNode>();
            while (tokens.Current.Type != TokenType.NEWLINE && !tokens.EOS)
            {
                paramList.Add(ParseParam(tokens, scopes, expandMacros));
            }
            return paramList;
        }

        private IParamNode ParseParam(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool expandMacros = true)
        {
            Token head = tokens.Current;
            switch (tokens.Current.Type)
            {
                case TokenType.OPEN_BRACKET:
                    return new ListNode(head.Location, ParseList(tokens, scopes));
                case TokenType.STRING:
                    tokens.MoveNext();
                    return new StringNode(head);
                case TokenType.IDENTIFIER:
                    if (!expandMacros)
                    {
                        if (tokens.MoveNext())
                        {
                            if (tokens.Current.Type == TokenType.OPEN_PAREN && !expandMacros)
                            {
                                return new MacroInvocationNode(this, head, ParseMacroParamList(tokens));
                            }
                            else
                            {
                                tokens.PutBack(head);
                                return ParseAtom(tokens, scopes);
                            }
                        }
                        else
                        {
                            return new IdentifierNode(head, scopes);
                        }
                    }
                    else
                    {
                        return ParseAtom(tokens, scopes);
                    }
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



        private IAtomNode ParseAtom(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            //Use Shift Reduce Parsing
            Token head = tokens.Current;
            Stack<Either<IAtomNode, Token>> grammarSymbols = new Stack<Either<IAtomNode, Token>>();
            bool ended = false;
            while (!ended)
            {
                bool shift = false, lookingForAtom = grammarSymbols.Count == 0 || grammarSymbols.Peek().IsRight;
                Token lookAhead = tokens.Current;

                if (!ended && !lookingForAtom) //Is already a complete node. Needs an operator of matching precedence and a node of matching prec to reduce.
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
                            IAtomNode node = grammarSymbols.Peek().GetLeft;
                            int treePrec = node.Precedence;
                            if (precedences.ContainsKey(lookAhead.Type) && precedences[lookAhead.Type] >= treePrec)
                            {
                                Reduce(grammarSymbols, precedences[lookAhead.Type]);
                            }
                            shift = true;
                            break;
                        default:
                            ended = true;
                            break;
                    }
                }
                else if (!ended) //Is just an operator. Error if two operators in a row.
                {
                    //Error if two operators in a row.
                    switch (lookAhead.Type)
                    {
                        case TokenType.IDENTIFIER:
                        case TokenType.NUMBER:
                            shift = true;
                            break;
                        case TokenType.OPEN_PAREN:
                            tokens.MoveNext();
                            grammarSymbols.Push(new Left<IAtomNode, Token>(new ParenthesizedAtomNode(lookAhead.Location, ParseAtom(tokens, scopes))));
                            if (tokens.Current.Type != TokenType.CLOSE_PAREN)
                            {
                                Log(Errors, tokens.Current.Location, "Unmatched open parenthesis (currently at " + tokens.Current.Type + ").");
                                return new EmptyNode();
                            }
                            tokens.MoveNext();
                            break;
                        case TokenType.COMMA:
                            Log(Errors, lookAhead.Location, "Unexpected comma (perhaps unrecognized macro invocation?).");
                            IgnoreRestOfStatement(tokens);
                            return new EmptyNode();
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
                        default:
                            Log(Errors, lookAhead.Location, "Expected identifier or literal, got " + lookAhead.Type + '.');
                            IgnoreRestOfStatement(tokens);
                            return new EmptyNode();
                    }
                }

                if (shift)
                {
                    if (lookAhead.Type == TokenType.IDENTIFIER)
                    {
                        if (ExpandIdentifier(tokens))
                            continue;
                        grammarSymbols.Push(new Left<IAtomNode, Token>(new IdentifierNode(lookAhead, scopes)));
                    }
                    else if (lookAhead.Type == TokenType.NUMBER)
                    {
                        grammarSymbols.Push(new Left<IAtomNode, Token>(new NumberNode(lookAhead)));
                    }
                    else if (lookAhead.Type == TokenType.ERROR)
                    {
                        Log(Errors, lookAhead.Location, String.Format("Unexpected token: {0}", lookAhead.Content));
                        tokens.MoveNext();
                        return new EmptyNode();
                    }
                    else
                    {
                        grammarSymbols.Push(new Right<IAtomNode, Token>(lookAhead));
                    }
                    tokens.MoveNext();
                    continue;
                }
            }
            while (grammarSymbols.Count > 1)
            {
                Reduce(grammarSymbols, 1);
            }
            if (grammarSymbols.Peek().IsRight)
            {
                Log(Errors, grammarSymbols.Peek().GetRight.Location, "Unexpected token: " + grammarSymbols.Peek().GetRight.Type);
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
            while (grammarSymbols.Count > 1 && grammarSymbols.Peek().GetLeft.Precedence > targetPrecedence)
            {
                //These shouldn't error...
                IAtomNode r = grammarSymbols.Pop().GetLeft;
                Token op = grammarSymbols.Pop().GetRight;
                IAtomNode l = grammarSymbols.Pop().GetLeft;

                grammarSymbols.Push(new Left<IAtomNode, Token>(new OperatorNode(l, op, r, l.Precedence)));
            }
        }

        private IList<IAtomNode> ParseList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            tokens.MoveNext();
            IList<IAtomNode> atoms = new List<IAtomNode>();
            do
            {
                atoms.Add(ParseAtom(tokens, scopes));
                if (tokens.Current.Type == TokenType.COMMA)
                    tokens.MoveNext();
            } while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_BRACKET);
            if (tokens.Current.Type == TokenType.CLOSE_BRACKET)
                tokens.MoveNext();
            return atoms;
        }

        public ILineNode ParseLine(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            if (IsIncluding)
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
                        else
                        {
                            tokens.MoveNext();
                            if (tokens.Current.Type == TokenType.COLON)
                            {
                                tokens.MoveNext();
                                if (scopes.Head.Labels.ContainsKey(nextToken.Content))
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
                                tokens.PutBack(nextToken);
                                return new StatementListNode(ParseStatementList(tokens, scopes));
                            }
                        }
                    case TokenType.OPEN_BRACE:
                        return ParseBlock(tokens, new ImmutableStack<Closure>(new Closure(scopes.Head.IncludedBy), scopes));
                    case TokenType.PREPROCESSOR_DIRECTIVE:
                        return ParsePreprocessor(tokens, scopes);
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
            else
            {
                bool hasNext = true;
                while (tokens.Current.Type != TokenType.PREPROCESSOR_DIRECTIVE && (hasNext = tokens.MoveNext())) ;
                if (hasNext)
                {
                    return ParsePreprocessor(tokens, scopes);
                }
                else
                {
                    Log(Errors, null, String.Format("Missing {0} endif(s).", Inclusion.Count));
                    return new EmptyNode();
                }
            }
        }

        private ILineNode ParsePreprocessor(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Token directiveName = tokens.Current;
            tokens.MoveNext();
            //Note: Not a ParseParamList because no commas.
            IList<IParamNode> paramList = ParseParamList(tokens, scopes, false);
            Maybe<ILineNode> retVal = HandleDirective(this, directiveName, paramList, tokens);
            if (retVal.IsNothing)
                return new EmptyNode();
            else
            {
                currentOffset += retVal.FromJust.Size;
                return retVal.FromJust;
            }
        }

        private IList<StatementNode> ParseStatementList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            IList<StatementNode> stmts = new List<StatementNode>();
            do
            {
                stmts.Add(ParseStatement(tokens, scopes));
                if (tokens.Current.Type == TokenType.SEMICOLON)
                    tokens.MoveNext();
            } while (tokens.Current.Type != TokenType.NEWLINE);
            return stmts;
        }

        /***
         *   Precondition: tokens.Current.Type == TokenType.IDENTIFIER
         *   Postcondition: tokens.Current is fully reduced (i.e. not a macro, and not a definition)
         *   Returns: true iff tokens was actually expanded.
         */
        public bool ExpandIdentifier(MergeableGenerator<Token> tokens, ImmutableStack<string> seenDefinitions = ImmutableStack<string>.Nil, ImmutableStack<Tuple<string, int>> seenMacros = ImmutableStack<Tuple<string,int>>.Nil)
        {
            bool ret = false;
            //Macros and Definitions.
            if (Macros.ContainsKey(tokens.Current.Content))
            {
                Token head = tokens.Current;
                tokens.MoveNext();
                IList<IList<Token>> parameters = ParseMacroParamList(tokens);
                if (tokens.Current.Type == TokenType.CLOSE_PAREN)
                {
                    if(Macros[head.Content].ContainsKey(parameters.Count) && !seenMacros.Contains(new Tuple<string, int>(head.Content, parameters.Count)))
                    {
                        tokens.PrependEnumerator(ExpandAll(new List<Token>(Macros[head.Content][parameters.Count].ApplyMacro(head, parameters)), seenDefinitions, new ImmutableStack<Tuple<string, int>>(new Tuple<string,int>(head.Content, parameters.Count), seenMacros)).GetEnumerator());
                    }
                    else
                    {
                        Error(head.Location, String.Format("No overload of {0} with {1} parameters, or recursive definition.", head.Content, parameters.Count));
                    }
                }
                else if (tokens.Current.Type != TokenType.CLOSE_PAREN)
                {
                    Error(tokens.Current.Location, "Unmatched open parenthesis.");
                }
                else
                {
                    Error(head.Location, "Incorrect number of parameters: " + parameters.Count);
                }
                ret = true;
                if (tokens.Current.Type == TokenType.IDENTIFIER)
                    ExpandIdentifier(tokens);
            }
            else if (Definitions.ContainsKey(tokens.Current.Content))
            {
                bool noteos = Definitions[tokens.Current.Content].ApplyDefinition(tokens);
                if (noteos && tokens.Current.Type == TokenType.IDENTIFIER)
                {
                    ExpandIdentifier(tokens);
                }
            }

            return ret;
        }
        
        //TODO: Interface these better so they're not mutually recursive making new MergeableGenerators each loop
        //Ideally, expand within the definitions themselves so that I only every have to substitute for the head.
        public IEnumerable<Token> ExpandAll(IList<Token> tokens, ImmutableStack<string> seenDefinitions, ImmutableStack<Tuple<string, int>> seenMacros)
        {
            MergeableGenerator<Token> myGen = new MergeableGenerator<Token>(tokens);
            while(!myGen.EOS)
            {
                while(ExpandIdentifier(myGen, seenDefinitions, seenMacros)) ;
                yield return myGen.Current;
                myGen.MoveNext();
            }
        }

        private static void Log(IList<string> record, Location? causedError, string message)
        {
            if (causedError.HasValue)
                record.Add(String.Format("In File {0}, Line {1}, Column {2}: {3}", Path.GetFileName(causedError.Value.file), causedError.Value.lineNum, causedError.Value.colNum, message));
            else
                record.Add(message);
        }
        public void Message(Location? loc, string message)
        {
            Log(Messages, loc, message);
        }
        public void Warning(Location? loc, string message)
        {
            Log(Warnings, loc, message);
        }
        public void Error(Location? loc, string message)
        {
            Log(Errors, loc, message);
        }
        private void IgnoreRestOfStatement(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON && tokens.MoveNext()) ;
            if (tokens.Current.Type == TokenType.SEMICOLON) tokens.MoveNext();
        }
        private void IgnoreRestOfLine(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.MoveNext()) ;
        }

        public void Clear()
        {
            Macros.Clear();
            Definitions.Clear();
            Raws.Clear();
            Inclusion = ImmutableStack<bool>.Nil;
            currentOffset = 0;
            pastOffsets.Clear();
            Messages.Clear();
            Warnings.Clear();
            Errors.Clear();
        }
    }
}

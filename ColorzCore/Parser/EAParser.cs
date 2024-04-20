using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using ColorzCore.Parser.Macros;
using ColorzCore.Preprocessor;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

//TODO: Make errors less redundant (due to recursive nature, many paths will give several redundant errors).

namespace ColorzCore.Parser
{
    class EAParser
    {
        public MacroCollection Macros { get; }
        public Dictionary<string, Definition> Definitions { get; }
        public Dictionary<string, IList<Raw>> Raws { get; }
        public static readonly HashSet<string> SpecialCodes = new HashSet<string> { "ORG", "PUSH", "POP", "MESSAGE", "WARNING", "ERROR", "ASSERT", "PROTECT", "ALIGN", "FILL" };
        //public static readonly HashSet<string> BuiltInMacros = new HashSet<string> { "String", "AddToPool" };
        //TODO: Built in macros.
        //public static readonly Dictionary<string, BuiltInMacro(?)> BuiltInMacros;
        public ImmutableStack<Closure> GlobalScope { get; }
        public int CurrentOffset
        {
            get { return currentOffset; }
            private set
            {
                if (value > EAOptions.Instance.maximumRomSize)
                {
                    if (validOffset) //Error only the first time.
                    {
                        Error(head == null ? new Location?() : head.Location, "Invalid offset: " + value.ToString("X"));
                        validOffset = false;
                    }
                }
                else
                {
                    currentOffset = value;
                    validOffset = true;
                    offsetInitialized = true;
                }
            }

        }
        public ImmutableStack<bool> Inclusion { get; set; }

        public Pool Pool { get; private set; }

        private readonly DirectiveHandler directiveHandler;

        private readonly Stack<Tuple<int, bool>> pastOffsets; // currentOffset, offsetInitialized
        private readonly IList<Tuple<int, int, Location>> protectedRegions;

        public Log log;

        public bool IsIncluding
        {
            get
            {
                bool acc = true;

                for (ImmutableStack<bool> temp = Inclusion; !temp.IsEmpty && acc; temp = temp.Tail)
                {
                    acc &= temp.Head;
                }

                return acc;
            }
        }

        private bool validOffset;
        private bool offsetInitialized; // false until first ORG, used to warn about writing before first org 
        private int currentOffset;
        private Token? head; //TODO: Make this make sense

        public EAParser(Dictionary<string, IList<Raw>> raws, Log log, DirectiveHandler directiveHandler)
        {
            GlobalScope = new ImmutableStack<Closure>(new BaseClosure(this), ImmutableStack<Closure>.Nil);
            pastOffsets = new Stack<Tuple<int, bool>>();
            protectedRegions = new List<Tuple<int, int, Location>>();
            this.log = log;
            Raws = raws;
            CurrentOffset = 0;
            validOffset = true;
            offsetInitialized = false;
            Macros = new MacroCollection(this);
            Definitions = new Dictionary<string, Definition>();
            Inclusion = ImmutableStack<bool>.Nil;
            this.directiveHandler = directiveHandler;

            Pool = new Pool();
        }

        public bool IsReservedName(string name)
        {
            return Raws.ContainsKey(name.ToUpper()) || SpecialCodes.Contains(name.ToUpper());
        }
        public bool IsValidDefinitionName(string name)
        {
            return !(Definitions.ContainsKey(name) || IsReservedName(name));
        }
        public bool IsValidMacroName(string name, int paramNum)
        {
            return !(Macros.HasMacro(name, paramNum)) && !IsReservedName(name);
        }
        public bool IsValidLabelName(string name)
        {
            return true;//!IsReservedName(name);
            //TODO?
        }
        public IList<ILineNode> ParseAll(IEnumerable<Token> tokenStream)
        {
            //TODO: Make BlockNode or EAProgramNode?
            //Note must be strict to get all information on the closure before evaluating terms.
            IList<ILineNode> myLines = new List<ILineNode>();
            MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(tokenStream);
            tokens.MoveNext();
            while (!tokens.EOS)
            {
                if (tokens.Current.Type != TokenType.NEWLINE || tokens.MoveNext())
                {
                    ILineNode? retVal = ParseLine(tokens, GlobalScope);
                    retVal.IfJust(n => myLines.Add(n));
                }
            }
            return myLines;
        }

        private BlockNode ParseBlock(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Location start = tokens.Current.Location;
            tokens.MoveNext();
            BlockNode temp = new BlockNode();

            while (!tokens.EOS && tokens.Current.Type != TokenType.CLOSE_BRACE)
            {
                ILineNode? x = ParseLine(tokens, scopes);

                if (x != null)
                {
                    temp.Children.Add(x);
                }
            }

            if (!tokens.EOS)
            {
                tokens.MoveNext();
            }
            else
            {
                Error(start, "Unmatched brace.");
            }

            return temp;
        }

        // TODO: these next two functions should probably be moved into their own module

        public static int ConvertToAddress(int value)
        {
            /*
                NOTE: Offset 0 is always converted to a null address
                If one wants to instead refer to ROM offset 0 they would want to use the address directly instead.
                If ROM offset 0 is already address 0 then this is a moot point.
            */

            if (value > 0 && value < EAOptions.Instance.maximumRomSize)
            {
                value += EAOptions.Instance.romBaseAddress;
            }

            return value;
        }

        public static int ConvertToOffset(int value)
        {
            if (value >= EAOptions.Instance.romBaseAddress && value <= EAOptions.Instance.romBaseAddress + EAOptions.Instance.maximumRomSize)
            {
                value -= EAOptions.Instance.romBaseAddress;
            }

            return value;
        }

        private ILineNode? ParseStatement(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            while (ExpandIdentifier(tokens, scopes)) { }

            head = tokens.Current;
            tokens.MoveNext();

            //TODO: Replace with real raw information, and error if not valid.
            IList<IParamNode> parameters;
            //TODO: Make intelligent to reject malformed parameters.
            //TODO: Parse parameters after checking code validity.
            if (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON)
            {
                parameters = ParseParamList(tokens, scopes);
            }
            else
            {
                parameters = new List<IParamNode>();
                tokens.MoveNext();
            }

            string upperCodeIdentifier = head.Content.ToUpperInvariant();

            if (SpecialCodes.Contains(upperCodeIdentifier))
            {
                return upperCodeIdentifier switch
                {
                    "ORG" => ParseOrgStatement(parameters),
                    "PUSH" => ParsePushStatement(parameters),
                    "POP" => ParsePopStatement(parameters),
                    "ASSERT" => ParseAssertStatement(parameters),
                    "PROTECT" => ParseProtectStatement(parameters),
                    "ALIGN" => ParseAlignStatement(parameters),
                    "FILL" => ParseFillStatement(parameters),
                    "MESSAGE" => ParseMessageStatement(parameters, scopes),
                    "WARNING" => ParseWarningStatement(parameters, scopes),
                    "ERROR" => ParseErrorStatement(parameters, scopes),
                    _ => null, // TODO: this is an error
                };
            }
            else if (Raws.TryGetValue(upperCodeIdentifier, out IList<Raw>? raws))
            {
                //TODO: Check for matches. Currently should type error.
                foreach (Raw raw in raws)
                {
                    if (raw.Fits(parameters))
                    {
                        if ((CurrentOffset % raw.Alignment) != 0)
                        {
                            Error($"Bad code alignment (offset: {CurrentOffset:X8})");
                        }

                        StatementNode temp = new RawNode(raw, head, CurrentOffset, parameters);

                        // TODO: more efficient spacewise to just have contiguous writing and not an offset with every line?
                        CheckDataWrite(temp.Size);
                        CurrentOffset += temp.Size;

                        return temp;
                    }
                }

                if (raws.Count == 1)
                {
                    Error($"Incorrect parameters in raw `{raws[0].ToPrettyString()}`");
                }
                else
                {
                    Error($"Couldn't find suitable variant of raw `{head.Content}`.");

                    for (int i = 0; i < raws.Count; i++)
                    {
                        Error($"Variant {i + 1}: `{raws[i].ToPrettyString()}`");
                    }
                }

                IgnoreRestOfStatement(tokens);
                return null;
            }
            else
            {
                Error("Unrecognized code: " + head.Content);
                return null;
            }
        }

        private ILineNode? ParseOrgStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count != 1)
            {
                Error($"Incorrect number of parameters in ORG: {parameters.Count}");
                return null;
            }

            parameters[0].AsAtom().IfJust(
                atom => atom.TryEvaluate(e => Error(parameters[0].MyLocation, e.Message), EvaluationPhase.Immediate).IfJust(
                    offsetValue => { CurrentOffset = ConvertToOffset(offsetValue); },
                    () => Error(parameters[0].MyLocation, "Expected atomic param to ORG.")));

            return null;
        }

        private ILineNode? ParsePushStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count != 0)
            {
                Error("Incorrect number of parameters in PUSH: " + parameters.Count);
            }
            else
            {
                pastOffsets.Push(new Tuple<int, bool>(CurrentOffset, offsetInitialized));
            }

            return null;
        }

        private ILineNode? ParsePopStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count != 0)
            {
                Error($"Incorrect number of parameters in POP: {parameters.Count}");
            }
            else if (pastOffsets.Count == 0)
            {
                Error("POP without matching PUSH.");
            }
            else
            {
                Tuple<int, bool> tuple = pastOffsets.Pop();

                CurrentOffset = tuple.Item1;
                offsetInitialized = tuple.Item2;
            }

            return null;
        }

        private ILineNode? ParseAssertStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count != 1)
            {
                Error($"Incorrect number of parameters in ASSERT: {parameters.Count}");
                return null;
            }

            // helper for distinguishing boolean expressions and other expressions
            static bool IsConditionalOperatorHelper(IAtomNode node)
            {
                return node switch
                {
                    UnaryOperatorNode uon => uon.OperatorToken.Type switch
                    {
                        TokenType.LOGNOT_OP => true,
                        _ => false,
                    },

                    OperatorNode on => on.OperatorToken.Type switch
                    {
                        TokenType.LOGAND_OP => true,
                        TokenType.LOGOR_OP => true,
                        TokenType.COMPARE_EQ => true,
                        TokenType.COMPARE_NE => true,
                        TokenType.COMPARE_GT => true,
                        TokenType.COMPARE_GE => true,
                        TokenType.COMPARE_LE => true,
                        TokenType.COMPARE_LT => true,
                        _ => false,
                    },

                    _ => false,
                };
            }

            IAtomNode? atom = parameters[0].AsAtom();

            if (atom != null)
            {
                bool isBoolean = IsConditionalOperatorHelper(atom);

                atom.TryEvaluate(e => Error(parameters[0].MyLocation, e.Message), EvaluationPhase.Immediate).IfJust(
                    temp =>
                    {
                        // if boolean expession => fail if 0, else (legacy behavoir) fail if negative
                        if (isBoolean && temp == 0 || !isBoolean && temp < 0)
                        {
                            Error(parameters[0].MyLocation, "Assertion error: " + temp);
                        }
                    });
            }
            else
            {
                Error(parameters[0].MyLocation, "Expected atomic param to ASSERT.");
            }

            return null;
        }

        private ILineNode? ParseProtectStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count == 1)
            {
                parameters[0].AsAtom().IfJust(
                    atom => atom.TryEvaluate(e => { Error(parameters[0].MyLocation, e.Message); }, EvaluationPhase.Immediate).IfJust(
                    temp =>
                    {
                        protectedRegions.Add(new Tuple<int, int, Location>(temp, 4, head!.Location));
                    }),
                    () => { Error(parameters[0].MyLocation, "Expected atomic param to PROTECT"); });
            }
            else if (parameters.Count == 2)
            {
                int start = 0, end = 0;
                bool errorOccurred = false;
                parameters[0].AsAtom().IfJust(
                    atom => atom.TryEvaluate(e => { Error(parameters[0].MyLocation, e.Message); errorOccurred = true; }, EvaluationPhase.Immediate).IfJust(
                    temp =>
                    {
                        start = temp;
                    }),
                    () => { Error(parameters[0].MyLocation, "Expected atomic param to PROTECT"); errorOccurred = true; });
                parameters[1].AsAtom().IfJust(
                    atom => atom.TryEvaluate(e => { Error(parameters[0].MyLocation, e.Message); errorOccurred = true; }, EvaluationPhase.Immediate).IfJust(
                    temp =>
                    {
                        end = temp;
                    }),
                    () => { Error(parameters[0].MyLocation, "Expected atomic param to PROTECT"); errorOccurred = true; });
                if (!errorOccurred)
                {
                    int length = end - start;
                    if (length > 0)
                    {
                        protectedRegions.Add(new Tuple<int, int, Location>(start, length, head!.Location));
                    }
                    else
                    {
                        Warning("Protected region not valid (end offset not after start offset). No region protected.");
                    }
                }
            }
            else
            {
                Error("Incorrect number of parameters in PROTECT: " + parameters.Count);
            }

            return null;
        }

        private ILineNode? ParseAlignStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count != 1)
            {
                Error("Incorrect number of parameters in ALIGN: " + parameters.Count);
                return null;
            }

            parameters[0].AsAtom().IfJust(
                atom => atom.TryEvaluate(e => Error(parameters[0].MyLocation, e.Message), EvaluationPhase.Immediate).IfJust(
                temp => CurrentOffset = CurrentOffset % temp != 0 ? CurrentOffset + temp - CurrentOffset % temp : CurrentOffset),
                () => Error(parameters[0].MyLocation, "Expected atomic param to ALIGN"));

            return null;
        }

        private ILineNode? ParseFillStatement(IList<IParamNode> parameters)
        {
            if (parameters.Count > 2 || parameters.Count == 0)
            {
                Error("Incorrect number of parameters in FILL: " + parameters.Count);
                return null;
            }

            // FILL amount [value]

            int amount = 0;
            int value = 0;

            if (parameters.Count == 2)
            {
                // param 2 (if given) is fill value

                parameters[1].AsAtom().IfJust(
                    atom => atom.TryEvaluate(e => Error(parameters[0].MyLocation, e.Message), EvaluationPhase.Immediate).IfJust(
                        val => { value = val; }),
                    () => Error(parameters[0].MyLocation, "Expected atomic param to FILL"));
            }

            // param 1 is amount of bytes to fill
            parameters[0].AsAtom().IfJust(
                atom => atom.TryEvaluate(e => Error(parameters[0].MyLocation, e.Message), EvaluationPhase.Immediate).IfJust(
                    val => { amount = val; }),
                () => Error(parameters[0].MyLocation, "Expected atomic param to FILL"));

            if (amount > 0)
            {
                var data = new byte[amount];

                for (int i = 0; i < amount; ++i)
                {
                    data[i] = (byte)value;
                }

                var node = new DataNode(CurrentOffset, data);

                CheckDataWrite(amount);
                CurrentOffset += amount;

                return node;
            }

            return null;
        }

        private ILineNode? ParseMessageStatement(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            Message(PrettyPrintParamsForMessage(parameters, scopes));
            return null;
        }

        private ILineNode? ParseWarningStatement(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            Warning(PrettyPrintParamsForMessage(parameters, scopes));
            return null;
        }

        private ILineNode? ParseErrorStatement(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            Error(PrettyPrintParamsForMessage(parameters, scopes));
            return null;
        }

        public IList<IList<Token>> ParseMacroParamList(MergeableGenerator<Token> tokens)
        {
            IList<IList<Token>> parameters = new List<IList<Token>>();
            int parenNestings = 0;

            // HACK: this allows macro([1, 2, 3]) from expanding into a single parameter
            int bracketBalance = 0;
            do
            {
                tokens.MoveNext();
                List<Token> currentParam = new List<Token>();
                while (
                    !(parenNestings == 0
                      && (tokens.Current.Type == TokenType.CLOSE_PAREN || (bracketBalance == 0 && tokens.Current.Type == TokenType.COMMA)))
                    && tokens.Current.Type != TokenType.NEWLINE)
                {
                    switch (tokens.Current.Type)
                    {
                        case TokenType.CLOSE_PAREN:
                            parenNestings--;
                            break;
                        case TokenType.OPEN_PAREN:
                            parenNestings++;
                            break;
                        case TokenType.OPEN_BRACKET:
                            bracketBalance++;
                            break;
                        case TokenType.CLOSE_BRACKET:
                            bracketBalance--;
                            break;
                    }

                    currentParam.Add(tokens.Current);
                    tokens.MoveNext();
                }
                parameters.Add(currentParam);
            } while (tokens.Current.Type != TokenType.CLOSE_PAREN && tokens.Current.Type != TokenType.NEWLINE);
            if (tokens.Current.Type != TokenType.CLOSE_PAREN || parenNestings != 0)
            {
                Error(tokens.Current.Location, "Unmatched open parenthesis.");
            }
            else
            {
                tokens.MoveNext();
            }
            return parameters;
        }

        private IList<IParamNode> ParseParamList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool expandFirstDef = true)
        {
            IList<IParamNode> paramList = new List<IParamNode>();
            bool first = true;

            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON && !tokens.EOS)
            {
                Token localHead = tokens.Current;
                ParseParam(tokens, scopes, expandFirstDef || !first).IfJust(
                    n => paramList.Add(n),
                    () => Error(localHead.Location, "Expected parameter."));
                first = false;
            }

            if (tokens.Current.Type == TokenType.SEMICOLON)
            {
                tokens.MoveNext();
            }

            return paramList;
        }

        private IList<IParamNode> ParsePreprocParamList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool allowsFirstExpanded)
        {
            IList<IParamNode> temp = ParseParamList(tokens, scopes, allowsFirstExpanded);

            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i].Type == ParamType.STRING && ((StringNode)temp[i]).IsValidIdentifier())
                {
                    temp[i] = ((StringNode)temp[i]).ToIdentifier(scopes);
                }
            }

            return temp;
        }

        private IParamNode? ParseParam(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool expandDefs = true)
        {
            Token localHead = tokens.Current;
            switch (tokens.Current.Type)
            {
                case TokenType.OPEN_BRACKET:
                    return new ListNode(localHead.Location, ParseList(tokens, scopes));
                case TokenType.STRING:
                    tokens.MoveNext();
                    return new StringNode(localHead);
                case TokenType.MAYBE_MACRO:
                    //TODO: Move this and the one in ExpandId to a separate ParseMacroNode that may return an Invocation.
                    if (expandDefs && ExpandIdentifier(tokens, scopes))
                    {
                        return ParseParam(tokens, scopes);
                    }
                    else
                    {
                        tokens.MoveNext();
                        IList<IList<Token>> param = ParseMacroParamList(tokens);
                        //TODO: Smart errors if trying to redefine a macro with the same num of params.
                        return new MacroInvocationNode(this, localHead, param, scopes);
                    }
                case TokenType.IDENTIFIER:
                    if (expandDefs && Definitions.ContainsKey(localHead.Content) && ExpandIdentifier(tokens, scopes))
                    {
                        return ParseParam(tokens, scopes, expandDefs);
                    }
                    else
                    {
                        return ParseAtom(tokens, scopes, expandDefs);
                    }

                default:
                    return ParseAtom(tokens, scopes, expandDefs);
            }
        }

        private static readonly Dictionary<TokenType, int> precedences = new Dictionary<TokenType, int> {
            { TokenType.MUL_OP, 3 },
            { TokenType.DIV_OP, 3 },
            { TokenType.MOD_OP, 3 },
            { TokenType.ADD_OP, 4 },
            { TokenType.SUB_OP, 4 },
            { TokenType.LSHIFT_OP, 5 },
            { TokenType.RSHIFT_OP, 5 },
            { TokenType.SIGNED_RSHIFT_OP, 5 },
            { TokenType.COMPARE_GE, 6 },
            { TokenType.COMPARE_GT, 6 },
            { TokenType.COMPARE_LT, 6 },
            { TokenType.COMPARE_LE, 6 },
            { TokenType.COMPARE_EQ, 7 },
            { TokenType.COMPARE_NE, 7 },
            { TokenType.AND_OP, 8 },
            { TokenType.XOR_OP, 9 },
            { TokenType.OR_OP, 10 },
            { TokenType.LOGAND_OP, 11 },
            { TokenType.LOGOR_OP, 12 },
            { TokenType.UNDEFINED_COALESCE_OP, 13 },
        };

        private IAtomNode? ParseAtom(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool expandDefs = true)
        {
            //Use Shift Reduce Parsing
            Token localHead = tokens.Current;
            Stack<Either<IAtomNode, Token>> grammarSymbols = new Stack<Either<IAtomNode, Token>>();
            bool ended = false;
            while (!ended)
            {
                bool shift = false, lookingForAtom = grammarSymbols.Count == 0 || grammarSymbols.Peek().IsRight;
                Token lookAhead = tokens.Current;

                if (!ended && !lookingForAtom) //Is already a complete node. Needs an operator of matching precedence and a node of matching prec to reduce.
                {
                    //Verify next symbol to be a binary operator.
                    switch (lookAhead.Type)
                    {
                        case TokenType.MUL_OP:
                        case TokenType.DIV_OP:
                        case TokenType.MOD_OP:
                        case TokenType.ADD_OP:
                        case TokenType.SUB_OP:
                        case TokenType.LSHIFT_OP:
                        case TokenType.RSHIFT_OP:
                        case TokenType.SIGNED_RSHIFT_OP:
                        case TokenType.AND_OP:
                        case TokenType.XOR_OP:
                        case TokenType.OR_OP:
                        case TokenType.LOGAND_OP:
                        case TokenType.LOGOR_OP:
                        case TokenType.COMPARE_LT:
                        case TokenType.COMPARE_LE:
                        case TokenType.COMPARE_EQ:
                        case TokenType.COMPARE_NE:
                        case TokenType.COMPARE_GE:
                        case TokenType.COMPARE_GT:
                            if (precedences.TryGetValue(lookAhead.Type, out int precedence))
                            {
                                Reduce(grammarSymbols, precedence);
                            }
                            shift = true;
                            break;
                        case TokenType.UNDEFINED_COALESCE_OP:
                            // '??' is right-associative, so don't reduce here
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
                        case TokenType.MAYBE_MACRO:
                        case TokenType.NUMBER:
                            shift = true;
                            break;
                        case TokenType.OPEN_PAREN:
                            {
                                tokens.MoveNext();
                                IAtomNode? interior = ParseAtom(tokens, scopes);
                                if (tokens.Current.Type != TokenType.CLOSE_PAREN)
                                {
                                    Error(tokens.Current.Location, "Unmatched open parenthesis (currently at " + tokens.Current.Type + ").");
                                    return null;
                                }
                                else if (interior == null)
                                {
                                    Error(lookAhead.Location, "Expected expression inside paretheses. ");
                                    return null;
                                }
                                else
                                {
                                    grammarSymbols.Push(new Left<IAtomNode, Token>(interior));
                                    tokens.MoveNext();
                                    break;
                                }
                            }
                        case TokenType.SUB_OP:
                        case TokenType.LOGNOT_OP:
                        case TokenType.NOT_OP:
                            {
                                //Assume unary negation.
                                tokens.MoveNext();
                                IAtomNode? interior = ParseAtom(tokens, scopes);
                                if (interior == null)
                                {
                                    Error(lookAhead.Location, "Expected expression after unary operator.");
                                    return null;
                                }
                                grammarSymbols.Push(new Left<IAtomNode, Token>(new UnaryOperatorNode(lookAhead, interior)));
                                break;
                            }
                        case TokenType.COMMA:
                            Error(lookAhead.Location, "Unexpected comma (perhaps unrecognized macro invocation?).");
                            IgnoreRestOfStatement(tokens);
                            return null;
                        case TokenType.MUL_OP:
                        case TokenType.DIV_OP:
                        case TokenType.MOD_OP:
                        case TokenType.ADD_OP:
                        case TokenType.LSHIFT_OP:
                        case TokenType.RSHIFT_OP:
                        case TokenType.SIGNED_RSHIFT_OP:
                        case TokenType.AND_OP:
                        case TokenType.XOR_OP:
                        case TokenType.OR_OP:
                        case TokenType.LOGAND_OP:
                        case TokenType.LOGOR_OP:
                        case TokenType.COMPARE_LT:
                        case TokenType.COMPARE_LE:
                        case TokenType.COMPARE_EQ:
                        case TokenType.COMPARE_NE:
                        case TokenType.COMPARE_GE:
                        case TokenType.COMPARE_GT:
                        case TokenType.UNDEFINED_COALESCE_OP:
                        default:
                            Error(lookAhead.Location, "Expected identifier or literal, got " + lookAhead.Type + ": " + lookAhead.Content + '.');
                            IgnoreRestOfStatement(tokens);
                            return null;
                    }
                }

                if (shift)
                {
                    if (lookAhead.Type == TokenType.IDENTIFIER)
                    {
                        if (expandDefs && ExpandIdentifier(tokens, scopes))
                        {
                            continue;
                        }

                        if (lookAhead.Content.ToUpper() == "CURRENTOFFSET")
                        {
                            grammarSymbols.Push(new Left<IAtomNode, Token>(new NumberNode(lookAhead, CurrentOffset)));
                        }
                        else
                        {
                            grammarSymbols.Push(new Left<IAtomNode, Token>(new IdentifierNode(lookAhead, scopes)));
                        }
                    }
                    else if (lookAhead.Type == TokenType.MAYBE_MACRO)
                    {
                        ExpandIdentifier(tokens, scopes);
                        continue;
                    }
                    else if (lookAhead.Type == TokenType.NUMBER)
                    {
                        grammarSymbols.Push(new Left<IAtomNode, Token>(new NumberNode(lookAhead)));
                    }
                    else if (lookAhead.Type == TokenType.ERROR)
                    {
                        Error(lookAhead.Location, $"Unexpected token: {lookAhead.Content}");
                        tokens.MoveNext();
                        return null;
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
                Reduce(grammarSymbols, int.MaxValue);
            }
            if (grammarSymbols.Peek().IsRight)
            {
                Error(grammarSymbols.Peek().GetRight.Location, $"Unexpected token: {grammarSymbols.Peek().GetRight.Type}");
            }
            return grammarSymbols.Peek().GetLeft;
        }

        /***
         *   Precondition: grammarSymbols alternates between IAtomNodes, operator Tokens, .Count is odd
         *                 the precedences of the IAtomNodes is increasing.
         *   Postcondition: Either grammarSymbols.Count == 1, or everything in grammarSymbols will have precedence <= targetPrecedence.
         *
         */
        private static void Reduce(Stack<Either<IAtomNode, Token>> grammarSymbols, int targetPrecedence)
        {
            while (grammarSymbols.Count > 1)// && grammarSymbols.Peek().GetLeft.Precedence > targetPrecedence)
            {
                //These shouldn't error...
                IAtomNode r = grammarSymbols.Pop().GetLeft;

                if (precedences[grammarSymbols.Peek().GetRight.Type] > targetPrecedence)
                {
                    grammarSymbols.Push(new Left<IAtomNode, Token>(r));
                    break;
                }
                else
                {
                    Token op = grammarSymbols.Pop().GetRight;
                    IAtomNode l = grammarSymbols.Pop().GetLeft;

                    grammarSymbols.Push(new Left<IAtomNode, Token>(new OperatorNode(l, op, r, l.Precedence)));
                }
            }
        }

        private IList<IAtomNode> ParseList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Token localHead = tokens.Current;
            tokens.MoveNext();

            IList<IAtomNode> atoms = new List<IAtomNode>();
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_BRACKET)
            {
                IAtomNode? res = ParseAtom(tokens, scopes);
                res.IfJust(
                    n => atoms.Add(n),
                    () => Error(tokens.Current.Location, "Expected atomic value, got " + tokens.Current.Type + "."));
                if (tokens.Current.Type == TokenType.COMMA)
                {
                    tokens.MoveNext();
                }
            }
            if (tokens.Current.Type == TokenType.CLOSE_BRACKET)
            {
                tokens.MoveNext();
            }
            else
            {
                Error(localHead.Location, "Unmatched open bracket.");
            }

            return atoms;
        }

        public ILineNode? ParseLine(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            if (IsIncluding)
            {
                if (tokens.Current.Type == TokenType.NEWLINE || tokens.Current.Type == TokenType.SEMICOLON)
                {
                    tokens.MoveNext();
                    return null;
                }
                head = tokens.Current;
                switch (head.Type)
                {
                    case TokenType.IDENTIFIER:
                    case TokenType.MAYBE_MACRO:
                        if (ExpandIdentifier(tokens, scopes))
                        {
                            // NOTE: we check here if we didn't end up with something that can't be a statement

                            switch (tokens.Current.Type)
                            {
                                case TokenType.IDENTIFIER:
                                case TokenType.MAYBE_MACRO:
                                case TokenType.OPEN_BRACE:
                                case TokenType.PREPROCESSOR_DIRECTIVE:
                                    return ParseLine(tokens, scopes);

                                default:
                                    // it is somewhat common for users to do '#define Foo 0xABCD' and then later 'Foo:'
                                    Error($"Expansion of macro `{head.Content}` did not result in a valid statement. Did you perhaps attempt to define a label or symbol with that name?");
                                    IgnoreRestOfLine(tokens);

                                    return null;
                            }
                        }
                        else
                        {
                            tokens.MoveNext();
                            switch (tokens.Current.Type)
                            {
                                case TokenType.COLON:
                                    tokens.MoveNext();
                                    TryDefineSymbol(scopes, head.Content, CurrentOffset);
                                    return null;
                                case TokenType.ASSIGN:
                                    tokens.MoveNext();

                                    ParseAtom(tokens, scopes, true).IfJust(
                                        atom => atom.TryEvaluate(
                                            e => TryDefineSymbol(scopes, head.Content, atom), EvaluationPhase.Early).IfJust(
                                            value => TryDefineSymbol(scopes, head.Content, value)),
                                        () => Error($"Couldn't define symbol `{head.Content}`: exprected expression."));

                                    return null;

                                default:
                                    tokens.PutBack(head);
                                    return ParseStatement(tokens, scopes);
                            }
                        }
                    case TokenType.OPEN_BRACE:
                        return ParseBlock(tokens, new ImmutableStack<Closure>(new Closure(), scopes));
                    case TokenType.PREPROCESSOR_DIRECTIVE:
                        return ParsePreprocessor(tokens, scopes);
                    case TokenType.OPEN_BRACKET:
                        Error("Unexpected list literal.");
                        IgnoreRestOfLine(tokens);
                        break;
                    case TokenType.NUMBER:
                    case TokenType.OPEN_PAREN:
                        Error("Unexpected mathematical expression.");
                        IgnoreRestOfLine(tokens);
                        break;
                    default:
                        tokens.MoveNext();

                        if (string.IsNullOrEmpty(head.Content))
                        {
                            Error($"Unexpected token: {head.Type}.");
                        }
                        else
                        {
                            Error($"Unexpected token: {head.Type}: {head.Content}.");
                        }

                        IgnoreRestOfLine(tokens);
                        break;
                }
                return null;
            }
            else
            {
                bool hasNext = true;
                while (tokens.Current.Type != TokenType.PREPROCESSOR_DIRECTIVE && (hasNext = tokens.MoveNext()))
                {
                    ;
                }

                if (hasNext)
                {
                    return ParsePreprocessor(tokens, scopes);
                }
                else
                {
                    Error(null, $"Missing {Inclusion.Count} endif(s).");
                    return null;
                }
            }
        }

        private void TryDefineSymbol(ImmutableStack<Closure> scopes, string name, int value)
        {
            if (scopes.Head.HasLocalSymbol(name))
            {
                Warning($"Symbol already in scope, ignoring: {name}");
            }
            else if (!IsValidLabelName(name))
            {
                // NOTE: IsValidLabelName returns true always. This is dead code
                Error($"Invalid symbol name {name}.");
            }
            else
            {
                scopes.Head.AddSymbol(name, value);
            }
        }

        private void TryDefineSymbol(ImmutableStack<Closure> scopes, string name, IAtomNode expression)
        {
            if (scopes.Head.HasLocalSymbol(name))
            {
                Warning($"Symbol already in scope, ignoring: {name}");
            }
            else if (!IsValidLabelName(name))
            {
                // NOTE: IsValidLabelName returns true always. This is dead code
                Error($"Invalid symbol name {name}.");
            }
            else
            {
                scopes.Head.AddSymbol(name, expression);
            }
        }

        private ILineNode? ParsePreprocessor(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            head = tokens.Current;
            tokens.MoveNext();

            // Note: Not a ParseParamList because no commas.
            // HACK: #if wants its parameters to be expanded, but other directives (define, ifdef, undef, etc) do not
            IList<IParamNode> paramList = ParsePreprocParamList(tokens, scopes, head.Content == "#if");
            ILineNode? result = directiveHandler.HandleDirective(this, head, paramList, tokens);

            if (result != null)
            {
                CheckDataWrite(result.Size);
                CurrentOffset += result.Size;
            }

            return result;
        }

        /***
         *   Precondition: tokens.Current.Type == TokenType.IDENTIFIER || MAYBE_MACRO
         *   Postcondition: tokens.Current is fully reduced (i.e. not a macro, and not a definition)
         *   Returns: true iff tokens was actually expanded.
         */
        public bool ExpandIdentifier(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            bool ret = false;
            //Macros and Definitions.
            if (tokens.Current.Type == TokenType.MAYBE_MACRO && Macros.ContainsName(tokens.Current.Content))
            {
                Token localHead = tokens.Current;
                tokens.MoveNext();
                IList<IList<Token>> parameters = ParseMacroParamList(tokens);
                if (Macros.HasMacro(localHead.Content, parameters.Count))
                {
                    tokens.PrependEnumerator(Macros.GetMacro(localHead.Content, parameters.Count).ApplyMacro(localHead, parameters, scopes).GetEnumerator());
                }
                else
                {
                    Error($"No overload of {localHead.Content} with {parameters.Count} parameters.");
                }
                return true;
            }
            else if (tokens.Current.Type == TokenType.MAYBE_MACRO)
            {
                Token localHead = tokens.Current;
                tokens.MoveNext();
                tokens.PutBack(new Token(TokenType.IDENTIFIER, localHead.Location, localHead.Content));
                return true;
            }
            else if (Definitions.ContainsKey(tokens.Current.Content))
            {
                Token localHead = tokens.Current;
                tokens.MoveNext();
                tokens.PrependEnumerator(Definitions[localHead.Content].ApplyDefinition(localHead).GetEnumerator());
                return true;
            }

            return ret;
        }

        public void Message(Location? loc, string message)
        {
            log.Message(Log.MessageKind.MESSAGE, loc, message);
        }

        public void Warning(Location? loc, string message)
        {
            log.Message(Log.MessageKind.WARNING, loc, message);
        }

        public void Error(Location? loc, string message)
        {
            log.Message(Log.MessageKind.ERROR, loc, message);
        }

        // shorthand helpers

        public void Message(string message) => Message(head?.Location, message);
        public void Warning(string message) => Warning(head?.Location, message);
        public void Error(string message) => Error(head?.Location, message);

        private void IgnoreRestOfStatement(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON && tokens.MoveNext()) { }
            if (tokens.Current.Type == TokenType.SEMICOLON)
            {
                tokens.MoveNext();
            }
        }

        private void IgnoreRestOfLine(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.MoveNext()) { }
        }

        public void Clear()
        {
            Macros.Clear();
            Definitions.Clear();
            Raws.Clear();
            Inclusion = ImmutableStack<bool>.Nil;
            CurrentOffset = 0;
            pastOffsets.Clear();
        }

        private string PrettyPrintParamsForMessage(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            return string.Join(" ", parameters.Select(parameter => parameter switch
            {
                StringNode node => ExpandUserFormatString(scopes, parameter.MyLocation.OffsetBy(1), node.Value),
                _ => parameter.PrettyPrint(),
            }));
        }

        private static readonly Regex formatItemRegex = new Regex(@"\{(?<expr>[^:}]+)(?:\:(?<format>[^:}]*))?\}");

        private string ExpandUserFormatString(ImmutableStack<Closure> scopes, Location baseLocation, string stringValue)
        {
            string UserFormatStringError(string message, string details)
            {
                Error($"An error occurred while expanding format string ({message}).");
                return $"<{message}: {details}>";
            }

            return formatItemRegex.Replace(stringValue, match =>
            {
                string expr = match.Groups["expr"].Value!;
                string? format = match.Groups["format"].Value;

                MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(
                    new Tokenizer().TokenizeLine(
                        $"{expr} \n", baseLocation.file, baseLocation.lineNum, baseLocation.colNum + match.Index));

                tokens.MoveNext();

                IAtomNode? node = ParseAtom(tokens, scopes);

                if (node == null || tokens.Current.Type != TokenType.NEWLINE)
                {
                    return UserFormatStringError("bad expression", $"'{expr}'");
                }

                if (node.TryEvaluate(e => Error(node.MyLocation, e.Message),
                    EvaluationPhase.Early) is int value)
                {
                    try
                    {
                        // TODO: do we need to raise an error when result == format?
                        // this happens (I think) when a custom format specifier with no substitution
                        return value.ToString(format, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException e)
                    {
                        return UserFormatStringError("bad format specifier", $"'{format}' ({e.Message})");
                    }
                }
                else
                {
                    return UserFormatStringError("failed to evaluate expression", $"'{expr}'");
                }
            });
        }

        // Return value: Location where protection occurred. Nothing if location was not protected.
        private Location? IsProtected(int offset, int length)
        {
            foreach (Tuple<int, int, Location> protectedRegion in protectedRegions)
            {
                //They intersect if the last offset in the given region is after the start of this one
                //and the first offset in the given region is before the last of this one
                if (offset + length > protectedRegion.Item1 && offset < protectedRegion.Item1 + protectedRegion.Item2)
                {
                    return protectedRegion.Item3;
                }
            }

            return null;
        }

        private void CheckDataWrite(int length)
        {
            // TODO: maybe make this warning optional?
            if (!offsetInitialized)
            {
                Warning("Writing before initializing offset. You may be breaking the ROM! (use `ORG offset` to set write offset).");
                offsetInitialized = false; // only warn once
            }

            // TODO (maybe?): save Location of PROTECT statement, for better diagnosis
            // We would then print something like "Trying to write data to area protected at <location>"

            if (IsProtected(CurrentOffset, length) is Location prot)
            {
                Error($"Trying to write data to area protected by {prot}");
            }
        }
    }
}

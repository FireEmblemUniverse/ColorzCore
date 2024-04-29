using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor;
using ColorzCore.Preprocessor.Macros;
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
    public class EAParser
    {
        public Dictionary<string, Definition> Definitions { get; }
        public MacroCollection Macros { get; }
        public DirectiveHandler DirectiveHandler { get; }

        public Dictionary<string, IList<Raw>> Raws { get; }

        public ImmutableStack<bool> Inclusion { get; set; }

        public Logger log;

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

        private Token? head; // TODO: Make this make sense

        public EAParser(Dictionary<string, IList<Raw>> raws, Logger log, DirectiveHandler directiveHandler)
        {
            GlobalScope = new ImmutableStack<Closure>(new BaseClosure(), ImmutableStack<Closure>.Nil);
            pastOffsets = new Stack<(int, bool)>();
            protectedRegions = new List<(int, int, Location)>();
            this.log = log;
            Raws = raws;
            CurrentOffset = 0;
            validOffset = true;
            offsetInitialized = false;
            Macros = new MacroCollection(this);
            Definitions = new Dictionary<string, Definition>();
            Inclusion = ImmutableStack<bool>.Nil;
            DirectiveHandler = directiveHandler;
        }

        public bool IsReservedName(string name)
        {
            return Raws.ContainsKey(name.ToUpperInvariant()) || SpecialCodes.Contains(name.ToUpperInvariant());
        }

        public bool IsValidDefinitionName(string name)
        {
            return !(Definitions.ContainsKey(name) || IsReservedName(name));
        }

        public bool IsValidMacroName(string name, int paramNum)
        {
            return !Macros.HasMacro(name, paramNum) && !IsReservedName(name);
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
                Error(start, "Didn't find matching brace.");
            }

            return temp;
        }

        public static readonly HashSet<string> SpecialCodes = new HashSet<string> { "ORG", "PUSH", "POP", "MESSAGE", "WARNING", "ERROR", "ASSERT", "PROTECT", "ALIGN", "FILL" };

        private ILineNode? ParseStatement(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            // NOTE: here previously lied en ExpandIdentifier loop
            // though because this is only called from ParseLine after the corresponding check, this is not needed

            head = tokens.Current;
            tokens.MoveNext();

            switch (tokens.Current.Type)
            {
                case TokenType.COLON:
                    tokens.MoveNext();
                    return HandleLabel(head.Content, scopes);

                case TokenType.ASSIGN:
                    tokens.MoveNext();
                    return ParseAssignment(head.Content, tokens, scopes);
            }

            // NOTE: those remarks are old ones from Colorz, idrk what they mean -Stan
            // TODO: Replace with real raw information, and error if not valid.
            // TODO: Make intelligent to reject malformed parameters.
            // TODO: Parse parameters after checking code validity.

            IList<IParamNode> parameters = tokens.Current.Type switch
            {
                TokenType.NEWLINE or TokenType.SEMICOLON => new List<IParamNode>(),
                _ => ParseParamList(tokens, scopes),
            };

            string upperCodeIdentifier = head.Content.ToUpperInvariant();

            if (SpecialCodes.Contains(upperCodeIdentifier))
            {
                return upperCodeIdentifier switch
                {
                    "ORG" => ParseOrgStatement(parameters, scopes),
                    "PUSH" => ParsePushStatement(parameters, scopes),
                    "POP" => ParsePopStatement(parameters, scopes),
                    "ASSERT" => ParseAssertStatement(parameters, scopes),
                    "PROTECT" => ParseProtectStatement(parameters, scopes),
                    "ALIGN" => ParseAlignStatement(parameters, scopes),
                    "FILL" => ParseFillStatement(parameters, scopes),
                    "MESSAGE" => ParseMessageStatement(parameters, scopes),
                    "WARNING" => ParseWarningStatement(parameters, scopes),
                    "ERROR" => ParseErrorStatement(parameters, scopes),
                    _ => null, // TODO: this is an error
                };
            }

            return ParseRawStatement(upperCodeIdentifier, tokens, parameters);
        }

        private ILineNode? ParseAssignment(string name, MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            IAtomNode? atom = this.ParseAtom(tokens, scopes, true);

            if (atom != null)
            {
                return HandleSymbolAssignment(name, atom, scopes);
            }
            else
            {
                Error($"Couldn't define symbol `{name}`: exprected expression.");
            }

            return null;
        }

        private ILineNode? ParseRawStatement(string upperCodeIdentifier, MergeableGenerator<Token> tokens, IList<IParamNode> parameters)
        {
            if (Raws.TryGetValue(upperCodeIdentifier, out IList<Raw>? raws))
            {
                // find the raw matching with parameters
                foreach (Raw raw in raws)
                {
                    if (raw.Fits(parameters))
                    {
                        return HandleRawStatement(new RawNode(raw, head!, CurrentOffset, parameters));
                    }
                }

                if (raws.Count == 1)
                {
                    Error($"Incorrect parameters in raw `{raws[0].ToPrettyString()}`");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append($"Couldn't find suitable variant of raw `{head!.Content}`.");

                    for (int i = 0; i < raws.Count; i++)
                    {
                        sb.Append($"\nVariant {i + 1}: `{raws[i].ToPrettyString()}`");
                    }

                    Error(sb.ToString());
                }

                IgnoreRestOfStatement(tokens);
                return null;
            }
            else
            {
                Error($"Unrecognized statement code: {head!.Content}");
                return null;
            }
        }

        private ILineNode? ParseOrgStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementOneParam("ORG", parameters, HandleOrgStatement);
        }

        private ILineNode? ParsePushStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            if (parameters.Count == 0)
            {
                return HandlePushStatement();
            }
            else
            {
                return StatementExpectsParamCount("PUSH", parameters, 0, 0);
            }
        }

        private ILineNode? ParsePopStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            if (parameters.Count == 0)
            {
                return HandlePopStatement();
            }
            else
            {
                return StatementExpectsParamCount("POP", parameters, 0, 0);
            }
        }

        private ILineNode? ParseAssertStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementOneParam("ASSERT", parameters, HandleAssertStatement);
        }

        // Helper method for printing errors
        private ILineNode? StatementExpectsAtom(string statementName, IParamNode param)
        {
            Error(param.MyLocation,
                $"{statementName} expects an Atom (got {DiagnosticsHelpers.PrettyParamType(param.Type)}).");

            return null;
        }

        // Helper method for printing errors
        private ILineNode? StatementExpectsParamCount(string statementName, IList<IParamNode> parameters, int min, int max)
        {
            if (min == max)
            {
                Error($"A {statementName} statement expects {min} parameters, got {parameters.Count}.");
            }
            else
            {
                Error($"A {statementName} statement expects {min} to {max} parameters, got {parameters.Count}.");
            }

            return null;
        }

        private delegate ILineNode? HandleStatementOne(IAtomNode node);
        private delegate ILineNode? HandleStatementTwo(IAtomNode firstNode, IAtomNode? optionalSecondNode);

        private ILineNode? ParseStatementOneParam(string name, IList<IParamNode> parameters, HandleStatementOne handler)
        {
            if (parameters.Count == 1)
            {
                if (parameters[0] is IAtomNode expr)
                {
                    return handler(expr);
                }
                else
                {
                    return StatementExpectsAtom(name, parameters[0]);
                }
            }
            else
            {
                return StatementExpectsParamCount(name, parameters, 1, 1);
            }
        }

        private ILineNode? ParseStatementTwoParam(string name, IList<IParamNode> parameters, HandleStatementTwo handler)
        {
            if (parameters.Count == 1)
            {
                if (parameters[0] is IAtomNode firstNode)
                {
                    return handler(firstNode, null);
                }
                else
                {
                    return StatementExpectsAtom(name, parameters[0]);
                }
            }
            else if (parameters.Count == 2)
            {
                if (parameters[0] is IAtomNode firstNode)
                {
                    if (parameters[1] is IAtomNode secondNode)
                    {
                        return handler(firstNode, secondNode);
                    }
                    else
                    {
                        return StatementExpectsAtom(name, parameters[1]);
                    }
                }
                else
                {
                    return StatementExpectsAtom(name, parameters[0]);
                }
            }
            else
            {
                return StatementExpectsParamCount(name, parameters, 1, 2);
            }
        }

        private ILineNode? ParseProtectStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementTwoParam("PROTECT", parameters, HandleProtectStatement);
        }

        private ILineNode? ParseAlignStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementTwoParam("ALIGN", parameters, HandleAlignStatement);
        }

        private ILineNode? ParseFillStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementTwoParam("FILL", parameters, HandleFillStatement);
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
            }
            while (tokens.Current.Type != TokenType.CLOSE_PAREN && tokens.Current.Type != TokenType.NEWLINE);

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

        public IList<IParamNode> ParsePreprocParamList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool allowsFirstExpanded)
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
            switch (localHead.Type)
            {
                case TokenType.OPEN_BRACKET:
                    return new ListNode(localHead.Location, ParseList(tokens, scopes));
                case TokenType.STRING:
                    tokens.MoveNext();
                    return new StringNode(localHead);
                case TokenType.MAYBE_MACRO:
                    //TODO: Move this and the one in ExpandId to a separate ParseMacroNode that may return an Invocation.
                    if (expandDefs && ExpandIdentifier(tokens, scopes, true))
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
                    if (expandDefs && ExpandIdentifier(tokens, scopes, true))
                    {
                        return ParseParam(tokens, scopes, expandDefs);
                    }
                    else
                    {
                        switch (localHead.Content.ToUpperInvariant())
                        {
                            case "__FILE__":
                                tokens.MoveNext();
                                return new StringNode(new Token(TokenType.STRING, localHead.Location, localHead.GetSourceLocation().file));

                            default:
                                return this.ParseAtom(tokens, scopes, expandDefs);
                        }
                    }

                default:
                    return this.ParseAtom(tokens, scopes, expandDefs);
            }
        }

        private IList<IAtomNode> ParseList(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            Token localHead = tokens.Current;
            tokens.MoveNext();

            IList<IAtomNode> atoms = new List<IAtomNode>();
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_BRACKET)
            {
                IAtomNode? res = this.ParseAtom(tokens, scopes);
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

                        return ParseStatement(tokens, scopes);

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

        private ILineNode? ParsePreprocessor(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes)
        {
            head = tokens.Current;
            tokens.MoveNext();

            ILineNode? result = DirectiveHandler.HandleDirective(this, head, tokens, scopes);

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
        public bool ExpandIdentifier(MergeableGenerator<Token> tokens, ImmutableStack<Closure> scopes, bool insideExpression = false)
        {
            // function-like macros
            if (tokens.Current.Type == TokenType.MAYBE_MACRO)
            {
                if (Macros.ContainsName(tokens.Current.Content))
                {
                    Token localHead = tokens.Current;
                    tokens.MoveNext();

                    IList<IList<Token>> parameters = ParseMacroParamList(tokens);

                    if (Macros.TryGetMacro(localHead.Content, parameters.Count, out IMacro? macro))
                    {
                        /* macro is 100% not null here, but because we can't use NotNullWhen on TryGetMacro,
                         * since the attribute is unavailable in .NET Framework (which we still target),
                         * the compiler will still diagnose a nullable dereference if we don't use '!' also */

                        ApplyMacroExpansion(tokens, macro!.ApplyMacro(localHead, parameters, scopes), insideExpression);
                    }
                    else
                    {
                        Error($"No overload of {localHead.Content} with {parameters.Count} parameters.");
                    }
                    return true;
                }
                else
                {
                    Token localHead = tokens.Current;
                    tokens.MoveNext();

                    tokens.PutBack(new Token(TokenType.IDENTIFIER, localHead.Location, localHead.Content));
                    return true;
                }
            }

            // object-like macros (aka "Definitions")
            if (Definitions.TryGetValue(tokens.Current.Content, out Definition? definition) && !definition.NonProductive)
            {
                Token localHead = tokens.Current;
                tokens.MoveNext();

                ApplyMacroExpansion(tokens, definition.ApplyDefinition(localHead), insideExpression);
                return true;
            }

            return false;
        }

        private void ApplyMacroExpansion(MergeableGenerator<Token> tokens, IEnumerable<Token> expandedTokens, bool insideExpression = false)
        {
            if (insideExpression && EAOptions.IsWarningEnabled(EAOptions.Warnings.UnguardedExpressionMacros))
            {
                // here we check for any operator that isn't enclosed in parenthesises

                IList<Token> expandedList = expandedTokens.ToList();

                DiagnosticsHelpers.VisitUnguardedOperators(expandedList,
                    token => Warning(token.Location, $"Unguarded expansion of mathematical operator. Consider adding guarding parenthesises around definition."));

                tokens.PrependEnumerator(expandedList.GetEnumerator());
            }
            else
            {
                tokens.PrependEnumerator(expandedTokens.GetEnumerator());
            }
        }

        private void MessageTrace(Logger.MessageKind kind, Location? location, string message)
        {
            if (location is Location myLocation && myLocation.macroLocation != null)
            {
                MacroLocation macroLocation = myLocation.macroLocation;
                MessageTrace(kind, macroLocation.Location, message);
                log.Message(Logger.MessageKind.NOTE, location, $"From inside of macro `{macroLocation.MacroName}`.");
            }
            else
            {
                string[] messages = message.Split('\n');
                log.Message(kind, location, messages[0]);

                for (int i = 1; i < messages.Length; i++)
                {
                    log.Message(Logger.MessageKind.CONTINUE, messages[i]);
                }
            }
        }

        // shorthand helpers

        public void Message(Location? location, string message) => MessageTrace(Logger.MessageKind.MESSAGE, location, message);
        public void Warning(Location? location, string message) => MessageTrace(Logger.MessageKind.WARNING, location, message);
        public void Error(Location? location, string message) => MessageTrace(Logger.MessageKind.ERROR, location, message);

        public void Message(string message) => MessageTrace(Logger.MessageKind.MESSAGE, head?.Location, message);
        public void Warning(string message) => MessageTrace(Logger.MessageKind.WARNING, head?.Location, message);
        public void Error(string message) => MessageTrace(Logger.MessageKind.ERROR, head?.Location, message);

        public void IgnoreRestOfStatement(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.SEMICOLON && tokens.MoveNext()) { }
            if (tokens.Current.Type == TokenType.SEMICOLON)
            {
                tokens.MoveNext();
            }
        }

        public void IgnoreRestOfLine(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.MoveNext()) { }
        }

        /// <summary>
        /// Consumes incoming tokens util end of line.
        /// </summary>
        /// <param name="tokens">token stream</param>
        /// <param name="scopesForMacros">If non-null, will expand any macros as they are encountered using this scope</param>
        /// <returns>The resulting list of tokens</returns>
        public IList<Token> GetRestOfLine(MergeableGenerator<Token> tokens, ImmutableStack<Closure>? scopesForMacros)
        {
            IList<Token> result = new List<Token>();

            while (tokens.Current.Type != TokenType.NEWLINE)
            {
                if (scopesForMacros == null || !ExpandIdentifier(tokens, scopesForMacros))
                {
                    result.Add(tokens.Current);
                    tokens.MoveNext();
                }
            }

            return result;
        }

        private string PrettyPrintParamsForMessage(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            return string.Join(" ", parameters.Select(parameter => parameter switch
            {
                StringNode node => ExpandUserFormatString(scopes, parameter.MyLocation, node.Value),
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

                Location itemLocation = baseLocation.OffsetBy(match.Index);

                MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(
                    Tokenizer.TokenizeLine($"{expr} \n", itemLocation));

                tokens.MoveNext();

                IAtomNode? node = this.ParseAtom(tokens, scopes);

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

        /*
         * =========================================
         * = NON STRICTLY PARSE RELATED START HERE =
         * =========================================
         */

        public int CurrentOffset
        {
            get => currentOffset;

            private set
            {
                if (value < 0 || value > EAOptions.MaximumBinarySize)
                {
                    if (validOffset) //Error only the first time.
                    {
                        Error($"Invalid offset: {value:X}");
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

        public ImmutableStack<Closure> GlobalScope { get; }

        private readonly Stack<(int, bool)> pastOffsets; // currentOffset, offsetInitialized
        private readonly IList<(int, int, Location)> protectedRegions;

        private bool validOffset;
        private bool offsetInitialized; // false until first ORG, used to warn about writing before first org 
        private int currentOffset;

        // TODO: these next two functions should probably be moved into their own module

        public static int ConvertToAddress(int value)
        {
            /*
                NOTE: Offset 0 is always converted to a null address
                If one wants to instead refer to ROM offset 0 they would want to use the address directly instead.
                If ROM offset 0 is already address 0 then this is a moot point.
            */

            if (value > 0 && value < EAOptions.MaximumBinarySize)
            {
                value += EAOptions.BaseAddress;
            }

            return value;
        }

        public static int ConvertToOffset(int value)
        {
            if (value >= EAOptions.BaseAddress && value <= EAOptions.BaseAddress + EAOptions.MaximumBinarySize)
            {
                value -= EAOptions.BaseAddress;
            }

            return value;
        }

        // Helper method for statement handlers
        private int? EvaluteAtom(IAtomNode node)
        {
            return node.TryEvaluate(e => Error(node.MyLocation, e.Message), EvaluationPhase.Immediate);
        }

        private ILineNode? HandleRawStatement(RawNode node)
        {
            if ((CurrentOffset % node.Raw.Alignment) != 0)
            {
                Error($"Bad alignment for raw {node.Raw.Name}: offseet ({CurrentOffset:X8}) needs to be {node.Raw.Alignment}-aligned.");
                return null;
            }
            else
            {
                // TODO: more efficient spacewise to just have contiguous writing and not an offset with every line?
                CheckDataWrite(node.Size);
                CurrentOffset += node.Size;

                return node;
            }
        }

        private ILineNode? HandleOrgStatement(IAtomNode offsetNode)
        {
            if (EvaluteAtom(offsetNode) is int offset)
            {
                CurrentOffset = ConvertToOffset(offset);
            }
            else
            {
                // EvaluateAtom already printed an error message
            }

            return null;
        }

        private ILineNode? HandlePushStatement()
        {
            pastOffsets.Push((CurrentOffset, offsetInitialized));
            return null;
        }

        private ILineNode? HandlePopStatement()
        {
            if (pastOffsets.Count == 0)
            {
                Error("POP without matching PUSH.");
            }
            else
            {
                (CurrentOffset, offsetInitialized) = pastOffsets.Pop();
            }

            return null;
        }

        private ILineNode? HandleAssertStatement(IAtomNode node)
        {
            // helper for distinguishing boolean expressions and other expressions
            // TODO: move elsewhere perhaps
            static bool IsBooleanResultHelper(IAtomNode node)
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

            bool isBoolean = IsBooleanResultHelper(node);

            if (EvaluteAtom(node) is int result)
            {
                if (isBoolean && result == 0)
                {
                    Error(node.MyLocation, "Assertion failed");
                }
                else if (!isBoolean && result < 0)
                {
                    Error(node.MyLocation, $"Assertion failed with value {result}.");
                }
            }
            else
            {
                Error("Failed to evaluate ASSERT expression.");
            }

            return null;
        }

        private ILineNode? HandleProtectStatement(IAtomNode beginAtom, IAtomNode? endAtom)
        {
            if (EvaluteAtom(beginAtom) is int beginValue)
            {
                beginValue = ConvertToAddress(beginValue);

                int length = 4;

                if (endAtom != null)
                {
                    if (EvaluteAtom(endAtom) is int endValue)
                    {
                        endValue = ConvertToAddress(endValue);

                        length = endValue - beginValue;

                        switch (length)
                        {
                            case < 0:
                                Error($"Invalid PROTECT region: end address ({endValue:X8}) is before start address ({beginValue:X8}).");
                                return null;

                            case 0:
                                // NOTE: does this need to be an error?
                                Error($"Empty PROTECT region: end address is equal to start address ({beginValue:X8}).");
                                return null;
                        }
                    }
                    else
                    {
                        // EvaluateAtom already printed an error message
                        return null;
                    }
                }

                protectedRegions.Add((beginValue, length, head!.Location));

                return null;
            }
            else
            {
                // EvaluateAtom already printed an error message
                return null;
            }
        }

        private ILineNode? HandleAlignStatement(IAtomNode alignNode, IAtomNode? offsetNode)
        {
            if (EvaluteAtom(alignNode) is int alignValue)
            {
                if (alignValue > 0)
                {
                    int alignOffset = 0;

                    if (offsetNode != null)
                    {
                        if (EvaluteAtom(offsetNode) is int rawOffset)
                        {
                            if (rawOffset >= 0)
                            {
                                alignOffset = ConvertToOffset(rawOffset) % alignValue;
                            }
                            else
                            {
                                Error($"ALIGN offset cannot be negative (got {rawOffset})");
                                return null;
                            }
                        }
                        else
                        {
                            // EvaluateAtom already printed an error message
                            return null;
                        }
                    }

                    if (CurrentOffset % alignValue != alignOffset)
                    {
                        CurrentOffset += alignValue - (CurrentOffset + alignValue - alignOffset) % alignValue;
                    }

                    return null;
                }
                else
                {
                    Error($"Invalid ALIGN value (got {alignValue}).");
                    return null;
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
                return null;
            }
        }

        private ILineNode? HandleFillStatement(IAtomNode amountNode, IAtomNode? valueNode)
        {
            if (EvaluteAtom(amountNode) is int amount)
            {
                if (amount > 0)
                {
                    int fillValue = 0;

                    if (valueNode != null)
                    {
                        if (EvaluteAtom(valueNode) is int rawValue)
                        {
                            fillValue = rawValue;
                        }
                        else
                        {
                            // EvaluateAtom already printed an error message
                            return null;
                        }
                    }

                    var data = new byte[amount];

                    for (int i = 0; i < amount; ++i)
                    {
                        data[i] = (byte)fillValue;
                    }

                    var node = new DataNode(CurrentOffset, data);

                    CheckDataWrite(amount);
                    CurrentOffset += amount;

                    return node;
                }
                else
                {
                    Error($"Invalid FILL amount (got {amount}).");
                    return null;
                }
            }
            else
            {
                // EvaluateAtom already printed an error message
                return null;
            }
        }

        private ILineNode? HandleSymbolAssignment(string name, IAtomNode atom, ImmutableStack<Closure> scopes)
        {
            if (atom.TryEvaluate(_ => { }, EvaluationPhase.Early) is int value)
            {
                TryDefineSymbol(scopes, name, value);
            }
            else
            {
                TryDefineSymbol(scopes, name, atom);
            }

            return null;
        }

        private ILineNode? HandleLabel(string name, ImmutableStack<Closure> scopes)
        {
            TryDefineSymbol(scopes, name, ConvertToAddress(CurrentOffset));
            return null;
        }

        public bool IsValidLabelName(string name)
        {
            // TODO: this could be where checks for CURRENTOFFSET, __LINE__ and __FILE__ are?
            return true; // !IsReservedName(name);
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

        // Return value: Location where protection occurred. Nothing if location was not protected.
        private Location? IsProtected(int offset, int length)
        {
            int address = ConvertToAddress(offset);

            foreach ((int protectedAddress, int protectedLength, Location location) in protectedRegions)
            {
                /* They intersect if the last offset in the given region is after the start of this one
                 * and the first offset in the given region is before the last of this one. */

                if (address + length > protectedAddress && address < protectedAddress + protectedLength)
                {
                    return location;
                }
            }

            return null;
        }

        private void CheckDataWrite(int length)
        {
            if (!offsetInitialized)
            {
                if (EAOptions.IsWarningEnabled(EAOptions.Warnings.UninitializedOffset))
                {
                    Warning("Writing before initializing offset. You may be breaking the ROM! (use `ORG offset` to set write offset).");
                }

                offsetInitialized = false; // only warn once
            }

            if (IsProtected(CurrentOffset, length) is Location prot)
            {
                Error($"Trying to write data to area protected by {prot}");
            }
        }
    }
}

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

        public Logger Logger { get; }

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

        public EAParseConsumer ParseConsumer { get; }

        public EAParser(Logger log, Dictionary<string, IList<Raw>> raws, DirectiveHandler directiveHandler, EAParseConsumer parseConsumer)
        {
            Logger = log;
            Raws = raws;
            Macros = new MacroCollection(this);
            Definitions = new Dictionary<string, Definition>();
            Inclusion = ImmutableStack<bool>.Nil;
            DirectiveHandler = directiveHandler;
            ParseConsumer = parseConsumer;
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
                    ILineNode? retVal = ParseLine(tokens, ParseConsumer.GlobalScope);
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
                Logger.Error(start, "Didn't find matching brace.");
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
                    return ParseConsumer.HandleLabel(head.Location, head.Content, scopes);

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
                return ParseConsumer.HandleSymbolAssignment(head!.Location, name, atom, scopes);
            }
            else
            {
                Logger.Error(head!.Location, $"Couldn't define symbol `{name}`: exprected expression.");
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
                        return ParseConsumer.HandleRawStatement(new RawNode(raw, head!, ParseConsumer.CurrentOffset, parameters));
                    }
                }

                if (raws.Count == 1)
                {
                    Logger.Error(head!.Location, $"Incorrect parameters in raw `{raws[0].ToPrettyString()}`");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append($"Couldn't find suitable variant of raw `{head!.Content}`.");

                    for (int i = 0; i < raws.Count; i++)
                    {
                        sb.Append($"\nVariant {i + 1}: `{raws[i].ToPrettyString()}`");
                    }

                    Logger.Error(head!.Location, sb.ToString());
                }

                IgnoreRestOfStatement(tokens);
                return null;
            }
            else
            {
                Logger.Error(head!.Location, $"Unrecognized statement code: {head!.Content}");
                return null;
            }
        }

        private ILineNode? ParseOrgStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementOneParam("ORG", parameters, ParseConsumer.HandleOrgStatement);
        }

        private ILineNode? ParsePushStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            if (parameters.Count == 0)
            {
                return ParseConsumer.HandlePushStatement(head!.Location);
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
                return ParseConsumer.HandlePopStatement(head!.Location);
            }
            else
            {
                return StatementExpectsParamCount("POP", parameters, 0, 0);
            }
        }

        private ILineNode? ParseAssertStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementOneParam("ASSERT", parameters, ParseConsumer.HandleAssertStatement);
        }

        // Helper method for printing errors
        private ILineNode? StatementExpectsAtom(string statementName, IParamNode param)
        {
            Logger.Error(param.MyLocation,
                $"{statementName} expects an Atom (got {DiagnosticsHelpers.PrettyParamType(param.Type)}).");

            return null;
        }

        // Helper method for printing errors
        private ILineNode? StatementExpectsParamCount(string statementName, IList<IParamNode> parameters, int min, int max)
        {
            if (min == max)
            {
                Logger.Error(head!.Location, $"A {statementName} statement expects {min} parameters, got {parameters.Count}.");
            }
            else
            {
                Logger.Error(head!.Location, $"A {statementName} statement expects {min} to {max} parameters, got {parameters.Count}.");
            }

            return null;
        }

        private delegate ILineNode? HandleStatementOne(Location location, IAtomNode node);
        private delegate ILineNode? HandleStatementTwo(Location location, IAtomNode firstNode, IAtomNode? optionalSecondNode);

        private ILineNode? ParseStatementOneParam(string name, IList<IParamNode> parameters, HandleStatementOne handler)
        {
            if (parameters.Count == 1)
            {
                if (parameters[0] is IAtomNode expr)
                {
                    return handler(head!.Location, expr);
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
                    return handler(head!.Location, firstNode, null);
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
                        return handler(head!.Location, firstNode, secondNode);
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
            return ParseStatementTwoParam("PROTECT", parameters, ParseConsumer.HandleProtectStatement);
        }

        private ILineNode? ParseAlignStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementTwoParam("ALIGN", parameters, ParseConsumer.HandleAlignStatement);
        }

        private ILineNode? ParseFillStatement(IList<IParamNode> parameters, ImmutableStack<Closure> _)
        {
            return ParseStatementTwoParam("FILL", parameters, ParseConsumer.HandleFillStatement);
        }

        private ILineNode? ParseMessageStatement(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            Logger.Message(head!.Location, PrettyPrintParamsForMessage(parameters, scopes));
            return null;
        }

        private ILineNode? ParseWarningStatement(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            Logger.Warning(head!.Location, PrettyPrintParamsForMessage(parameters, scopes));
            return null;
        }

        private ILineNode? ParseErrorStatement(IList<IParamNode> parameters, ImmutableStack<Closure> scopes)
        {
            Logger.Error(head!.Location, PrettyPrintParamsForMessage(parameters, scopes));
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
                Logger.Error(tokens.Current.Location, "Unmatched open parenthesis.");
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
                    () => Logger.Error(localHead.Location, "Expected parameter."));
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
                    () => Logger.Error(tokens.Current.Location, "Expected atomic value, got " + tokens.Current.Type + "."));
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
                Logger.Error(localHead.Location, "Unmatched open bracket.");
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
                                    Logger.Error(head.Location, $"Expansion of macro `{head.Content}` did not result in a valid statement. Did you perhaps attempt to define a label or symbol with that name?");
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
                        Logger.Error(head.Location, "Unexpected list literal.");
                        IgnoreRestOfLine(tokens);
                        break;

                    case TokenType.NUMBER:
                    case TokenType.OPEN_PAREN:
                        Logger.Error(head.Location, "Unexpected mathematical expression.");
                        IgnoreRestOfLine(tokens);
                        break;

                    default:
                        tokens.MoveNext();

                        if (string.IsNullOrEmpty(head.Content))
                        {
                            Logger.Error(head.Location, $"Unexpected token: {head.Type}.");
                        }
                        else
                        {
                            Logger.Error(head.Location, $"Unexpected token: {head.Type}: {head.Content}.");
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
                    Logger.Error(null, $"Missing {Inclusion.Count} endif(s).");
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
                ParseConsumer.HandlePreprocessorLineNode(head.Location, result);
            }

            return null;
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
                        Logger.Error(localHead.Location, $"No overload of {localHead.Content} with {parameters.Count} parameters.");
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
                    token => Logger.Warning(token.Location, $"Unguarded expansion of mathematical operator. Consider adding guarding parenthesises around definition."));

                tokens.PrependEnumerator(expandedList.GetEnumerator());
            }
            else
            {
                tokens.PrependEnumerator(expandedTokens.GetEnumerator());
            }
        }

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
            string UserFormatStringError(Location loc, string message, string details)
            {
                Logger.Error(loc, $"An error occurred while expanding format string ({message}).");
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
                    return UserFormatStringError(itemLocation, "bad expression", $"'{expr}'");
                }

                if (node.TryEvaluate(e => Logger.Error(node.MyLocation, e.Message),
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
                        return UserFormatStringError(node.MyLocation, "bad format specifier", $"'{format}' ({e.Message})");
                    }
                }
                else
                {
                    return UserFormatStringError(node.MyLocation, "failed to evaluate expression", $"'{expr}'");
                }
            });
        }
    }
}

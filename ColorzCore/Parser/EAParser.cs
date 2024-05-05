using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Interpreter.Diagnostics;
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

        public delegate IAtomNode BindIdentifierFunc(Token identifierToken);

        public IParseConsumer ParseConsumer { get; }

        // TODO: IParseContextProvider or something like that?
        public BindIdentifierFunc BindIdentifier { get; }
        public StringProcessor StringProcessor { get; }

        public EAParser(Logger log, Dictionary<string, IList<Raw>> raws, IParseConsumer parseConsumer, BindIdentifierFunc bindIdentifier, StringProcessor stringProcessor)
        {
            Logger = log;
            Raws = raws;
            Macros = new MacroCollection(this);
            Definitions = new Dictionary<string, Definition>();
            Inclusion = ImmutableStack<bool>.Nil;
            DirectiveHandler = new DirectiveHandler();
            ParseConsumer = parseConsumer;
            BindIdentifier = bindIdentifier;
            StringProcessor = stringProcessor;
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

        public void ParseAll(IEnumerable<Token> tokenStream)
        {
            MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(tokenStream);
            tokens.MoveNext();

            while (!tokens.EOS)
            {
                if (tokens.Current.Type != TokenType.NEWLINE || tokens.MoveNext())
                {
                    ParseLine(tokens);
                }
            }
        }

        public static readonly HashSet<string> SpecialCodes = new HashSet<string>()
        {
            "ORG",
            "PUSH",
            "POP",
            "MESSAGE",
            "WARNING",
            "ERROR",
            "ASSERT",
            "PROTECT",
            "ALIGN",
            "FILL",
            "STRING",
            "BASE64",
            // "SECTION", // TODO
            // "DSECTION", // TODO
        };

        private void ParseStatement(MergeableGenerator<Token> tokens)
        {
            // NOTE: here previously lied en ExpandIdentifier loop
            // though because this is only called from ParseLine after the corresponding check, this is not needed

            Token head = tokens.Current;
            tokens.MoveNext();

            switch (tokens.Current.Type)
            {
                case TokenType.COLON:
                    tokens.MoveNext();
                    ParseConsumer.OnLabel(head.Location, head.Content);
                    return;

                case TokenType.ASSIGN:
                    tokens.MoveNext();
                    ParseAssignment(head, head.Content, tokens);
                    return;
            }

            // NOTE: those remarks are old ones from Colorz, idrk what they mean -Stan
            // TODO: Replace with real raw information, and error if not valid.
            // TODO: Make intelligent to reject malformed parameters.
            // TODO: Parse parameters after checking code validity.

            IList<IParamNode> parameters = tokens.Current.Type switch
            {
                TokenType.NEWLINE or TokenType.SEMICOLON => new List<IParamNode>(),
                _ => ParseParamList(tokens),
            };

            string upperCodeIdentifier = head.Content.ToUpperInvariant();

            if (SpecialCodes.Contains(upperCodeIdentifier))
            {
                switch (upperCodeIdentifier)
                {
                    case "ORG":
                        ParseOrgStatement(head, parameters);
                        break;

                    case "PUSH":
                        ParsePushStatement(head, parameters);
                        break;

                    case "POP":
                        ParsePopStatement(head, parameters);
                        break;

                    case "ASSERT":
                        ParseAssertStatement(head, parameters);
                        break;

                    case "PROTECT":
                        ParseProtectStatement(head, parameters);
                        break;

                    case "ALIGN":
                        ParseAlignStatement(head, parameters);
                        break;

                    case "FILL":
                        ParseFillStatement(head, parameters);
                        break;

                    case "MESSAGE":
                        ParseMessageStatement(head, parameters);
                        break;

                    case "WARNING":
                        ParseWarningStatement(head, parameters);
                        break;

                    case "ERROR":
                        ParseErrorStatement(head, parameters);
                        break;

                    case "STRING":
                        ParseStringStatement(head, parameters);
                        break;

                    case "BASE64":
                        ParseBase64Statement(head, parameters);
                        break;
                }
            }
            else
            {
                ParseRawStatement(head, upperCodeIdentifier, tokens, parameters);
            }
        }

        private void ParseAssignment(Token head, string name, MergeableGenerator<Token> tokens)
        {
            IAtomNode? atom = this.ParseAtom(tokens);

            if (atom != null)
            {
                ParseConsumer.OnSymbolAssignment(head.Location, name, atom);
            }
            else
            {
                Logger.Error(head.Location, $"Couldn't define symbol `{name}`: exprected expression.");
            }
        }

        private void ParseRawStatement(Token head, string upperCodeIdentifier, MergeableGenerator<Token> tokens, IList<IParamNode> parameters)
        {
            if (Raws.TryGetValue(upperCodeIdentifier, out IList<Raw>? raws))
            {
                // find the raw matching with parameters
                foreach (Raw raw in raws)
                {
                    if (raw.Fits(parameters))
                    {
                        ParseConsumer.OnRawStatement(head.Location, raw, parameters);
                        return;
                    }
                }

                if (raws.Count == 1)
                {
                    Logger.Error(head.Location, $"Incorrect parameters in raw `{raws[0].ToPrettyString()}`");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    sb.Append($"Couldn't find suitable variant of raw `{upperCodeIdentifier}`.");

                    for (int i = 0; i < raws.Count; i++)
                    {
                        sb.Append($"\nVariant {i + 1}: `{raws[i].ToPrettyString()}`");
                    }

                    Logger.Error(head.Location, sb.ToString());
                }

                IgnoreRestOfStatement(tokens);
            }
            else
            {
                Logger.Error(head.Location, $"Unrecognized statement code: {upperCodeIdentifier}");
            }
        }

        private void ParseOrgStatement(Token head, IList<IParamNode> parameters)
        {
            ParseStatementOneParam(head, "ORG", parameters, ParseConsumer.OnOrgStatement);
        }

        private void ParsePushStatement(Token head, IList<IParamNode> parameters)
        {
            if (parameters.Count == 0)
            {
                ParseConsumer.OnPushStatement(head.Location);
            }
            else
            {
                StatementExpectsParamCount(head, "PUSH", parameters, 0, 0);
            }
        }

        private void ParsePopStatement(Token head, IList<IParamNode> parameters)
        {
            if (parameters.Count == 0)
            {
                ParseConsumer.OnPopStatement(head.Location);
            }
            else
            {
                StatementExpectsParamCount(head, "POP", parameters, 0, 0);
            }
        }

        private void ParseAssertStatement(Token head, IList<IParamNode> parameters)
        {
            ParseStatementOneParam(head, "ASSERT", parameters, ParseConsumer.OnAssertStatement);
        }

        // Helper method for printing errors
        private void StatementExpectsAtom(string statementName, IParamNode param)
        {
            Logger.Error(param.MyLocation,
                $"{statementName} expects an Atom (got {DiagnosticsHelpers.PrettyParamType(param.Type)}).");
        }

        // Helper method for printing errors
        private void StatementExpectsParamCount(Token head, string statementName, IList<IParamNode> parameters, int min, int max)
        {
            if (min == max)
            {
                Logger.Error(head.Location, $"A {statementName} statement expects {min} parameters, got {parameters.Count}.");
            }
            else
            {
                Logger.Error(head.Location, $"A {statementName} statement expects {min} to {max} parameters, got {parameters.Count}.");
            }
        }

        private delegate void HandleStatementOne(Location location, IAtomNode node);
        private delegate void HandleStatementTwo(Location location, IAtomNode firstNode, IAtomNode? optionalSecondNode);

        private void ParseStatementOneParam(Token head, string name, IList<IParamNode> parameters, HandleStatementOne handler)
        {
            if (parameters.Count == 1)
            {
                if (parameters[0] is IAtomNode expr)
                {
                    handler(head.Location, expr);
                }
                else
                {
                    StatementExpectsAtom(name, parameters[0]);
                }
            }
            else
            {
                StatementExpectsParamCount(head, name, parameters, 1, 1);
            }
        }

        private void ParseStatementTwoParam(Token head, string name, IList<IParamNode> parameters, HandleStatementTwo handler)
        {
            if (parameters.Count == 1)
            {
                if (parameters[0] is IAtomNode firstNode)
                {
                    handler(head.Location, firstNode, null);
                }
                else
                {
                    StatementExpectsAtom(name, parameters[0]);
                }
            }
            else if (parameters.Count == 2)
            {
                if (parameters[0] is IAtomNode firstNode)
                {
                    if (parameters[1] is IAtomNode secondNode)
                    {
                        handler(head.Location, firstNode, secondNode);
                    }
                    else
                    {
                        StatementExpectsAtom(name, parameters[1]);
                    }
                }
                else
                {
                    StatementExpectsAtom(name, parameters[0]);
                }
            }
            else
            {
                StatementExpectsParamCount(head, name, parameters, 1, 2);
            }
        }

        private void ParseProtectStatement(Token head, IList<IParamNode> parameters)
        {
            ParseStatementTwoParam(head, "PROTECT", parameters, ParseConsumer.OnProtectStatement);
        }

        private void ParseAlignStatement(Token head, IList<IParamNode> parameters)
        {
            ParseStatementTwoParam(head, "ALIGN", parameters, ParseConsumer.OnAlignStatement);
        }

        private void ParseFillStatement(Token head, IList<IParamNode> parameters)
        {
            ParseStatementTwoParam(head, "FILL", parameters, ParseConsumer.OnFillStatement);
        }

        private void ParseMessageStatement(Token head, IList<IParamNode> parameters)
        {
            Logger.Message(head.Location, PrettyPrintParamsForMessage(parameters));
        }

        private void ParseWarningStatement(Token head, IList<IParamNode> parameters)
        {
            Logger.Warning(head.Location, PrettyPrintParamsForMessage(parameters));
        }

        private void ParseErrorStatement(Token head, IList<IParamNode> parameters)
        {
            Logger.Error(head.Location, PrettyPrintParamsForMessage(parameters));
        }

        private void ParseStringStatement(Token head, IList<IParamNode> parameters)
        {
            void HandleStringStatement(Token head, StringNode node, string? encodingName)
            {
                string formattedString = StringProcessor.ExpandUserFormatString(
                    node.MyLocation, this, node.Value);

                byte[] encodedString = StringProcessor.EncodeString(
                    head.Location, formattedString, encodingName);

                if (encodedString.Length != 0)
                {
                    ParseConsumer.OnData(head.Location, encodedString);
                }
            }

            // NOTE: this is copy-pasted from ParseStatementTwoParam but adjusted for string param

            if (parameters.Count == 1)
            {
                if (parameters[0] is StringNode firstNode)
                {
                    HandleStringStatement(head, firstNode, null);
                }
                else
                {
                    Logger.Error(parameters[0].MyLocation,
                        $"STRING expects a String (got {DiagnosticsHelpers.PrettyParamType(parameters[0].Type)}).");
                }
            }
            else if (parameters.Count == 2)
            {
                if (parameters[0] is StringNode firstNode)
                {
                    if (parameters[1] is StringNode secondNode)
                    {
                        HandleStringStatement(head, firstNode, secondNode.Value);
                    }
                    else
                    {
                        Logger.Error(parameters[1].MyLocation,
                            $"STRING expects a String (got {DiagnosticsHelpers.PrettyParamType(parameters[1].Type)}).");
                    }
                }
                else
                {
                    Logger.Error(parameters[0].MyLocation,
                        $"STRING expects a String (got {DiagnosticsHelpers.PrettyParamType(parameters[0].Type)}).");
                }
            }
            else
            {
                Logger.Error(head.Location,
                    $"A STRING statement expects 1 to 2 parameters, got {parameters.Count}.");
            }
        }

        private void ParseBase64Statement(Token head, IList<IParamNode> parameters)
        {
            using MemoryStream memoryStream = new MemoryStream();

            foreach (IParamNode parameter in parameters)
            {
                if (parameter is StringNode node)
                {
                    try
                    {
                        byte[] base64Bytes = Convert.FromBase64String(node.Value);
                        memoryStream.Write(base64Bytes, 0, base64Bytes.Length);
                    }
                    catch (FormatException e)
                    {
                        Logger.Error(node.MyLocation, $"Failed to parse Base64 string: {e.Message}");
                        return;
                    }
                }
                else
                {
                    Logger.Error(head.Location, $"expects a String (got {DiagnosticsHelpers.PrettyParamType(parameter.Type)}).");
                }
            }

            byte[] bytes = memoryStream.ToArray();
            ParseConsumer.OnData(head.Location, bytes);
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

        private IList<IParamNode> ParseParamList(MergeableGenerator<Token> tokens)
        {
            IList<IParamNode> paramList = new List<IParamNode>();

            while (!IsTokenAlwaysPastEndOfStatement(tokens.Current) && !tokens.EOS)
            {
                Token localHead = tokens.Current;
                ParseParam(tokens).IfJust(
                    n => paramList.Add(n),
                    () => Logger.Error(localHead.Location, "Expected parameter."));
            }

            if (tokens.Current.Type == TokenType.SEMICOLON)
            {
                tokens.MoveNext();
            }

            return paramList;
        }

        private static readonly Regex idRegex = new Regex("^([a-zA-Z_][a-zA-Z0-9_]*)$");

        public IList<IParamNode> ParsePreprocParamList(MergeableGenerator<Token> tokens)
        {
            static bool IsValidIdentifier(string value)
            {
                return idRegex.IsMatch(value);
            }

            IList<IParamNode> temp = ParseParamList(tokens);

            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i] is StringNode stringNode && IsValidIdentifier(stringNode.Value))
                {
                    // TODO: what is this for? can we omit it?
                    temp[i] = BindIdentifier(stringNode.SourceToken);
                }
            }

            return temp;
        }

        public IParamNode? ParseParam(MergeableGenerator<Token> tokens)
        {
            Token localHead = tokens.Current;
            switch (localHead.Type)
            {
                case TokenType.OPEN_BRACKET:
                    return new ListNode(localHead.Location, ParseList(tokens));
                case TokenType.STRING:
                    tokens.MoveNext();
                    return new StringNode(localHead);
                case TokenType.MAYBE_MACRO:
                    //TODO: Move this and the one in ExpandId to a separate ParseMacroNode that may return an Invocation.
                    if (ExpandIdentifier(tokens, true))
                    {
                        return ParseParam(tokens);
                    }
                    else
                    {
                        tokens.MoveNext();
                        IList<IList<Token>> param = ParseMacroParamList(tokens);
                        //TODO: Smart errors if trying to redefine a macro with the same num of params.
                        return new MacroInvocationNode(this, localHead, param);
                    }
                case TokenType.IDENTIFIER:
                    if (ExpandIdentifier(tokens, true))
                    {
                        return ParseParam(tokens);
                    }
                    else
                    {
                        switch (localHead.Content.ToUpperInvariant())
                        {
                            case "__FILE__":
                                tokens.MoveNext();
                                return new StringNode(new Token(TokenType.STRING, localHead.Location, localHead.GetSourceLocation().file));

                            default:
                                return this.ParseAtom(tokens);
                        }
                    }

                default:
                    return this.ParseAtom(tokens);
            }
        }

        private IList<IAtomNode> ParseList(MergeableGenerator<Token> tokens)
        {
            Token localHead = tokens.Current;
            tokens.MoveNext();

            IList<IAtomNode> atoms = new List<IAtomNode>();
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.Current.Type != TokenType.CLOSE_BRACKET)
            {
                IAtomNode? res = this.ParseAtom(tokens);
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

        public void ParseLine(MergeableGenerator<Token> tokens)
        {
            if (IsIncluding)
            {
                if (tokens.Current.Type == TokenType.NEWLINE || tokens.Current.Type == TokenType.SEMICOLON)
                {
                    tokens.MoveNext();
                    return;
                }

                Token head = tokens.Current;
                switch (head.Type)
                {
                    case TokenType.IDENTIFIER:
                    case TokenType.MAYBE_MACRO:
                        if (ExpandIdentifier(tokens))
                        {
                            // NOTE: we check here if we didn't end up with something that can't be a statement

                            switch (tokens.Current.Type)
                            {
                                case TokenType.IDENTIFIER:
                                case TokenType.MAYBE_MACRO:
                                case TokenType.OPEN_BRACE:
                                case TokenType.PREPROCESSOR_DIRECTIVE:
                                    // recursion!
                                    ParseLine(tokens);
                                    return;

                                default:
                                    // it is somewhat common for users to do '#define Foo 0xABCD' and then later 'Foo:'
                                    Logger.Error(head.Location, $"Expansion of macro `{head.Content}` did not result in a valid statement. Did you perhaps attempt to define a label or symbol with that name?");
                                    IgnoreRestOfStatement(tokens);
                                    break;
                            }

                            return;
                        }
                        else
                        {
                            ParseStatement(tokens);
                        }

                        break;

                    case TokenType.OPEN_BRACE:
                        tokens.MoveNext();
                        ParseConsumer.OnOpenScope(head.Location);
                        break;

                    case TokenType.CLOSE_BRACE:
                        tokens.MoveNext();
                        ParseConsumer.OnCloseScope(head.Location);
                        break;

                    case TokenType.PREPROCESSOR_DIRECTIVE:
                        ParsePreprocessor(tokens);
                        break;

                    case TokenType.OPEN_BRACKET:
                        Logger.Error(head.Location, "Unexpected list literal.");
                        IgnoreRestOfStatement(tokens);
                        break;

                    case TokenType.NUMBER:
                    case TokenType.OPEN_PAREN:
                        Logger.Error(head.Location, "Unexpected mathematical expression.");
                        IgnoreRestOfStatement(tokens);
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

                        IgnoreRestOfStatement(tokens);
                        break;
                }
            }
            else
            {
                bool hasNext = true;

                while (tokens.Current.Type != TokenType.PREPROCESSOR_DIRECTIVE && (hasNext = tokens.MoveNext()))
                {
                }

                if (hasNext)
                {
                    ParsePreprocessor(tokens);
                }
                else
                {
                    Logger.Error(null, $"Missing {Inclusion.Count} endif(s).");
                }
            }
        }

        private void ParsePreprocessor(MergeableGenerator<Token> tokens)
        {
            Token head = tokens.Current;
            tokens.MoveNext();
            DirectiveHandler.HandleDirective(this, head, tokens);
        }

        /***
         *   Precondition: tokens.Current.Type == TokenType.IDENTIFIER || MAYBE_MACRO
         *   Postcondition: tokens.Current is fully reduced (i.e. not a macro, and not a definition)
         *   Returns: true iff tokens was actually expanded.
         */
        public bool ExpandIdentifier(MergeableGenerator<Token> tokens, bool insideExpression = false)
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

                        ApplyMacroExpansion(tokens, macro!.ApplyMacro(localHead, parameters), insideExpression);
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
            while (!IsTokenAlwaysPastEndOfStatement(tokens.Current) && tokens.MoveNext())
            {
            }

            if (tokens.Current.Type == TokenType.SEMICOLON)
            {
                tokens.MoveNext();
            }
        }

        public void IgnoreRestOfLine(MergeableGenerator<Token> tokens)
        {
            while (tokens.Current.Type != TokenType.NEWLINE && tokens.MoveNext())
            {
            }
        }

        /// <summary>
        /// Consumes incoming tokens util end of line.
        /// </summary>
        /// <param name="tokens">token stream</param>
        /// <param name="scopesForMacros">If non-null, will expand any macros as they are encountered using this scope</param>
        /// <returns>The resulting list of tokens</returns>
        public IList<Token> GetRestOfLine(MergeableGenerator<Token> tokens)
        {
            IList<Token> result = new List<Token>();

            while (tokens.Current.Type != TokenType.NEWLINE)
            {
                if (!ExpandIdentifier(tokens))
                {
                    result.Add(tokens.Current);
                    tokens.MoveNext();
                }
            }

            return result;
        }

        private static bool IsTokenAlwaysPastEndOfStatement(Token token) => token.Type switch
        {
            TokenType.NEWLINE => true,
            TokenType.SEMICOLON => true,
            TokenType.OPEN_BRACE => true,
            TokenType.CLOSE_BRACE => true,
            _ => false,
        };

        private string PrettyPrintParamsForMessage(IList<IParamNode> parameters)
        {
            return string.Join(" ", parameters.Select(parameter => parameter switch
            {
                StringNode node => StringProcessor.ExpandUserFormatString(parameter.MyLocation, this, node.Value),
                _ => parameter.PrettyPrint(),
            }));
        }
    }
}

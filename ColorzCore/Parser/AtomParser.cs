using System.Collections.Generic;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter.Diagnostics;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;

namespace ColorzCore.Parser
{
    public static class AtomParser
    {
        private static readonly IDictionary<TokenType, int> precedences = new Dictionary<TokenType, int> {
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

        public static bool IsInfixOperator(Token token) => precedences.ContainsKey(token.Type);

        public static IAtomNode? ParseAtom(this EAParser self, MergeableGenerator<Token> tokens)
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
                                self.Reduce(grammarSymbols, precedence);
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
                                IAtomNode? interior = self.ParseAtom(tokens);
                                if (tokens.Current.Type != TokenType.CLOSE_PAREN)
                                {
                                    self.Logger.Error(tokens.Current.Location, "Unmatched open parenthesis (currently at " + tokens.Current.Type + ").");
                                    return null;
                                }
                                else if (interior == null)
                                {
                                    self.Logger.Error(lookAhead.Location, "Expected expression inside paretheses. ");
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
                                IAtomNode? interior = self.ParseAtom(tokens);
                                if (interior == null)
                                {
                                    self.Logger.Error(lookAhead.Location, "Expected expression after unary operator.");
                                    return null;
                                }
                                grammarSymbols.Push(new Left<IAtomNode, Token>(new UnaryOperatorNode(lookAhead, interior)));
                                break;
                            }
                        case TokenType.COMMA:
                            self.Logger.Error(lookAhead.Location, "Unexpected comma (perhaps unrecognized macro invocation?).");
                            self.IgnoreRestOfStatement(tokens);
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
                            self.Logger.Error(lookAhead.Location, $"Expected identifier or literal, got {lookAhead.Type}: {lookAhead.Content}.");
                            self.IgnoreRestOfStatement(tokens);
                            return null;
                    }
                }

                if (shift)
                {
                    switch (lookAhead.Type)
                    {
                        case TokenType.IDENTIFIER:
                            if (self.ExpandIdentifier(tokens, true))
                            {
                                continue;
                            }

                            grammarSymbols.Push(new Left<IAtomNode, Token>(lookAhead.Content.ToUpperInvariant() switch
                            {
                                "__LINE__" => new NumberNode(lookAhead, lookAhead.GetSourceLocation().line),
                                _ => self.BindIdentifier(lookAhead),
                            }));

                            break;

                        case TokenType.MAYBE_MACRO:
                            self.ExpandIdentifier(tokens, true);
                            continue;
                        case TokenType.NUMBER:
                            grammarSymbols.Push(new Left<IAtomNode, Token>(new NumberNode(lookAhead)));
                            break;
                        case TokenType.ERROR:
                            self.Logger.Error(lookAhead.Location, $"Unexpected token: {lookAhead.Content}");
                            tokens.MoveNext();
                            return null;
                        default:
                            grammarSymbols.Push(new Right<IAtomNode, Token>(lookAhead));
                            break;
                    }
                    tokens.MoveNext();
                    continue;
                }
            }
            while (grammarSymbols.Count > 1)
            {
                self.Reduce(grammarSymbols, int.MaxValue);
            }
            if (grammarSymbols.Peek().IsRight)
            {
                self.Logger.Error(grammarSymbols.Peek().GetRight.Location, $"Unexpected token: {grammarSymbols.Peek().GetRight.Type}");
            }
            return grammarSymbols.Peek().GetLeft;
        }

        /***
         *   Precondition: grammarSymbols alternates between IAtomNodes, operator Tokens, .Count is odd
         *                 the precedences of the IAtomNodes is increasing.
         *   Postcondition: Either grammarSymbols.Count == 1, or everything in grammarSymbols will have precedence <= targetPrecedence.
         *
         */
        private static void Reduce(this EAParser self, Stack<Either<IAtomNode, Token>> grammarSymbols, int targetPrecedence)
        {
            while (grammarSymbols.Count > 1)// && grammarSymbols.Peek().GetLeft.Precedence > targetPrecedence)
            {
                // These shouldn't error...
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

                    OperatorNode operatorNode = new OperatorNode(l, op, r, l.Precedence);

                    if (EAOptions.IsWarningEnabled(EAOptions.Warnings.UnintuitiveExpressionMacros))
                    {
                        if (DiagnosticsHelpers.DoesOperationSpanMultipleMacrosUnintuitively(operatorNode))
                        {
                            MacroLocation? mloc = operatorNode.MyLocation.macroLocation;
                            string message = DiagnosticsHelpers.GetEmphasizedExpression(operatorNode, l => l.macroLocation == mloc);

                            if (mloc != null)
                            {
                                message += $"\nUnintuitive expression resulting from expansion of macro `{mloc.MacroName}`.";
                            }
                            else
                            {
                                message += "\nUnintuitive expression resulting from expansion of macro.";
                            }

                            message += "\nConsider guarding your expressions using parenthesis.";

                            self.Logger.Warning(operatorNode.MyLocation, message);
                        }
                    }

                    grammarSymbols.Push(new Left<IAtomNode, Token>(operatorNode));
                }
            }
        }
    }
}

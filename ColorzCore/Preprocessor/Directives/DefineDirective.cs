using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor.Macros;

namespace ColorzCore.Preprocessor.Directives
{
    class DefineDirective : SimpleDirective
    {
        public override int MinParams => 1;

        public override int? MaxParams => 2;

        public override bool RequireInclusion => true;

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            if (parameters[0] is MacroInvocationNode signature)
            {
                string name = signature.Name;
                IList<Token> myParams = new List<Token>();
                foreach (IList<Token> l1 in signature.Parameters)
                {
                    if (l1.Count != 1 || l1[0].Type != TokenType.IDENTIFIER)
                    {
                        p.Error(l1[0].Location, $"Macro parameters must be identifiers (got {l1[0].Content}).");
                    }
                    else
                    {
                        myParams.Add(l1[0]);
                    }
                }
                /* if (!p.IsValidMacroName(name, myParams.Count))
                {
                    if (p.IsReservedName(name))
                    {
                        p.Error(signature.MyLocation, "Invalid redefinition: " + name);
                    }
                    else
                        p.Warning(signature.MyLocation, "Redefining " + name + '.');
                }*/
                if (p.Macros.HasMacro(name, myParams.Count))
                    p.Warning(signature.MyLocation, $"Redefining {name}.");
                IList<Token>? toRepl;
                if (parameters.Count != 2)
                {
                    toRepl = new List<Token>();
                }
                else
                    toRepl = ExpandParam(p, parameters[1], myParams.Select((Token t) => t.Content));
                if (toRepl != null)
                {
                    p.Macros.AddMacro(new Macro(myParams, toRepl), name, myParams.Count);
                }
            }
            else
            {
                //Note [mutually] recursive definitions are handled by Parser expansion.
                string? name;
                if (parameters[0].Type == ParamType.ATOM && (name = ((IAtomNode)parameters[0]).GetIdentifier()) != null)
                {
                    if (p.Definitions.ContainsKey(name))
                        p.Warning(parameters[0].MyLocation, "Redefining " + name + '.');
                    if (parameters.Count == 2)
                    {
                        IList<Token>? toRepl = ExpandParam(p, parameters[1], Enumerable.Empty<string>());
                        if (toRepl != null)
                        {
                            p.Definitions[name] = new Definition(toRepl);
                        }
                    }
                    else
                    {
                        p.Definitions[name] = new Definition();
                    }
                }
                else
                {
                    p.Error(parameters[0].MyLocation, $"Definition names must be identifiers (got {parameters[0].ToString()}).");
                }
            }
            return null;
        }

        private static IList<Token>? ExpandParam(EAParser p, IParamNode param, IEnumerable<string> myParams)
        {
            return TokenizeParam(p, param).Fmap(tokens => ExpandAllIdentifiers(p,
                new Queue<Token>(tokens), ImmutableStack<string>.FromEnumerable(myParams),
                ImmutableStack<Tuple<string, int>>.Nil)).Fmap(x => new List<Token>(x));
        }

        private static IList<Token>? TokenizeParam(EAParser p, IParamNode param)
        {
            switch (param.Type)
            {
                case ParamType.STRING:
                    Token input = ((StringNode)param).MyToken;
                    Tokenizer t = new Tokenizer();
                    return new List<Token>(t.TokenizeLine(input.Content, input.FileName, input.LineNumber, input.ColumnNumber));
                case ParamType.MACRO:
                    try
                    {
                        IList<Token> myBody = new List<Token>(((MacroInvocationNode)param).ExpandMacro());
                        return myBody;
                    }
                    catch (KeyNotFoundException)
                    {
                        MacroInvocationNode asMacro = (MacroInvocationNode)param;
                        p.Error(asMacro.MyLocation, "Undefined macro: " + asMacro.Name);
                    }
                    break;
                case ParamType.LIST:
                    ListNode n = (ListNode)param;
                    return new List<Token>(n.ToTokens());
                case ParamType.ATOM:
                    return new List<Token>(((IAtomNode)param).ToTokens());
            }
            return null;
        }
        private static IEnumerable<Token> ExpandAllIdentifiers(EAParser p, Queue<Token> tokens, ImmutableStack<string> seenDefs, ImmutableStack<Tuple<string, int>> seenMacros)
        {
            while (tokens.Count > 0)
            {
                Token current = tokens.Dequeue();
                if (current.Type == TokenType.IDENTIFIER)
                {
                    if (p.Macros.ContainsName(current.Content) && tokens.Count > 0 && tokens.Peek().Type == TokenType.OPEN_PAREN)
                    {
                        IList<IList<Token>> param = p.ParseMacroParamList(new MergeableGenerator<Token>(tokens)); //TODO: I don't like wrapping this in a mergeable generator..... Maybe interface the original better?
                        if (!seenMacros.Contains(new Tuple<string, int>(current.Content, param.Count)) && p.Macros.HasMacro(current.Content, param.Count))
                        {
                            foreach (Token t in p.Macros.GetMacro(current.Content, param.Count).ApplyMacro(current, param, p.GlobalScope))
                            {
                                yield return t;
                            }
                        }
                        else if (seenMacros.Contains(new Tuple<string, int>(current.Content, param.Count)))
                        {
                            yield return current;
                            foreach (IList<Token> l in param)
                                foreach (Token t in l)
                                    yield return t;
                        }
                        else
                        {
                            yield return current;
                        }
                    }
                    else if (!seenDefs.Contains(current.Content) && p.Definitions.ContainsKey(current.Content))
                    {
                        foreach (Token t in p.Definitions[current.Content].ApplyDefinition(current))
                            yield return t;
                    }
                    else
                    {
                        yield return current;
                    }
                }
                else
                {
                    yield return current;
                }
            }
        }
    }
}

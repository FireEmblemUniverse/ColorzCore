using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    class DefineDirective : IDirective
    {
        public int MinParams => 1;

        public int? MaxParams => 2;

        public bool RequireInclusion => true;

        public Maybe<ILineNode> Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            if (parameters[0].Type == ParamType.MACRO)
            {
                MacroInvocationNode signature = (MacroInvocationNode)(parameters[0]);
                string name = signature.Name;
                IList<Token> myParams = new List<Token>();
                foreach (IList<Token> l1 in signature.Parameters)
                {
                    if (l1.Count != 1 || l1[0].Type != TokenType.IDENTIFIER)
                    {
                        /*if (l1.Count == 0)
                            p.Error(l1[0].Location, "Missing parameter."); //TODO: This shouldn't be reached?
                        else*/
                        p.Error(l1[0].Location, "Macro parameters must be identifiers.");
                    }
                    else
                    {
                        myParams.Add(l1[0]);
                    }
                }
                if (!p.IsValidMacroName(name, myParams.Count))
                {
                    if (p.IsRawName(name))
                    {
                        p.Error(signature.MyLocation, "Invalid redefinition: " + name);
                    }
                    else
                        p.Warning(signature.MyLocation, "Redefining " + name + '.');
                }
                if (parameters.Count != 2)
                {
                    p.Error(signature.MyLocation, "Empty macro definition."); //TODO: Make location info better?
                    return new Nothing<ILineNode>();
                }

                Maybe<IList<Token>> toRepl = TokenizeParam(p, parameters[1]);
                if (!toRepl.IsNothing)
                {
                    if (!p.Macros.ContainsKey(name))
                        p.Macros[name] = new Dictionary<int, Macro>();
                    p.Macros[name][myParams.Count] = new Macro(myParams, toRepl.FromJust);
                }
            }
            else
            {
                //Note [mutually] recursive definitions are handled by Parser expansion.
                Maybe<string> maybeIdentifier;
                if (parameters[0].Type == ParamType.ATOM && !(maybeIdentifier = ((IAtomNode)parameters[0]).GetIdentifier()).IsNothing)
                {
                    string name = maybeIdentifier.FromJust;
                    if(!p.IsValidDefinitionName(name)
                    {
                        if (p.IsRawName(name))
                        {
                            p.Error(signature.MyLocation, "Invalid redefinition: " + name);
                        }
                        else
                            p.Warning(signature.MyLocation, "Redefining " + name + '.');
                    }
                    if (parameters.Count == 2)
                    {
                        Maybe<IList<Token>> toRepl = TokenizeParam(p, parameters[1]);
                        if (!toRepl.IsNothing)
                        {
                            p.Definitions[name] = new Definition(toRepl.FromJust);
                        }
                    }
                    else
                    {
                        p.Definitions[name] = new Definition();
                    }
                }
                else
                {
                    p.Error(parameters[0].MyLocation, "Definition names must be identifiers.");
                }
            }
            return new Nothing<ILineNode>();
        }
        private Maybe<IList<Token>> TokenizeParam(EAParser p, IParamNode param)
        {

            switch (param.Type)
            {
                case ParamType.STRING:
                    Token input = ((StringNode)param).MyToken;
                    Tokenizer t = new Tokenizer();
                    return new Just<IList<Token>>(new List<Token>(t.TokenizeLine(input.Content, input.FileName, input.LineNumber, input.ColumnNumber)));
                case ParamType.MACRO:
                    try
                    {
                        IList<Token> myBody = new List<Token>(((MacroInvocationNode)param).ExpandMacro());
                        return new Just<IList<Token>>(myBody);
                    }
                    catch (KeyNotFoundException)
                    {
                        MacroInvocationNode asMacro = (MacroInvocationNode)param;
                        p.Error(asMacro.MyLocation, "Undefined macro: " + asMacro.Name);
                    }
                    break;
                case ParamType.LIST:
                    ListNode n = (ListNode)param;
                    return new Just<IList<Token>>(new List<Token>(n.ToTokens()));
                case ParamType.ATOM:
                    return new Just<IList<Token>>(new List<Token>(((IAtomNode)param).ToTokens()));
            }
            return new Nothing<IList<Token>>();
        }
        private IList<Token> ExpandAllIdentifiers(EAParser p, Stack<Token> tokens, ImmutableStack<string> seenDefs, ImmutableStack<Tuple<string, int>> seenMacros)
        {
            IList<Token> output = new List<Token>
            while(tokens.Count > 0)
            {
                Token current = tokens.Pop();
                if(current.Type == TokenType.IDENTIFIER)
                {
                    if(p.Macros.ContainsKey(current.Content) && tokens.Count > 0 && tokens.Peek().Type == TokenType.OPEN_PAREN)
                    {
                        IList<IList<Token>> params = p.ParseMacroParamList(new MergeableGenerator(tokens.GetEnumerator())); //TODO: I don't like wrapping this in a gergeable generator..... Maybe interface the original better?
                    }
                } 
                else
                {
                    output.Add(current);
                }
            }
        
        }
    }
}

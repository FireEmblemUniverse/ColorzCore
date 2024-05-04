using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor.Macros;

namespace ColorzCore.Preprocessor.Directives
{
    class DefineDirective : IDirective
    {
        public bool RequireInclusion => true;

        public void Execute(EAParser p, Token self, MergeableGenerator<Token> tokens)
        {
            Token nextToken = tokens.Current;
            IList<string>? parameters;

            switch (nextToken.Type)
            {
                case TokenType.IDENTIFIER:
                    tokens.MoveNext();
                    parameters = null;
                    break;

                case TokenType.MAYBE_MACRO:
                    tokens.MoveNext();
                    parameters = FlattenParameters(p, p.ParseMacroParamList(tokens));
                    break;

                case TokenType.NEWLINE:
                    p.Logger.Error(self.Location, "Invalid use of directive '#define': missing macro name.");
                    return;

                default:
                    p.Logger.Error(self.Location, $"Invalid use of directive '#define': expected macro name, got {nextToken}");
                    p.IgnoreRestOfLine(tokens);
                    return;
            }

            IList<Token>? macroBody = ExpandMacroBody(p, p.GetRestOfLine(tokens));

            if (parameters != null)
            {
                // function-like macro
                DefineFunctionMacro(p, nextToken, parameters, macroBody);
            }
            else
            {
                // object-like macro
                DefineObjectMacro(p, nextToken, macroBody);
            }
        }

        private static void DefineObjectMacro(EAParser p, Token nameToken, IList<Token> macroBody)
        {
            string name = nameToken.Content;

            if (p.Definitions.ContainsKey(name) && EAOptions.IsWarningEnabled(EAOptions.Warnings.ReDefine))
            {
                p.Logger.Warning(nameToken.Location, $"Redefining {name}.");
            }

            if (macroBody.Count == 1 && macroBody[0].Type == TokenType.IDENTIFIER && macroBody[0].Content == name)
            {
                /* an object-like macro whose only inner token is a reference to itself is non-productive
                 * this is: it doesn't participate in macro expansion. */

                p.Definitions[name] = new Definition();
            }
            else
            {
                p.Definitions[name] = new Definition(macroBody);
            }
        }

        private static void DefineFunctionMacro(EAParser p, Token nameToken, IList<string> parameters, IList<Token> macroBody)
        {
            string name = nameToken.Content;

            if (p.Macros.HasMacro(name, parameters.Count) && EAOptions.IsWarningEnabled(EAOptions.Warnings.ReDefine))
            {
                p.Logger.Warning(nameToken.Location, $"Redefining {name}(...) with {parameters.Count} parameters.");
            }

            p.Macros.AddMacro(new UserMacro(parameters, macroBody), name, parameters.Count);
        }

        private static IList<string> FlattenParameters(EAParser p, IList<IList<Token>> rawParameters)
        {
            IList<string> result = new List<string>();

            foreach (IList<Token> parameter in rawParameters)
            {
                if (parameter.Count != 1 || parameter[0].Type != TokenType.IDENTIFIER)
                {
                    p.Logger.Error(parameter[0].Location, $"Macro parameters must be single identifiers (got {parameter[0].Content}).");
                    result.Add($"${result.Count}");
                }
                else
                {
                    result.Add(parameter[0].Content);
                }
            }

            return result;
        }

        private static IList<Token> ExpandMacroBody(EAParser _, IList<Token> body)
        {
            if (body.Count == 1 && body[0].Type == TokenType.STRING)
            {
                // FIXME: for some reason, locations of tokens in this are offset by 1

                Token token = body[0];
                return new List<Token>(Tokenizer.TokenizeLine(token.Content, token.Location));
            }

            return body;
        }
    }
}

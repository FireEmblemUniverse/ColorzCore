using System;
using System.Collections.Generic;
using System.IO;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    public class IncludeEncodingTableDirective : BaseIncludeDirective
    {
        private readonly StringProcessor stringProcessor;

        public IncludeEncodingTableDirective(StringProcessor stringProcessor)
        {
            this.stringProcessor = stringProcessor;
        }

        public override int MinParams => 2;

        public override int? MaxParams => 2;

        /* HACK: this is set by HandleInclude and read by Execute
         * because we depend BaseIncludeDirective Execute, there's no elegant way of passing this around. */
        private TblEncoding? lastTblEncoding = null;

        public override void Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            string? encodingName = parameters[0].ToString();

            if (encodingName != null)
            {
                if (stringProcessor.TableEncodings.ContainsKey(encodingName))
                {
                    p.Logger.Error(self.Location, $"String encoding '{encodingName}' already exists.");
                }
                else
                {
                    base.Execute(p, self, new List<IParamNode>() { parameters[1] }, tokens);

                    if (lastTblEncoding != null)
                    {
                        stringProcessor.TableEncodings[encodingName] = lastTblEncoding;
                        lastTblEncoding = null;
                    }
                    else
                    {
                        p.Logger.Error(self.Location, $"Could not load encoding from table file '{parameters[1]}'.");
                    }
                }
            }
            else
            {
                p.Logger.Error(self.Location, $"{self.Content} expected encoding name as first parameter.");
            }
        }

        public override void HandleInclude(EAParser p, Token self, string path, MergeableGenerator<Token> tokens)
        {
            using TextReader textReader = new StreamReader(path);
            lastTblEncoding = TblEncoding.FromTextReader(textReader);
        }
    }
}

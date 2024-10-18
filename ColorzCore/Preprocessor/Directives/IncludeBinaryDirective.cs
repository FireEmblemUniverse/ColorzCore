using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using System.IO;
using ColorzCore.Parser;
using ColorzCore.IO;

namespace ColorzCore.Preprocessor.Directives
{
    public class IncludeBinaryDirective : BaseIncludeDirective
    {
        public override void HandleInclude(EAParser p, Token self, string path, MergeableGenerator<Token> _)
        {
            try
            {
                byte[] data = File.ReadAllBytes(path);
                p.ParseConsumer.OnData(self.Location, data);
            }
            catch (IOException e)
            {
                p.Logger.Error(self.Location, $"Error reading file \"{path}\": {e.Message}.");
            }
        }
    }
}

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
    class IncludeDirective : BaseIncludeDirective
    {
        public override void HandleInclude(EAParser p, Token self, string path, MergeableGenerator<Token> tokens)
        {
            try
            {
                FileStream inputFile = new FileStream(path, FileMode.Open);
                tokens.PrependEnumerator(new Tokenizer().TokenizeFile(inputFile, path.Replace('\\', '/')).GetEnumerator());
            }
            catch (IOException e)
            {
                p.Logger.Error(self.Location, $"Error reading file \"{path}\": {e.Message}.");
            }
        }
    }
}

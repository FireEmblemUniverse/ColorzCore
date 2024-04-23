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
    class IncludeDirective : SimpleDirective
    {
        public override int MinParams => 1;

        public override int? MaxParams => 1;

        public override bool RequireInclusion => true;

        public IncludeFileSearcher FileSearcher { get; set; } = new IncludeFileSearcher();

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            string? existantFile = FileSearcher.FindFile(Path.GetDirectoryName(self.FileName), parameters[0].ToString()!);

            if (existantFile != null)
            {
                try
                {
                    string pathname = existantFile;

                    FileStream inputFile = new FileStream(pathname, FileMode.Open);
                    Tokenizer newFileTokenizer = new Tokenizer();
                    tokens.PrependEnumerator(newFileTokenizer.Tokenize(inputFile).GetEnumerator());
                }
                catch (Exception)
                {
                    p.Error(self.Location, "Error reading file \"" + parameters[0].ToString() + "\".");
                }
            }
            else
            {
                p.Error(parameters[0].MyLocation, "Could not find file \"" + parameters[0].ToString() + "\".");
            }
            return null;
        }
    }
}

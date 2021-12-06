using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser.AST;
using System.IO;
using ColorzCore.Parser;
using ColorzCore.IO;

namespace ColorzCore.Preprocessor.Directives
{
    class IncludeDirective : IDirective
    {
        public int MinParams { get { return 1; } }

        public int? MaxParams { get { return 1; } }

        public bool RequireInclusion { get { return true; } }

        public IncludeFileSearcher FileSearcher { get; set; }

        public Maybe<ILineNode> Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens) {
            var file = parameters[0].ToString().Replace("\\", "/");
            Maybe<string> existantFile = FileSearcher.FindFile(Path.GetDirectoryName(self.FileName), file);

            if (!existantFile.IsNothing)
            {
                try
                {
                    string pathname = existantFile.FromJust;

                    FileStream inputFile = new FileStream(pathname, FileMode.Open);
                    Tokenizer newFileTokenizer = new Tokenizer();
                    tokens.PrependEnumerator(newFileTokenizer.Tokenize(inputFile).GetEnumerator());
                }
                catch(Exception)
                {
                    p.Error(self.Location, "Error reading file \"" + file + "\".");
                }
            }
            else
            {
                p.Error(parameters[0].MyLocation, "Could not find file \"" + file + "\".");
            }
            return new Nothing<ILineNode>();
        }
    }
}

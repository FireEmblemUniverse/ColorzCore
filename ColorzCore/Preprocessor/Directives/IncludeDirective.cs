﻿using System;
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

        public IncludeFileSearcher FileSearcher { get; set; } = new IncludeFileSearcher();

        public ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
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

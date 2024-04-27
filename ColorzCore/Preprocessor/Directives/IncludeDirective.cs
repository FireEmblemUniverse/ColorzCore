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
        // TODO: merge include and incbin into a shared base class

        public override int MinParams => 1;

        public override int? MaxParams => 1;

        public override bool RequireInclusion => true;

        public IncludeFileSearcher FileSearcher { get; set; } = new IncludeFileSearcher();

        public override ILineNode? Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
        {
            string pathExpression = parameters[0].ToString()!;

            if (EAOptions.TranslateBackslashesInPaths)
            {
                pathExpression = pathExpression.Replace('\\', '/');
            }

            string? existantFile = FileSearcher.FindFile(Path.GetDirectoryName(self.FileName), pathExpression);

            if (existantFile != null)
            {
                if (EAOptions.IsWarningEnabled(EAOptions.Warnings.NonPortablePath))
                {
                    string portablePathExpression = IOUtility.GetPortablePathExpression(existantFile, pathExpression);

                    if (pathExpression != portablePathExpression)
                    {
                        p.Warning(self.Location, $"Path is not portable (should be \"{portablePathExpression}\").");
                    }
                }

                try
                {
                    FileStream inputFile = new FileStream(existantFile, FileMode.Open);
                    tokens.PrependEnumerator(new Tokenizer().Tokenize(inputFile).GetEnumerator());
                }
                catch (Exception e)
                {
                    p.Error(self.Location, $"Error reading file \"{parameters[0].ToString()}\": {e.Message}.");
                }
            }
            else
            {
                p.Error(parameters[0].MyLocation, $"Could not find file \"{parameters[0].ToString()}\".");
            }

            return null;
        }
    }
}

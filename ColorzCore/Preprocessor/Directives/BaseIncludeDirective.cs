
using System.Collections.Generic;
using System.IO;
using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Preprocessor.Directives
{
    public abstract class BaseIncludeDirective : SimpleDirective
    {
        public override int MinParams => 1;

        public override int? MaxParams => 1;

        public override bool RequireInclusion => true;

        public IncludeFileSearcher FileSearcher { get; set; } = new IncludeFileSearcher();

        public override void Execute(EAParser p, Token self, IList<IParamNode> parameters, MergeableGenerator<Token> tokens)
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
                        p.Logger.Warning(self.Location, $"Path is not portable (should be \"{portablePathExpression}\").");
                    }
                }

                HandleInclude(p, self, existantFile, tokens);
            }
            else
            {
                p.Logger.Error(parameters[0].MyLocation, $"Could not find file \"{pathExpression}\".");
            }
        }

        public abstract void HandleInclude(EAParser p, Token self, string path, MergeableGenerator<Token> tokens);
    }
}

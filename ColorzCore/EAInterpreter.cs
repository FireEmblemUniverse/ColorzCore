using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ColorzCore
{
    //Class to excapsulate all steps in EA script interpretation.
    class EAInterpreter
    {
        private Dictionary<string, IList<Raw>> allRaws;
        private EAParser myParser;
        private string game, iFile;
        private Stream sin;
        private Log log;
        private IOutput output;

        public EAInterpreter(IOutput output, string game, string? rawsFolder, string rawsExtension, Stream sin, string inFileName, Log log)
        {
            this.game = game;
            this.output = output;

            try
            {
                allRaws = SelectRaws(game, ListAllRaws(rawsFolder, rawsExtension));
            }
            catch (RawReader.RawParseException e)
            {
                Location loc = new Location
                {
                    file = e.FileName,
                    lineNum = e.LineNumber,
                    colNum = 1
                };

                log.Message(Log.MessageKind.ERROR, loc, "An error occured while parsing raws");
                log.Message(Log.MessageKind.ERROR, loc, e.Message);

                Environment.Exit(-1); // ew?
            }

            this.sin = sin;
            this.log = log;
            iFile = inFileName;

            IncludeFileSearcher includeSearcher = new IncludeFileSearcher();
            includeSearcher.IncludeDirectories.Add(AppDomain.CurrentDomain.BaseDirectory);

            foreach (string path in EAOptions.Instance.includePaths)
                includeSearcher.IncludeDirectories.Add(path);

            IncludeFileSearcher toolSearcher = new IncludeFileSearcher { AllowRelativeInclude = false };
            toolSearcher.IncludeDirectories.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"));

            foreach (string path in EAOptions.Instance.toolsPaths)
                includeSearcher.IncludeDirectories.Add(path);

            myParser = new EAParser(allRaws, log, new Preprocessor.DirectiveHandler(includeSearcher, toolSearcher));

            myParser.Definitions['_' + game + '_'] = new Definition();
            myParser.Definitions["__COLORZ_CORE__"] = new Definition();
        }

        public bool Interpret()
        {
            Tokenizer t = new Tokenizer();

            ExecTimer.Timer.AddTimingPoint(ExecTimer.KEY_GENERIC);

            foreach (Tuple<string, string> defpair in EAOptions.Instance.defs)
            {
                myParser.ParseAll(t.TokenizeLine("#define " + defpair.Item1 + " " + defpair.Item2, "cmd", 0));
            }

            IList<ILineNode> lines = new List<ILineNode>(myParser.ParseAll(t.Tokenize(sin, iFile)));

            /* First pass on AST: Identifier resolution.
             * 
             * Suppose we had the code
             * 
             * POIN myLabel
             * myLabel:
             * 
             * At parse time, myLabel did not exist for the POIN. 
             * It is at this point we want to make sure all references to identifiers are valid, before assembling.
             */
            List<Token> undefinedIds = new List<Token>();
            foreach (ILineNode line in lines)
            {
                try
                {
                    line.EvaluateExpressions(undefinedIds);
                }
                catch (MacroInvocationNode.MacroException e)
                {
                    myParser.Error(e.CausedError.MyLocation, "Unexpanded macro.");
                }
            }

            foreach (Token errCause in undefinedIds)
            {
                if (errCause.Content.StartsWith(Pool.pooledLabelPrefix, StringComparison.Ordinal))
                {
                    myParser.Error(errCause.Location, "Unpooled data (forgot #pool?)");
                }
                else
                {
                    myParser.Error(errCause.Location, $"Undefined identifier `{errCause.Content}`");
                }
            }

            /* Last step: assembly */

            ExecTimer.Timer.AddTimingPoint(ExecTimer.KEY_DATAWRITE);

            if (!log.HasErrored)
            {
                foreach (ILineNode line in lines)
                {
                    if (Program.Debug)
                    {
                        log.Message(Log.MessageKind.DEBUG, line.PrettyPrint(0));
                    }

                    line.WriteData(output);
                }

                output.Commit();

                log.Output.WriteLine("No errors. Please continue being awesome.");
                return true;
            }
            else
            {
                log.Output.WriteLine("Errors occurred; no changes written.");
                return false;
            }
        }

        public bool WriteNocashSymbols(TextWriter output)
        {
            foreach (var label in myParser.GlobalScope.Head.LocalLabels())
            {
                output.WriteLine("{0:X8} {1}", EAParser.ConvertToAddress(label.Value), label.Key);
            }

            return true;
        }

        private static IEnumerable<Raw> LoadAllRaws(string rawsFolder, string rawsExtension)
        {
            var directoryInfo = new DirectoryInfo(rawsFolder);
            var files = directoryInfo.GetFiles("*" + rawsExtension, SearchOption.AllDirectories);

            foreach (FileInfo fileInfo in files)
            {
                using var fs = new FileStream(fileInfo.FullName, FileMode.Open);

                foreach (var raw in RawReader.ParseAllRaws(fs))
                    yield return raw;
            }
        }

        private static IList<Raw> ListAllRaws(string? rawsFolder, string rawsExtension)
        {
            if (rawsFolder != null)
            {
                return new List<Raw>(LoadAllRaws(rawsFolder, rawsExtension));
            }
            else
            {
                return GetFallbackRaws();
            }
        }

        private static IList<Raw> GetFallbackRaws()
        {
            static List<IRawParam> CreateParams(int bitSize, bool isPointer)
            {
                return new List<IRawParam>() { new AtomicParam("Data", 0, bitSize, isPointer) };
            }

            static Raw CreateRaw(string name, int byteSize, int alignment, bool isPointer)
            {
                return new Raw(name, byteSize * 8, 0, alignment, CreateParams(byteSize * 8, isPointer), true);
            }

            return new List<Raw>()
            {
                CreateRaw("BYTE", 1, 1, false),
                CreateRaw("SHORT", 2, 2, false),
                CreateRaw("WORD", 4, 4, false),
                CreateRaw("POIN", 4, 4, true),
                CreateRaw("SHORT2", 2, 1, false),
                CreateRaw("WORD2", 4, 1, false),
                CreateRaw("POIN2", 4, 1, true),
            };
        }

        private static Dictionary<string, IList<Raw>> SelectRaws(string game, IList<Raw> allRaws)
        {
            Dictionary<string, IList<Raw>> result = new Dictionary<string, IList<Raw>>();

            foreach (Raw r in allRaws)
            {
                if (r.Game.Count == 0 || r.Game.Contains(game))
                    result.AddTo(r.Name, r);
            }

            return result;
        }
    }
}

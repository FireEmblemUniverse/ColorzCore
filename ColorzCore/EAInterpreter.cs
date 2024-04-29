using ColorzCore.DataTypes;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Preprocessor;
using ColorzCore.Preprocessor.Directives;
using ColorzCore.Preprocessor.Macros;
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
        private EAParseConsumer myParseConsumer;
        private string game, iFile;
        private Stream sin;
        private Logger log;
        private IOutput output;

        public EAInterpreter(IOutput output, string game, string? rawsFolder, string rawsExtension, Stream sin, string inFileName, Logger log)
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
                    line = e.LineNumber,
                    column = 1
                };

                log.Message(Logger.MessageKind.ERROR, loc, "An error occured while parsing raws");
                log.Message(Logger.MessageKind.ERROR, loc, e.Message);

                Environment.Exit(-1); // ew?
            }

            this.sin = sin;
            this.log = log;
            iFile = inFileName;

            IncludeFileSearcher includeSearcher = new IncludeFileSearcher();
            includeSearcher.IncludeDirectories.Add(AppDomain.CurrentDomain.BaseDirectory);

            foreach (string path in EAOptions.IncludePaths)
                includeSearcher.IncludeDirectories.Add(path);

            IncludeFileSearcher toolSearcher = new IncludeFileSearcher { AllowRelativeInclude = false };
            toolSearcher.IncludeDirectories.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"));

            foreach (string path in EAOptions.ToolsPaths)
                includeSearcher.IncludeDirectories.Add(path);

            myParseConsumer = new EAParseConsumer(log);
            myParser = new EAParser(log, allRaws, new DirectiveHandler(includeSearcher, toolSearcher), myParseConsumer);

            myParser.Definitions[$"_{game}_"] = new Definition();
            myParser.Definitions["__COLORZ_CORE__"] = new Definition();

            if (EAOptions.IsExtensionEnabled(EAOptions.Extensions.ReadDataMacros) && output is ROM rom)
            {
                myParser.Definitions["__has_read_data_macros"] = new Definition();

                myParser.Macros.BuiltInMacros.Add("ReadByteAt", new ReadDataAt(myParser, rom, 1));
                myParser.Macros.BuiltInMacros.Add("ReadShortAt", new ReadDataAt(myParser, rom, 2));
                myParser.Macros.BuiltInMacros.Add("ReadWordAt", new ReadDataAt(myParser, rom, 4));
            }
            else
            {
                BuiltInMacro unsupportedMacro = new ErrorMacro(myParser, "Macro unsupported in this configuration.", i => i == 1);

                myParser.Macros.BuiltInMacros.Add("ReadByteAt", unsupportedMacro);
                myParser.Macros.BuiltInMacros.Add("ReadShortAt", unsupportedMacro);
                myParser.Macros.BuiltInMacros.Add("ReadWordAt", unsupportedMacro);
            }

            {
                Pool pool = new Pool();

                myParser.Macros.BuiltInMacros.Add("AddToPool", new AddToPool(pool));
                myParser.DirectiveHandler.Directives.Add("pool", new PoolDirective(pool));
            }
        }

        public bool Interpret()
        {
            Tokenizer t = new Tokenizer();

            ExecTimer.Timer.AddTimingPoint(ExecTimer.KEY_GENERIC);

            foreach ((string name, string body) in EAOptions.PreDefintions)
            {
                myParser.ParseAll(t.TokenizeLine($"#define {name} {body}", "cmd", 0));
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
            List<(Location, Exception)> evaluationErrors = new List<(Location, Exception)>();
            foreach (ILineNode line in lines)
            {
                try
                {
                    line.EvaluateExpressions(evaluationErrors, EvaluationPhase.Final);
                }
                catch (MacroInvocationNode.MacroException e)
                {
                    log.Error(e.CausedError.MyLocation, "Unexpanded macro.");
                }
            }

            foreach ((Location location, Exception e) in evaluationErrors)
            {
                if (e is IdentifierNode.UndefinedIdentifierException uie
                    && uie.CausedError.Content.StartsWith(Pool.pooledLabelPrefix, StringComparison.Ordinal))
                {
                    log.Error(location, "Unpooled data (forgot #pool?)");
                }
                else
                {
                    log.Error(location, e.Message);
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
                        log.Message(Logger.MessageKind.DEBUG, line.PrettyPrint(0));
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
            foreach (var label in myParseConsumer.GlobalScope.Head.LocalSymbols())
            {
                output.WriteLine("{0:X8} {1}", EAParseConsumer.ConvertToAddress(label.Value), label.Key);
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

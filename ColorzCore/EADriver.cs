using ColorzCore.DataTypes;
using ColorzCore.Interpreter;
using ColorzCore.Interpreter.Diagnostics;
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
    // Class to excapsulate all steps in EA script interpretation (lexing -> parsing -> interpretation -> commit).
    class EADriver
    {
        private Dictionary<string, IList<Raw>> allRaws;
        private EAParser myParser;
        private EAInterpreter myInterpreter;
        private string iFile;
        private Stream sin;
        private Logger Logger { get; }
        private IOutput output;

        public EADriver(IOutput output, string? game, string? rawsFolder, string rawsExtension, Stream sin, string inFileName, Logger logger)
        {
            this.output = output;

            try
            {
                allRaws = SelectRaws(game, ListAllRaws(rawsFolder, rawsExtension));
            }
            catch (RawReader.RawParseException e)
            {
                Location loc = new Location(e.FileName, e.LineNumber, 1);

                logger.Message(Logger.MessageKind.ERROR, loc, "An error occured while parsing raws");
                logger.Message(Logger.MessageKind.ERROR, loc, e.Message);

                Environment.Exit(-1); // ew?
            }

            this.sin = sin;
            Logger = logger;
            iFile = inFileName;

            myInterpreter = new EAInterpreter(logger);

            ParseConsumerChain parseConsumers = new ParseConsumerChain();

            if (EAOptions.IsWarningEnabled(EAOptions.Warnings.SetSymbolMacros))
            {
                parseConsumers.Add(new SetSymbolMacroDetector(logger));
            }

            // add the interpreter last
            parseConsumers.Add(myInterpreter);

            StringProcessor stringProcessor = new StringProcessor(logger);

            myParser = new EAParser(logger, allRaws, parseConsumers, myInterpreter.BindIdentifier, stringProcessor);

            myParser.Definitions["__COLORZ_CORE__"] = new Definition();

            myParser.Definitions["__COLORZ_CORE_VERSION__"] = new Definition(
                new Token(TokenType.NUMBER, new Location("builtin", 0, 0), "20240504"));

            if (game != null)
            {
                myParser.Definitions[$"_{game}_"] = new Definition();
            }

            IncludeFileSearcher includeSearcher = new IncludeFileSearcher();
            includeSearcher.IncludeDirectories.Add(AppDomain.CurrentDomain.BaseDirectory);

            foreach (string path in EAOptions.IncludePaths)
            {
                includeSearcher.IncludeDirectories.Add(path);
            }

            myParser.DirectiveHandler.Directives["include"] = new IncludeDirective() { FileSearcher = includeSearcher };
            myParser.DirectiveHandler.Directives["incbin"] = new IncludeBinaryDirective() { FileSearcher = includeSearcher };

            myParser.DirectiveHandler.Directives["inctbl"] = new IncludeEncodingTableDirective(stringProcessor)
            {
                FileSearcher = includeSearcher
            };

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

            if (EAOptions.IsExtensionEnabled(EAOptions.Extensions.IncludeTools))
            {
                myParser.Definitions["__has_incext"] = new Definition();

                IncludeFileSearcher toolSearcher = new IncludeFileSearcher { AllowRelativeInclude = false };
                toolSearcher.IncludeDirectories.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"));

                foreach (string path in EAOptions.ToolsPaths)
                {
                    includeSearcher.IncludeDirectories.Add(path);
                }

                IDirective incextDirective = new IncludeExternalDirective { FileSearcher = toolSearcher };
                IDirective inctextDirective = new IncludeToolEventDirective { FileSearcher = toolSearcher };

                myParser.DirectiveHandler.Directives["incext"] = incextDirective;
                myParser.DirectiveHandler.Directives["inctext"] = inctextDirective;
                myParser.DirectiveHandler.Directives["inctevent"] = inctextDirective;
            }

            if (EAOptions.IsExtensionEnabled(EAOptions.Extensions.AddToPool))
            {
                myParser.Definitions["__has_pool"] = new Definition();

                Pool pool = new Pool();

                myParser.Macros.BuiltInMacros.Add("AddToPool", new AddToPool(pool));
                myParser.DirectiveHandler.Directives.Add("pool", new PoolDirective(pool));
            }
        }

        public bool Interpret()
        {
            Tokenizer tokenizer = new Tokenizer();

            ExecTimer.Timer.AddTimingPoint(ExecTimer.KEY_GENERIC);

            foreach ((string name, string body) in EAOptions.PreDefintions)
            {
                Location location = new Location("CMD", 0, 1);
                myParser.ParseAll(tokenizer.TokenizePhrase($"#define {name} \"{body}\"", location));
            }

            myParser.ParseAll(tokenizer.Tokenize(sin, iFile));

            IList<ILineNode> lines = myInterpreter.HandleEndOfInput();

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
                    Logger.Error(e.CausedError.MyLocation, "Unexpanded macro.");
                }
            }

            foreach ((Location location, Exception e) in evaluationErrors)
            {
                if (e is IdentifierNode.UndefinedIdentifierException uie
                    && uie.CausedError.Content.StartsWith(Pool.pooledLabelPrefix, StringComparison.Ordinal))
                {
                    Logger.Error(location, "Unpooled data (forgot #pool?)");
                }
                else
                {
                    Logger.Error(location, e.Message);
                }
            }

            /* Last step: assembly */

            ExecTimer.Timer.AddTimingPoint(ExecTimer.KEY_DATAWRITE);

            if (!Logger.HasErrored)
            {
                foreach (ILineNode line in lines)
                {
                    if (Program.Debug)
                    {
                        Logger.Message(Logger.MessageKind.DEBUG, line.PrettyPrint(0));
                    }

                    line.WriteData(output);
                }

                output.Commit();

                Logger.Output.WriteLine("No errors. Please continue being awesome.");
                return true;
            }
            else
            {
                Logger.Output.WriteLine("Errors occurred; no changes written.");
                return false;
            }
        }

        public bool WriteNocashSymbols(TextWriter output)
        {
            for (int i = 0; i < myInterpreter.AllScopes.Count; i++)
            {
                string manglePrefix = string.Empty;

                switch (i)
                {
                    case 0:
                        output.WriteLine("; global scope");
                        break;
                    default:
                        output.WriteLine($"; local scope {i}");
                        manglePrefix = $"${i}$";
                        break;
                }

                Closure scope = myInterpreter.AllScopes[i];

                foreach (KeyValuePair<string, int> pair in scope.LocalSymbols())
                {
                    string name = pair.Key;
                    int address = EAInterpreter.ConvertToAddress(pair.Value);

                    output.WriteLine($"{address:X8} {manglePrefix}{name}");
                }
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

        private static Dictionary<string, IList<Raw>> SelectRaws(string? game, IList<Raw> allRaws)
        {
            Dictionary<string, IList<Raw>> result = new Dictionary<string, IList<Raw>>();

            foreach (Raw raw in allRaws)
            {
                if (raw.Game.Count == 0 || (game != null && raw.Game.Contains(game)))
                {
                    result.AddTo(raw.Name, raw);
                }
            }

            return result;
        }
    }
}

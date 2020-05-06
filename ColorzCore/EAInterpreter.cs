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
        private EAOptions opts;
        private IOutput output;

        public EAInterpreter(IOutput output, string game, string rawsFolder, string rawsExtension, Stream sin, string inFileName, Log log, EAOptions opts)
        {

            this.game = game;
            this.output = output;

            try
            {
                allRaws = ProcessRaws(game, LoadAllRaws(rawsFolder, rawsExtension));
            }
            catch (Raw.RawParseException e)
            {
                Location loc = new Location
                {
                    file = Raw.RawParseException.filename, // I get that this looks bad, but this exception happens at most once per execution... TODO: Make this less bad.
                    lineNum = e.rawline.ToInt(),
                    colNum = 1
                };

                log.Message(Log.MsgKind.ERROR, loc, "An error occured while parsing raws");
                log.Message(Log.MsgKind.ERROR, loc, e.Message);

                Environment.Exit(-1);
            }

            this.sin = sin;
            this.log = log;
            iFile = inFileName;
            this.opts = opts;

            IncludeFileSearcher includeSearcher = new IncludeFileSearcher();
            includeSearcher.IncludeDirectories.Add(AppDomain.CurrentDomain.BaseDirectory);

            foreach (string path in opts.includePaths)
                includeSearcher.IncludeDirectories.Add(path);

            IncludeFileSearcher toolSearcher = new IncludeFileSearcher { AllowRelativeInclude = false };
            toolSearcher.IncludeDirectories.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools"));

            foreach (string path in opts.toolsPaths)
                includeSearcher.IncludeDirectories.Add(path);

            myParser = new EAParser(allRaws, log, new Preprocessor.DirectiveHandler(includeSearcher, toolSearcher));

            myParser.Definitions['_' + game + '_'] = new Definition();
            myParser.Definitions["__COLORZ_CORE__"] = new Definition();
        }

        public bool Interpret()
        {
            Tokenizer t = new Tokenizer();
            ROM myROM = new ROM(fout);

            Program.Timer.AddTimingPoint(Program.ExecTimer.KEY_GENERIC);

            foreach (Tuple<string, string> defpair in opts.defs)
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
                } catch (MacroInvocationNode.MacroException e)
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
                    myParser.Error(errCause.Location, "Undefined identifier: " + errCause.Content);
                }
            }

            /* Last step: assembly */

            Program.Timer.AddTimingPoint(Program.ExecTimer.KEY_DATAWRITE);

            if (!log.HasErrored)
            {
                foreach (ILineNode line in lines)
                {
                    if (Program.Debug)
                    {
                        log.Message(Log.MsgKind.DEBUG, line.PrettyPrint(0));
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
                // TODO: more elegant offset to address mapping
                output.WriteLine("{0:X8} {1}", label.Value + 0x8000000, label.Key);
            }

            return true;
        }

        private static IList<Raw> LoadAllRaws(string rawsFolder, string rawsExtension)
        {
            string folder;
            DirectoryInfo directoryInfo = new DirectoryInfo(rawsFolder);
            folder = Path.GetFullPath(rawsFolder);
            FileInfo[] files = directoryInfo.GetFiles("*" + rawsExtension, SearchOption.AllDirectories);
            IEnumerable<Raw> allRaws = new List<Raw>();
            foreach (FileInfo fileInfo in files)
            {
                FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open);
                allRaws = allRaws.Concat(Raw.ParseAllRaws(fs));
                fs.Close();
            }
            return new List<Raw>(allRaws);
        }
        private static Dictionary<string, IList<Raw>> ProcessRaws(string game, IList<Raw> allRaws)
        {
            Dictionary<string, IList<Raw>> retVal = new Dictionary<string, IList<Raw>>();
            foreach (Raw r in allRaws)
            {
                if (r.Game.Contains(game))
                    retVal.AddTo(r.Name, r);
            }
            return retVal;
        }
    }
}

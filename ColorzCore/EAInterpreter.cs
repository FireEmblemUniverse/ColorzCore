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
        private FileStream fout;
        private TextWriter serr;
        private EAOptions opts;

        public EAInterpreter(string game, string rawsFolder, string rawsExtension, Stream sin, string inFileName, FileStream fout, TextWriter serr, EAOptions opts)
        {
            this.game = game;
            try
            {
                allRaws = ProcessRaws(game, LoadAllRaws(rawsFolder, rawsExtension));
            }
            catch (Raw.RawParseException e)
            {
                serr.WriteLine(e.Message);
                serr.WriteLine("Error occured as a result of the line:");
                serr.WriteLine(e.rawline);
                serr.WriteLine("In file " + Raw.RawParseException.filename); // I get that this looks bad, but this exception happens at most once per execution... TODO: Make this less bad.
                Environment.Exit(-1);
            }
            this.sin = sin;
            this.fout = fout;
            this.serr = serr;
            iFile = inFileName;
            this.opts = opts;

            myParser = new EAParser(allRaws);
            myParser.Definitions['_' + game + '_'] = new Definition();
        }

        public bool Interpret()
        {
            Tokenizer t = new Tokenizer();
            ROM myROM = new ROM(fout);

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
                myParser.Error(errCause.Location, "Undefined identifier: " + errCause.Content);
            }

            /* Last step: Message output and assembly */

            //TODO: sort them by file/line
            if (!opts.nomess)
            {
                serr.WriteLine("Messages:");
                if (myParser.Messages.Count == 0)
                    serr.WriteLine("No messages.");
                foreach (string message in myParser.Messages)
                {
                    serr.WriteLine(message);
                }
                serr.WriteLine();
            }

            if (opts.werr)
            {
                foreach (string warning in myParser.Warnings)
                    myParser.Errors.Add(warning);
            } else if (!opts.nowarn)
            {
                serr.WriteLine("Warnings:");
                if (myParser.Warnings.Count == 0)
                    serr.WriteLine("No warnings.");
                foreach (string warning in myParser.Warnings)
                {
                    serr.WriteLine(warning);
                }
                serr.WriteLine();
            }

            serr.WriteLine("Errors:");
            if (myParser.Errors.Count == 0)
                serr.WriteLine("No errors. Please continue being awesome.");
            foreach (string error in myParser.Errors)
            {
                serr.WriteLine(error);
            }

            if (myParser.Errors.Count == 0)
            {
                foreach (ILineNode line in lines)
                {
                    if (Program.Debug)
                    {
                        System.Console.Out.WriteLine(line.PrettyPrint(0));
                    }
                    line.WriteData(myROM);
                }

                myROM.WriteROM();
                return true;
            }
            else
            {
                serr.WriteLine("Errors occurred; no changes written.");
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

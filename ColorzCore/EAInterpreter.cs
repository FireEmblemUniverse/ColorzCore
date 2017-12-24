using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Raws;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ColorzCore
{
    //Class to excapsulate all steps in EA script interpretation.
    class EAInterpreter
    {
        private static readonly string RAWS_FOLDER = "Language Raws";
        private static readonly string RAWS_EXTENSION = ".txt"; 
        private Dictionary<string, IList<Raw>> allRaws;
        private EAParser myParser;
        private string game;

        public EAInterpreter(string game)
        {
            this.game = game;
            allRaws = ProcessRaws(game, LoadAllRaws());
        }

        public void Interpret(TextWriter outStream, string fileName)
        {
            myParser = new EAParser(allRaws);
            myParser.Definitions['_' + game + '_'] = new Definition();

            FileStream inputFile = new FileStream(fileName, FileMode.Open);
            Tokenizer t = new Tokenizer();

            IList<ILineNode> lines = new List<ILineNode>(myParser.ParseAll(t.Tokenize(inputFile)));

            //foreach (ILineNode l in lines)
            //    outStream.WriteLine(l.PrettyPrint(0));

            //TODO: sort them by file/line
            foreach (string message in myParser.Messages)
            {
                outStream.WriteLine("MESSAGE: " + message);
            }
            foreach (string warning in myParser.Warnings)
            {
                outStream.WriteLine("WARNING: " + warning);
            }
            foreach (string error in myParser.Errors)
            {
                outStream.WriteLine("ERROR: " + error);
            }

            //TODO: -WError flag?
            if(myParser.Errors.Count == 0)
            {

            }
            else
            {
                outStream.WriteLine("Errors occurred; no changes written.");
            }
        }

        private static IList<Raw> LoadAllRaws()
        {
            string folder;
            DirectoryInfo directoryInfo = new DirectoryInfo(RAWS_FOLDER);
            folder = Path.GetFullPath(RAWS_FOLDER);
            FileInfo[] files = directoryInfo.GetFiles("*" + RAWS_EXTENSION, SearchOption.AllDirectories);
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

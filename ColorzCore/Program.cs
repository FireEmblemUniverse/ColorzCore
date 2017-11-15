using ColorzCore.DataTypes;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;
using ColorzCore.Raws;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = "testFile.event";
            string game = "FE8";
            FileStream inputFile = new FileStream(fileName, FileMode.Open);

            //FirstPass(Tokenizer.Tokenize(inputStream));
            Tokenizer myTokenizer = new Tokenizer();
            EAParser myParser = new EAParser(ProcessRaws(game, LoadAllRaws()));
            myParser.Definitions['_'+game+'_'] = new Definition(); //For now, hardcode to assemble FE8.

            /*
            foreach (Token t in myTokenizer.Tokenize(inputStream, fileName))
            {
                Console.Out.WriteLine(t.ToString());
            }
            */
            //Console.WriteLine(test.myEnums.Peek().Current.ToString());

            
            foreach(ILineNode n in myParser.ParseAll(myTokenizer.Tokenize(inputFile)))
            {
                Console.WriteLine(n.PrettyPrint(0));
            }

            foreach (string error in myParser.Errors)
            {
                Console.Out.WriteLine(error);
            }

            //myParser.Clear();

            Console.WriteLine("Done.");

            Console.In.ReadLine();
            
        }
        private static IList<Raw> LoadAllRaws()
        {
            string folder = "Language Raws";
            string extension = ".txt";

            DirectoryInfo directoryInfo = new DirectoryInfo(folder);
            folder = Path.GetFullPath(folder);
            FileInfo[] files = directoryInfo.GetFiles("*" + extension, SearchOption.AllDirectories);
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
            foreach(Raw r in allRaws)
            {
                if (r.Game.Contains(game))
                    retVal.AddTo(r.Name, r);
            }
            return retVal;
        }
    }
}

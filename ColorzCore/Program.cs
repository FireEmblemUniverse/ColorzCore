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
            string fileName = "../../testFile.event";
            FileStream inputFile = new FileStream(fileName, FileMode.Open);

            //FirstPass(Tokenizer.Tokenize(inputStream));
            Tokenizer myTokenizer = new Tokenizer();
            EAParser myParser = new EAParser(new Dictionary<string, IList<Raw>>());

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
        /*
        private static EAInstance FirstPass(IEnumerable<Token> tokens)
        {





            throw new NotImplementedException();
        }
        */
    }
}

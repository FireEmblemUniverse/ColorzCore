using ColorzCore.Lexer;
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
            BufferedStream inputStream = new BufferedStream(inputFile);

            //FirstPass(Tokenizer.Tokenize(inputStream));

            
            foreach (Token t in Tokenizer.Tokenize(inputStream, fileName))
            {
                Console.Out.WriteLine(t.ToString());
            }

            Console.In.ReadLine();
            
            inputFile.Close();
        }

        private static EAInstance FirstPass(IEnumerable<Token> tokens)
        {





            throw new NotImplementedException();
        }
    }
}

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
            FileStream inputFile = new FileStream("../../testFile.event", FileMode.Open);
            BufferedStream inputStream = new BufferedStream(inputFile);

            //FirstPass(Tokenizer.Tokenize(inputStream));

            
            foreach (Token t in Tokenizer.Tokenize(inputStream))
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

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

            //FirstPass(Tokenizer.Tokenize(inputStream));
            EAInterpreter myInterpreter = new EAInterpreter(game);
            myInterpreter.Interpret(Console.Out, fileName);

            Console.WriteLine("Done.");

            Console.In.ReadLine();
            
        }
    }
}

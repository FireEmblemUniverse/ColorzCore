using System;

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

using System;
using System.IO;

namespace ColorzCore
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Required parameters missing.");
                return;
            }
            if(args[0] != "A")
            {
                Console.WriteLine("Only assembly is supported currently.");
                return;
            }
            string game = args[1];
            Stream inStream = Console.OpenStandardInput();
            FileStream outStream = null;
            TextWriter errorStream = Console.Error;
            string rawsFolder = "Language Raws", rawsExtension = ".txt";
            string inFileName = "stdin";
            for(int i = 2; i < args.Length; i++)
            {
                if(args[i][0] != '-')
                {
                    Console.Error.WriteLine("Unrecognized paramter: " + args[i]);
                }
                else
                {
                    string[] flag = args[i].Substring(1).Split(new char[]{':'}, 2);
                    switch(flag[0])
                    {
                        case "raws":
                            rawsFolder = flag[1];
                            break;
                        case "rawsExt":
                            rawsExtension = flag[1];
                            break;
                        case "output":
                            outStream = File.Open(flag[1], FileMode.Open, FileAccess.ReadWrite); //TODO: Handle file not found exceptions
                            break;
                        case "input":
                            inFileName = flag[1];
                            inStream = File.OpenRead(flag[1]);
                            break;
                        case "error":
                            errorStream = new StreamWriter(File.OpenWrite(flag[1]));
                            break;
                        default:
                            Console.Error.WriteLine("Unrecognized flag: " + flag[0]);
                            return;
                    }
                }
            }
            if(outStream == null)
            {
                Console.Error.WriteLine("No output specified for assembly.");
                return;
            }
            //FirstPass(Tokenizer.Tokenize(inputStream));
            EAInterpreter myInterpreter = new EAInterpreter(game, rawsFolder, rawsExtension, inStream, inFileName, outStream, errorStream);
            myInterpreter.Interpret();

            inStream.Close();
            outStream.Close();
            errorStream.Close();

            Console.WriteLine("Done.");

            Console.In.ReadLine();
            
        }
    }
}

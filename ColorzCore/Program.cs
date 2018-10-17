using System;
using System.IO;
using System.Collections.Generic;

namespace ColorzCore
{
    class Program
    {
        public static bool Debug = false;

        public const string TIMING_START   = "start";
        public const string TIMING_GENERIC = "parsing-interpreting";
        public const string TIMING_RAWPROC = "raw-processing";
        public const string TIMING_DATAWRITE = "data-writing";
        
        public static List<Tuple<DateTime, string>> timingPoints;

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
                    try
                    {


                        switch (flag[0])
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
                            case "debug":
                                Debug = true;
                                break;
                            default:
                                Console.Error.WriteLine("Unrecognized flag: " + flag[0]);
                                return;
                        }
                    }
                    catch(IOException e)
                    {
                        Console.Error.WriteLine("Exception: " + e.Message);
                        return;
                    }
                }
            }
            if(outStream == null)
            {
                Console.Error.WriteLine("No output specified for assembly.");
                return;
            }

            timingPoints = new List<Tuple<DateTime, string>>();
            timingPoints.Add(new Tuple<DateTime, string>(DateTime.Now, TIMING_START));

            //FirstPass(Tokenizer.Tokenize(inputStream));
            EAInterpreter myInterpreter = new EAInterpreter(game, rawsFolder, rawsExtension, inStream, inFileName, outStream, errorStream);

            timingPoints.Add(new Tuple<DateTime, string>(DateTime.Now, TIMING_RAWPROC));

            myInterpreter.Interpret();

            DateTime current = DateTime.Now;
            TimeSpan total = TimeSpan.Zero;

            Dictionary<string, TimeSpan> times = new Dictionary<string, TimeSpan>();
            Dictionary<string, int> count = new Dictionary<string, int>();

            foreach (Tuple<DateTime, string> point in timingPoints)
            {
                if (point.Item2 != TIMING_START)
                {
                    if (times.ContainsKey(point.Item2))
                    {
                        times[point.Item2] += point.Item1.Subtract(current);

                        switch (point.Item2)
                        {
                            case TIMING_RAWPROC:
                            case TIMING_GENERIC:
                            case TIMING_DATAWRITE:
                                break;

                            default:
                                count[point.Item2] += 1;
                                break;
                        }
                    }
                    else
                    {
                        times[point.Item2] = point.Item1.Subtract(current);
                        count[point.Item2] = 1;
                    }
                }

                current = point.Item1;
            }

            errorStream.WriteLine("Times:");

            SortedList<TimeSpan, string> sortedTimes = new SortedList<TimeSpan, string>();

            foreach (KeyValuePair<string, TimeSpan> time in times)
            {
                sortedTimes.Add(time.Value, time.Key);
                total += time.Value;
            }

            foreach (KeyValuePair<TimeSpan, string> time in sortedTimes)
            {
                errorStream.WriteLine("  " + time.Value + ": " + time.Key.ToString() + " (" + count[time.Value] + ")");
            }

            errorStream.WriteLine("Total:");
            errorStream.WriteLine("  " + total.ToString());

            inStream.Close();
            outStream.Close();
            errorStream.Close();
        }
    }
}

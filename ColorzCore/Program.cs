using System;
using System.IO;
using System.Collections.Generic;

namespace ColorzCore
{
    class Program
    {
        public static bool Debug = false;
        public static ExecTimer Timer = null;

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

            Timer = new ExecTimer();

            //FirstPass(Tokenizer.Tokenize(inputStream));
            EAInterpreter myInterpreter = new EAInterpreter(game, rawsFolder, rawsExtension, inStream, inFileName, outStream, errorStream);
            Timer.AddTimingPoint(ExecTimer.KEY_RAWPROC);

            myInterpreter.Interpret();

            // Print times

            errorStream.WriteLine();
            errorStream.WriteLine("Times:");

            foreach (KeyValuePair<TimeSpan, string> time in Timer.SortedTimes)
            {
                errorStream.WriteLine("  " + time.Value + ": " + time.Key.ToString() + " (" + Timer.Counts[time.Value] + ")");
            }

            // Print total time

            errorStream.WriteLine();
            errorStream.WriteLine("Total:");

            errorStream.WriteLine("  " + Timer.TotalTime.ToString());

            inStream.Close();
            outStream.Close();
            errorStream.Close();
        }

        public class ExecTimer
        {
            public const string KEY_RESET   = "__reset";
            public const string KEY_GENERIC = "parsing-interpreting";
            public const string KEY_RAWPROC = "raw-processing";
            public const string KEY_DATAWRITE = "data-writing";

            private List<Tuple<DateTime, string>> timingPoints;

            private Dictionary<string, TimeSpan> times;
            private Dictionary<string, int> counts;

            private TimeSpan totalTime;

            public SortedList<TimeSpan, string> SortedTimes
            {
                get
                {
                    if (this.times == null)
                        ComputeTimes();

                    SortedList<TimeSpan, string> sortedTimes = new SortedList<TimeSpan, string>();

                    foreach (KeyValuePair<string, TimeSpan> time in this.times)
                        sortedTimes.Add(time.Value, time.Key);

                    return sortedTimes;
                }
            }

            public Dictionary<string, TimeSpan> Times
            {
                get
                {
                    if (this.times == null)
                        ComputeTimes();

                    return this.times;
                }
            }

            public Dictionary<string, int> Counts
            {
                get
                {
                    if (this.counts == null)
                        ComputeTimes();

                    return this.counts;
                }
            }

            public TimeSpan TotalTime
            {
                get
                {
                    if (this.totalTime == null)
                        ComputeTimes();

                    return this.totalTime;
                }
            }

            public ExecTimer()
            {
                timingPoints = new List<Tuple<DateTime, string>>();
                timingPoints.Add(new Tuple<DateTime, string>(DateTime.Now, KEY_RESET));

                times = null;
                counts = null;
            }

            public void AddTimingPoint(string key)
            {
                timingPoints.Add(new Tuple<DateTime, string>(DateTime.Now, key));
            }

            private void ComputeTimes()
            {
                DateTime current = DateTime.Now;

                this.times  = new Dictionary<string, TimeSpan>();
                this.counts = new Dictionary<string, int>();

                this.totalTime = TimeSpan.Zero;

                foreach (Tuple<DateTime, string> point in timingPoints)
                {
                    if (point.Item2 != KEY_RESET)
                    {
                        TimeSpan span = point.Item1.Subtract(current);

                        if (times.ContainsKey(point.Item2))
                        {
                            times[point.Item2] += span;

                            switch (point.Item2)
                            {
                                case KEY_RAWPROC:
                                case KEY_GENERIC:
                                case KEY_DATAWRITE:
                                    break;

                                default:
                                    counts[point.Item2] += 1;
                                    break;
                            }
                        }
                        else
                        {
                            times[point.Item2] = span;
                            counts[point.Item2] = 1;
                        }

                        totalTime += span;
                    }

                    current = point.Item1;
                }
            }
        }
    }
}

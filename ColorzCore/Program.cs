using System;
using System.IO;
using System.Collections.Generic;
using ColorzCore.IO;
using ColorzCore.DataTypes;

namespace ColorzCore
{
    class Program
    {
        public static bool Debug = false;

        public static ExecTimer Timer = null;

        private static string[] helpstringarr = {"EA Colorz Core. Usage:",
            "./ColorzCore <A|D> <game> [-opts]",
            "",
            "Only A is allowed as assembly mode currently.",
            "Game may be any string; the respective _game_ variable gets defined in scripts.",
            "Available options:",
            "-raws:<dir>",
            "   Sets the raws directory to the one provided (relative to ColorzCore). Defaults to \"Language Raws\".",
            "-rawsExt:<ext>",
            "   Sets the extension of files used for raws to the one provided. Defaults to .txt.",
            "-output:<filename>",
            "   Set the file to write assembly to.",
            "-input:<filename>",
            "   Set the file to take input script from. Defaults to stdin.",
            "-error:<filename>",
            "   Set a file to redirect messages, warnings, and errors to. Defaults to stderr.",
            "-werr",
            "   Treat all warnings as errors and prevent assembly.",
            "--no-mess",
            "   Suppress output of messages.",
            "--no-warn",
            "   Suppress output of warnings.",
            "--quiet",
            "   Equivalent to --no-mess --no-warn.",
            "-h|--help",
            "   Display helpstring and exit.",
            "-debug",
            "   Enable debug mode. Not recommended for end users."};
        private static string helpstring = System.Linq.Enumerable.Aggregate(helpstringarr, (String a, String b) => { return a + '\n' + b; }) + '\n';

        private const int EXIT_SUCCESS = 0;
        private const int EXIT_FAILURE = 1;

        static int Main(string[] args)
        {
            EAOptions options = new EAOptions();

            IncludeFileSearcher rawSearcher = new IncludeFileSearcher();
            rawSearcher.IncludeDirectories.Add(AppDomain.CurrentDomain.BaseDirectory);

            Stream inStream = Console.OpenStandardInput();
            string inFileName = "stdin";

            FileStream outStream = null;
            string outFileName = "none";

            TextWriter errorStream = Console.Error;

            Maybe<string> rawsFolder = rawSearcher.FindDirectory("Language Raws");
            string rawsExtension = ".txt";

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i][0] != '-')
                {
                    Console.Error.WriteLine("Unrecognized paramter: " + args[i]);
                }
                else
                {
                    string[] flag = args[i].Substring(1).Split(new char[] { ':' }, 2);

                    try
                    {
                        switch (flag[0])
                        {
                            case "raws":
                                rawsFolder = rawSearcher.FindDirectory(flag[1]);
                                break;

                            case "rawsExt":
                                rawsExtension = flag[1];
                                break;

                            case "output":
                                outFileName = flag[1];
                                outStream = File.Open(outFileName, FileMode.Open, FileAccess.ReadWrite); //TODO: Handle file not found exceptions
                                break;

                            case "input":
                                inFileName = flag[1];
                                inStream = File.OpenRead(flag[1]);
                                break;

                            case "error":
                                errorStream = new StreamWriter(File.OpenWrite(flag[1]));
                                options.noColoredLog = true;
                                break;

                            case "debug":
                                Debug = true;
                                break;

                            case "werr":
                                options.werr = true;
                                break;

                            case "-no-mess":
                                options.nomess = true;
                                break;

                            case "-no-warn":
                                options.nowarn = true;
                                break;

                            case "-no-colored-log":
                                options.noColoredLog = true;
                                break;

                            case "quiet":
                                options.nomess = true;
                                options.nowarn = true;
                                break;

                            case "-nocash-sym":
                                options.nocashSym = true;
                                break;

                            case "I":
                            case "-include":
                                options.includePaths.Add(flag[1]);
                                break;

                            case "T":
                            case "-tools":
                                options.toolsPaths.Add(flag[1]);
                                break;

                            case "IT":
                            case "TI":
                                options.includePaths.Add(flag[1]);
                                options.toolsPaths.Add(flag[1]);
                                break;

                            case "h":
                            case "-help":
                                Console.Out.WriteLine(helpstring);
                                return EXIT_SUCCESS;

                            default:
                                Console.Error.WriteLine("Unrecognized flag: " + flag[0]);
                                return EXIT_FAILURE;
                        }
                    }
                    catch (IOException e)
                    {
                        Console.Error.WriteLine("Exception: " + e.Message);
                        return EXIT_FAILURE;
                    }
                }
            }

            if (args.Length < 2)
            {
                Console.WriteLine("Required parameters missing.");
                return EXIT_FAILURE;
            }

            if (args[0] != "A")
            {
                Console.WriteLine("Only assembly is supported currently.");
                return EXIT_FAILURE;
            }

            if (outStream == null)
            {
                Console.Error.WriteLine("No output specified for assembly.");
                return EXIT_FAILURE;
            }

            if (rawsFolder.IsNothing)
            {
                Console.Error.WriteLine("Couldn't find raws folder");
                return EXIT_FAILURE;
            }

            Timer = new ExecTimer();

            string game = args[1];

            //FirstPass(Tokenizer.Tokenize(inputStream));

            Log log = new Log {
                Output = errorStream,
                WarningsAreErrors = options.werr,
                NoColoredTags = options.noColoredLog
            };

            if (options.nowarn)
                log.IgnoredKinds.Add(Log.MsgKind.WARNING);

            if (options.nomess)
                log.IgnoredKinds.Add(Log.MsgKind.MESSAGE);

            EAInterpreter myInterpreter = new EAInterpreter(game, rawsFolder.FromJust, rawsExtension, inStream, inFileName, outStream, log, options);

            Timer.AddTimingPoint(ExecTimer.KEY_RAWPROC);

            bool success = myInterpreter.Interpret();

            if (success && options.nocashSym)
            {
                using (var output = File.CreateText(Path.ChangeExtension(outFileName, "sym")))
                {
                    if (!(success = myInterpreter.WriteNocashSymbols(output)))
                    {
                        log.Message(Log.MsgKind.ERROR, "Error trying to write no$gba symbol file.");
                    }
                }
            }

            // Print times

            log.Output.WriteLine();
            log.Output.WriteLine("Times:");

            foreach (KeyValuePair<TimeSpan, string> time in Timer.SortedTimes)
            {
                log.Output.WriteLine("  " + time.Value + ": " + time.Key.ToString() + " (" + Timer.Counts[time.Value] + ")");
            }

            // Print total time

            log.Output.WriteLine();
            log.Output.WriteLine("Total:");

            log.Output.WriteLine("  " + Timer.TotalTime.ToString());

            inStream.Close();
            outStream.Close();
            errorStream.Close();

            return success ? EXIT_SUCCESS : EXIT_FAILURE;
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

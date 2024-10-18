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

        private static readonly IDictionary<string, EAOptions.Warnings> warningNames = new Dictionary<string, EAOptions.Warnings>()
        {
            { "nonportable-pathnames", EAOptions.Warnings.NonPortablePath },
            { "unintuitive-expression-macros" , EAOptions.Warnings.UnintuitiveExpressionMacros },
            { "unguarded-expression-macros", EAOptions.Warnings.UnguardedExpressionMacros },
            { "redefine", EAOptions.Warnings.ReDefine },
            { "legacy", EAOptions.Warnings.LegacyFeatures },
            { "all", EAOptions.Warnings.All },
            { "extra", EAOptions.Warnings.Extra },
        };

        private static readonly string[] helpstringarr = {
            "EA Colorz Core. Usage:",
            "./ColorzCore <A|D|AA> <game> [-opts]",
            "",
            "A is to write ROM directly",
            "AA is to output assembly source file and linker script",
            "D is not allowed currently.",
            "Game may be any string; the respective _game_ variable gets defined in scripts.",
            "",
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
            "--nocash-sym",
            "   Outputs a no$ compatible .sym file corresponding to the output file.",
            "-I:<path>|--inlude:<path>",
            "   Add given path to list of paths to search for included files in.",
            "-T:<path>|--tools:<path>",
            "   Add given path to list of paths to search for tools in.",
            "-IT:<path>|-TI:<path>",
            "   Combines --include:<path> and --tools:<path>.",
            "-W:[no-]<name>:...|--warnings:[no-]<name>:...",
            "   Enable or disable warnings.",
            "   By default, all warnings but 'unguarded-expression-macros' are enabled.",
            "   Multiple warnings can be enabled/disabled at once.",
            "   Example: '--warnings:no-nonportable-pathnames:no-redefine'.",
            "   Possible values: " + string.Join(", ", warningNames.Keys),
            "-werr",
            "   Treat all warnings as errors and prevent assembly.",
            "--no-mess",
            "   Suppress output of messages.",
            "--no-warn",
            "   Suppress output of warnings.",
            "-quiet",
            "   Equivalent to --no-mess --no-warn.",
            "--no-colored-log",
            "   Don't use colored log tags when outputting logs to console/stderr.",
            "-[D|def|define]:<defname>=<defvalue>",
            "   Assembles as if \"#define <defname> <defvalue>\" were at the top of the input stream.",
            "-debug",
            "   Enable debug mode. Not recommended for end users.",
            "--build-times",
            "   Print build times at the end of build.",
            "--base-address:<number>",
            "   Treats the base load address of the binary as the given (hexadecimal) number,",
            "   for the purposes of POIN, ORG and CURRENTOFFSET. Defaults to 0x08000000.",
            "   Addresses are added to offsets from 0 to the maximum binary size.",
            "--maximum-size:<number>",
            "   Sets the maximum size of the binary. Defaults to 0x02000000.",
            "-romoffset:<number>",
            "   Compatibility alias for --base-address:<number>",
            "-h|--help",
            "   Display this message and exit.",
            ""
        };

        private static readonly string helpstring = System.Linq.Enumerable.Aggregate(helpstringarr,
            (string a, string b) => { return a + '\n' + b; });

        private const int EXIT_SUCCESS = 0;
        private const int EXIT_FAILURE = 1;

        static int Main(string[] args)
        {
            IncludeFileSearcher rawSearcher = new IncludeFileSearcher();
            rawSearcher.IncludeDirectories.Add(AppDomain.CurrentDomain.BaseDirectory);

            Stream inStream = Console.OpenStandardInput();
            string inFileName = "stdin";

            string outFileName = "none";
            string ldsFileName = "none";

            TextWriter errorStream = Console.Error;

            string? rawsFolder = rawSearcher.FindDirectory("Language Raws");
            string rawsExtension = ".txt";

            if (args.Length < 2)
            {
                Console.WriteLine(helpstring);
                return EXIT_FAILURE;
            }

            bool outputASM = false;
            if (args[0] == "AA")
                outputASM = true;
            else
                if (args[0] != "A")
            {
                Console.WriteLine("Only assembly is supported currently. Please run as \"./ColorzCore.exe A ...\".");
                return EXIT_FAILURE;
            }

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i][0] != '-')
                {
                    Console.Error.WriteLine("Unrecognized paramter: " + args[i]);
                    continue;
                }

                string[] flag = args[i].Split(new char[] { ':' }, 2);

                try
                {
                    switch (flag[0])
                    {
                        case "-raws":
                            rawsFolder = rawSearcher.FindDirectory(flag[1]);

                            if (rawsFolder == null)
                            {
                                Console.Error.WriteLine($"No such folder: {flag[1]}");
                                return EXIT_FAILURE;
                            }

                            break;

                        case "-rawsExt":
                            rawsExtension = flag[1];
                            break;

                        case "-output":
                            outFileName = flag[1];
                            break;

                        case "-input":
                            inFileName = flag[1].Replace('\\', '/');
                            inStream = File.OpenRead(flag[1]);
                            break;

                        case "-error":
                            errorStream = new StreamWriter(File.OpenWrite(flag[1]));
                            EAOptions.MonochromeLog = true;
                            break;

                        case "-debug":
                            Debug = true;
                            break;

                        case "-werr":
                            EAOptions.WarningsAreErrors = true;
                            break;

                        case "--no-mess":
                            EAOptions.QuietMessages = true;
                            break;

                        case "--no-warn":
                            EAOptions.QuietWarnings = true;
                            break;

                        case "--no-colored-log":
                            EAOptions.MonochromeLog = true;
                            break;

                        case "-quiet":
                        case "--quiet":
                            EAOptions.QuietMessages = true;
                            EAOptions.QuietWarnings = true;
                            break;

                        case "--nocash-sym":
                            EAOptions.ProduceNocashSym = true;
                            break;

                        case "--build-times":
                            EAOptions.BenchmarkBuildTimes = true;
                            break;

                        case "-I":
                        case "--include":
                            EAOptions.IncludePaths.Add(flag[1]);
                            break;

                        case "-T":
                        case "--tools":
                            EAOptions.ToolsPaths.Add(flag[1]);
                            break;

                        case "-IT":
                        case "-TI":
                            EAOptions.IncludePaths.Add(flag[1]);
                            EAOptions.ToolsPaths.Add(flag[1]);
                            break;

                        case "-h":
                        case "--help":
                            Console.Out.WriteLine(helpstring);
                            return EXIT_SUCCESS;

                        case "-D":
                        case "-def":
                        case "-define":
                            try
                            {
                                string[] def_args = flag[1].Split(new char[] { '=' }, 2);
                                EAOptions.PreDefintions.Add((def_args[0], def_args[1]));
                            }
                            catch (IndexOutOfRangeException)
                            {
                                Console.Error.WriteLine("Improperly formed -define directive.");
                            }
                            break;

                        case "-romoffset":
                        case "--base-address":
                            try
                            {
                                EAOptions.BaseAddress = Convert.ToInt32(flag[1], 16);
                            }
                            catch
                            {
                                Console.Error.WriteLine("Invalid hex base address given for binary.");
                            }
                            break;

                        case "--maximum-size":
                            try
                            {
                                EAOptions.MaximumBinarySize = Convert.ToInt32(flag[1], 16);
                            }
                            catch
                            {
                                Console.Error.WriteLine("Invalid hex size given for binary.");
                            }
                            break;

                        case "-W":
                        case "--warnings":
                            if (flag.Length == 1)
                            {
                                EAOptions.EnabledWarnings |= EAOptions.Warnings.All;
                            }
                            else
                            {
                                foreach (string warning in flag[1].Split(':'))
                                {
                                    string name = warning;
                                    bool invert = false;

                                    if (name.StartsWith("no-"))
                                    {
                                        name = name.Substring(3);
                                        invert = true;
                                    }

                                    if (warningNames.TryGetValue(name, out EAOptions.Warnings warnFlag))
                                    {
                                        if (invert)
                                        {
                                            EAOptions.EnabledWarnings &= ~warnFlag;
                                        }
                                        else
                                        {
                                            EAOptions.EnabledWarnings |= warnFlag;
                                        }
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine($"Unrecognized warning: {name}");
                                    }
                                }
                            }

                            break;

                        default:
                            Console.Error.WriteLine($"Unrecognized flag: {flag[0]}");
                            return EXIT_FAILURE;
                    }
                }
                catch (IOException e)
                {
                    Console.Error.WriteLine("Exception: " + e.Message);
                    return EXIT_FAILURE;
                }
            }

            if (outFileName == null)
            {
                Console.Error.WriteLine("No output specified for assembly.");
                return EXIT_FAILURE;
            }

            IOutput output;

            if (outputASM)
            {
                ldsFileName = Path.ChangeExtension(outFileName, "lds");
                output = new ASM(new StreamWriter(outFileName, false),
                                 new StreamWriter(ldsFileName, false));
            }
            else
            {
                FileStream outStream;

                if (File.Exists(outFileName) && !File.GetAttributes(outFileName).HasFlag(FileAttributes.ReadOnly))
                {
                    outStream = File.Open(outFileName, FileMode.Open, FileAccess.ReadWrite);
                }
                else if (!File.Exists(outFileName))
                {
                    outStream = File.Create(outFileName);
                }
                else
                {
                    Console.Error.WriteLine("Output file is read-only.");
                    return EXIT_FAILURE;
                }

                output = new ROM(outStream, EAOptions.MaximumBinarySize);
            }

            string game = args[1];

            //FirstPass(Tokenizer.Tokenize(inputStream));

            Logger log = new Logger
            {
                Output = errorStream,
                WarningsAreErrors = EAOptions.WarningsAreErrors,
                NoColoredTags = EAOptions.MonochromeLog,
                LocationBasePath = IOUtility.GetPortableBasePathForPrefix(inFileName),
            };

            if (EAOptions.QuietWarnings)
                log.IgnoredKinds.Add(Logger.MessageKind.WARNING);

            if (EAOptions.QuietMessages)
                log.IgnoredKinds.Add(Logger.MessageKind.MESSAGE);

            EADriver myDriver = new EADriver(output, game, rawsFolder, rawsExtension, inStream, inFileName, log);

            ExecTimer.Timer.AddTimingPoint(ExecTimer.KEY_RAWPROC);

            bool success = myDriver.Interpret();

            if (success && EAOptions.ProduceNocashSym)
            {
                using StreamWriter symOut = File.CreateText(Path.ChangeExtension(outFileName, "sym"));

                if (!(success = myDriver.WriteNocashSymbols(symOut)))
                {
                    log.Message(Logger.MessageKind.ERROR, "Error trying to write no$gba symbol file.");
                }
            }

            if (EAOptions.BenchmarkBuildTimes)
            {
                // Print times

                log.Output.WriteLine();
                log.Output.WriteLine("Times:");

                foreach (KeyValuePair<TimeSpan, string> time in ExecTimer.Timer.SortedTimes)
                {
                    log.Output.WriteLine("  " + time.Value + ": " + time.Key.ToString() + " (" + ExecTimer.Timer.Counts[time.Value] + ")");
                }

                // Print total time

                log.Output.WriteLine();
                log.Output.WriteLine("Total:");

                log.Output.WriteLine("  " + ExecTimer.Timer.TotalTime.ToString());
            }

            inStream.Close();
            output.Close();
            errorStream.Close();

            return success ? EXIT_SUCCESS : EXIT_FAILURE;
        }
    }
}

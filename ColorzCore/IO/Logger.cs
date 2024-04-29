using System;
using System.Collections.Generic;
using System.IO;
using ColorzCore.DataTypes;

namespace ColorzCore.IO
{
    public class Logger
    {
        public enum MessageKind
        {
            ERROR,
            WARNING,
            NOTE,
            MESSAGE,
            DEBUG,
            CONTINUE,
        }

        public bool HasErrored { get; private set; } = false;
        public bool WarningsAreErrors { get; set; } = false;

        public bool NoColoredTags { get; set; } = false;

        public string LocationBasePath { get; set; } = string.Empty;

        public List<MessageKind> IgnoredKinds { get; } = new List<MessageKind>();

        public TextWriter Output { get; set; } = Console.Error;

        private MessageKind continueKind = MessageKind.CONTINUE;
        private int continueLength = 0;

        protected struct LogDisplayConfig
        {
            public string tag;
            public ConsoleColor? tagColor;
        }

        protected static readonly Dictionary<MessageKind, LogDisplayConfig> KIND_DISPLAY_DICT = new Dictionary<MessageKind, LogDisplayConfig>
        {
            { MessageKind.ERROR, new LogDisplayConfig { tag = "error", tagColor = ConsoleColor.Red } },
            { MessageKind.WARNING, new LogDisplayConfig { tag = "warning", tagColor = ConsoleColor.Magenta } },
            { MessageKind.NOTE, new LogDisplayConfig { tag = "note", tagColor = null } },
            { MessageKind.MESSAGE, new LogDisplayConfig { tag = "message", tagColor = ConsoleColor.Blue } },
            { MessageKind.DEBUG, new LogDisplayConfig { tag = "debug", tagColor = ConsoleColor.Green } }
        };

        public void Message(string message)
        {
            Message(MessageKind.MESSAGE, null, message);
        }

        public void Message(MessageKind kind, string message)
        {
            Message(kind, null, message);
        }

        public void Message(MessageKind kind, Location? source, string message)
        {
            if (WarningsAreErrors && (kind == MessageKind.WARNING))
            {
                kind = MessageKind.ERROR;
            }

            HasErrored |= kind == MessageKind.ERROR;

            if (kind == MessageKind.CONTINUE)
            {
                if (continueKind != MessageKind.CONTINUE && !IgnoredKinds.Contains(continueKind))
                {
                    Console.Write("".PadLeft(continueLength));
                    Console.WriteLine(message);
                }
            }
            else if (!IgnoredKinds.Contains(kind))
            {
                if (KIND_DISPLAY_DICT.TryGetValue(kind, out LogDisplayConfig config))
                {
                    if (!NoColoredTags && config.tagColor.HasValue)
                    {
                        Console.ForegroundColor = config.tagColor.Value;
                    }

                    Output.Write($"{config.tag}: ");
                    continueLength = config.tag.Length + 2;

                    if (!NoColoredTags)
                        Console.ResetColor();

                    if (source.HasValue)
                    {
                        string locString = source.Value.ToString();

                        if (locString.StartsWith(LocationBasePath))
                        {
                            locString = locString.Substring(LocationBasePath.Length);
                        }

                        Output.Write($"{locString}: ");
                        continueLength += locString.Length + 2;
                    }

                    Output.WriteLine(message);

                    continueKind = kind;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ColorzCore.DataTypes;
using ColorzCore.Interpreter.Diagnostics;
using ColorzCore.IO;
using ColorzCore.Lexer;
using ColorzCore.Parser;
using ColorzCore.Parser.AST;

namespace ColorzCore.Interpreter
{
    // String handling the various processing of strings (encoding, formatting)
    public class StringProcessor
    {
        public IDictionary<string, TblEncoding> TableEncodings { get; } = new Dictionary<string, TblEncoding>();
        public Logger Logger { get; }

        public StringProcessor(Logger logger)
        {
            Logger = logger;
        }

        public byte[] EncodeString(Location locationForLogger, string inputString, string? encodingName)
        {
            encodingName ??= "UTF-8";

            switch (encodingName.ToUpperInvariant())
            {
                case "UTF-8":
                case "UTF8":
                    try
                    {
                        return Encoding.UTF8.GetBytes(inputString);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(locationForLogger,
                            $"Failed to encode string '{inputString}': {e.Message}.");
                        return Array.Empty<byte>();
                    }
            }

            if (TableEncodings.TryGetValue(encodingName, out TblEncoding? encoding))
            {
                try
                {
                    return encoding.ConvertToBytes(inputString);
                }
                catch (Exception e)
                {
                    Logger.Error(locationForLogger,
                        $"Failed to encode string '{inputString}': {e.Message}.");
                }
            }
            else
            {
                Logger.Error(locationForLogger,
                    $"Unknown encoding: '{encodingName}'.");
            }

            return Array.Empty<byte>();
        }

        private static readonly Regex formatItemRegex = new Regex(@"\{(?<expr>[^:}]+)(?:\:(?<format>[^:}]*))?\}");

        string UserFormatStringError(Location loc, string message, string details)
        {
            Logger.Error(loc, $"An error occurred while expanding format string ({message}).");
            return $"<{message}: {details}>";
        }

        public string ExpandUserFormatString(Location location, EAParser parser, string stringValue)
        {
            return formatItemRegex.Replace(stringValue, match =>
            {
                string expr = match.Groups["expr"].Value!;
                string? format = match.Groups["format"].Value;

                return ExpandFormatItem(parser, location.OffsetBy(match.Index), expr, format);
            });
        }

        public string ExpandFormatItem(EAParser parser, Location location, string expr, string? format = null)
        {
            MergeableGenerator<Token> tokens = new MergeableGenerator<Token>(
                Tokenizer.TokenizeLine($"{expr} \n", location));

            tokens.MoveNext();

            IParamNode? node = parser.ParseParam(tokens);

            if (node == null || tokens.Current.Type != TokenType.NEWLINE)
            {
                return UserFormatStringError(location, "bad expression", $"'{expr}'");
            }

            switch (node)
            {
                case IAtomNode atom:
                    if (atom.TryEvaluate(e => Logger.Error(atom.MyLocation, e.Message),
                        EvaluationPhase.Immediate) is int value)
                    {
                        try
                        {
                            // TODO: do we need to raise an error when result == format?
                            // this happens (I think) when a custom format specifier with no substitution
                            return value.ToString(format, CultureInfo.InvariantCulture);
                        }
                        catch (FormatException e)
                        {
                            return UserFormatStringError(node.MyLocation,
                                "bad format specifier", $"'{format}' ({e.Message})");
                        }
                    }
                    else
                    {
                        return UserFormatStringError(node.MyLocation,
                            "failed to evaluate expression", $"'{expr}'");
                    }

                case StringNode stringNode:
                    if (!string.IsNullOrEmpty(format))
                    {
                        return UserFormatStringError(node.MyLocation,
                            "string items cannot specify format", $"'{format}'");
                    }
                    else
                    {
                        return stringNode.Value;
                    }

                default:
                    return UserFormatStringError(node.MyLocation,
                        "invalid format item type", $"'{DiagnosticsHelpers.PrettyParamType(node.Type)}'");
            }
        }
    }
}

using ColorzCore.DataTypes;
using System;
using System.Collections.Generic;

namespace ColorzCore.Lexer
{
    public class Token
    {
        public TokenType Type { get; }
        public string Content { get; }
        public Location Location { get; }

        public string FileName => Location.file;
        public int LineNumber => Location.line;
        public int ColumnNumber => Location.column;

        public Token(TokenType type, Location location, string content = "")
        {
            Type = type;
            Location = location;
            Content = content;
        }

        public override string ToString() => $"{Location}, {Type}: {Content}";

        public Token MacroClone(MacroLocation macroLocation) => new Token(Type, Location.MacroClone(macroLocation), Content);

        // used for __LINE__ and __FILE__
        public Location GetSourceLocation()
        {
            Location location = Location;

            while (location.macroLocation != null)
            {
                location = location.macroLocation.Location;
            }

            return location;
        }
    }
}

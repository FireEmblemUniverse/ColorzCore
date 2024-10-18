using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    public struct Location
    {
        public string file;
        public int line, column;
        public MacroLocation? macroLocation;

        public Location(string fileName, int lineNum, int colNum, MacroLocation? macro = null) : this()
        {
            file = fileName;
            line = lineNum;
            column = colNum;
            macroLocation = macro;
        }

        public readonly Location OffsetBy(int columns) => new Location(file, line, column + columns, macroLocation);
        public readonly Location MacroClone(MacroLocation macro) => new Location(file, line, column, macro);

        public override readonly string ToString() => $"{file}:{line}:{column}";
    }
}

namespace ColorzCore.DataTypes
{
    public class MacroLocation
    {
        public string MacroName { get; }
        public Location Location { get; }

        public MacroLocation(string macroName, Location location)
        {
            MacroName = macroName;
            Location = location;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.Macros
{
    class MacroCollection
    {
        private static readonly Dictionary<string, BuiltInMacro> BuiltInMacros = new Dictionary<string, BuiltInMacro> { { "String", String.Instance } };

        private Dictionary<string, Dictionary<int, IMacro>> Macros { get; }

        public MacroCollection()
        {
            Macros = new Dictionary<string, Dictionary<int, IMacro>>();
        }

        public bool HasMacro(string name, int paramNum)
        {
            return BuiltInMacros.ContainsKey(name) && BuiltInMacros[name].ValidNumParams(paramNum) || Macros.ContainsKey(name) && Macros[name].ContainsKey(paramNum);
        }
        public IMacro GetMacro(string name, int paramNum)
        {
            return BuiltInMacros.ContainsKey(name) && BuiltInMacros[name].ValidNumParams(paramNum) ? BuiltInMacros[name] : Macros[name][paramNum];
        }
        public void AddMacro(IMacro macro, string name, int paramNum)
        {
            if (!Macros.ContainsKey(name))
                Macros[name] = new Dictionary<int, IMacro>();
            Macros[name][paramNum] = macro;
        }
        public void Clear()
        {
            Macros.Clear();
        }

        public bool ContainsName(string name)
        {
            return BuiltInMacros.ContainsKey(name) || Macros.ContainsKey(name);
        }
    }
}

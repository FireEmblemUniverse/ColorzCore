using System;
using System.Collections.Generic;
using System.Linq;
using ColorzCore.Parser;
using ColorzCore.Preprocessor.Macros;

namespace ColorzCore.Preprocessor
{
    public class MacroCollection
    {
        public Dictionary<string, BuiltInMacro> BuiltInMacros { get; }
        public EAParser Parent { get; }

        private Dictionary<string, Dictionary<int, IMacro>> Macros { get; }

        public MacroCollection(EAParser parent)
        {
            Macros = new Dictionary<string, Dictionary<int, IMacro>>();
            Parent = parent;

            BuiltInMacros = new Dictionary<string, BuiltInMacro>
            {
                { "String", new StringMacro() },
                { "IsDefined", new IsDefined(parent) },
            };
        }

        public bool HasMacro(string name, int paramNum)
        {
            return BuiltInMacros.ContainsKey(name) && BuiltInMacros[name].ValidNumParams(paramNum) || Macros.ContainsKey(name) && Macros[name].ContainsKey(paramNum);
        }

        // NOTE: NotNullWhen(true) is not available on .NET Framework 4.x, one of our targets
        public bool TryGetMacro(string name, int paramNum, out IMacro? macro)
        {
            if (BuiltInMacros.TryGetValue(name, out BuiltInMacro? builtinMacro) && builtinMacro.ValidNumParams(paramNum))
            {
                macro = builtinMacro;
                return true;
            }

            if (Macros.TryGetValue(name, out Dictionary<int, IMacro>? macros))
            {
                return macros.TryGetValue(paramNum, out macro);
            }

            macro = null;
            return false;
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

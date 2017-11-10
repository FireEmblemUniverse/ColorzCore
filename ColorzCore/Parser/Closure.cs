using System.Collections.Generic;

namespace ColorzCore.Parser
{
    public class Closure
    {
        public Dictionary<string, int> Labels { get; }
        public string IncludedBy {get; }

        public Closure(string includedBy)
        {
            Labels = new Dictionary<string, int>();
            IncludedBy = includedBy;
        }
    }
}
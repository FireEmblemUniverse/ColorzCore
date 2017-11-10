using System.Collections.Generic;

namespace ColorzCore.Parser
{
    public class Closure
    {
        public Dictionary<string, int> Labels { get; private set; }
        public string IncludedBy {get; private set;}

        public Closure(string includedBy)
        {
            Labels = new Dictionary<string, int>();
            IncludedBy = includedBy;
        }
    }
}
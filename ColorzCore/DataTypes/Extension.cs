using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    static class Extension
    {
        public static IEnumerable<T> PadTo<T>(this IEnumerable<T> self, int totalLength, T zero)
        {
            int count = 0;
            foreach (T t in self)
            {
                yield return t;
                count++;
            }
            for (; count < totalLength; count++)
                yield return zero; 
        }
    }
}

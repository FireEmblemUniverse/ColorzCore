using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Raws
{
    class Raw
    {
        public string Name { get; };
        public int Length;
        private short Code;
        private IList<IRawParam> myParams;
        private IList<Tuple<int, int, int>> fixedParams; //position, length, value
        
        public static Raw ParseRaw() {} //TODO
        
        /* Precondition: params fits the shape of this raw's params. */
        public IEnumerable<byte> GetBytes(IList<IParamNode> params)
        {
            //Represent a code's bytes as a list/array of its length.
            byte[] myBytes = new byte[Length];
            myBytes[0] = Code & 0xFF;
            myBytes[1] = Code >> 0x8;
            for(int i=0; i<myParams.Count; i++)
            {
                byte[] paramOutput = new byte[](myParams[i].Fit(params[i]));
                for(int j = 0; j < myParams.Length; j++)
                {
                    myBytes[myParams.Position + j] = paramOutput[j];
                }
            }
            foreach(Tuple<int, int, int> fp in fixedParams)
            {
                int val = fp.Value3;
                for(int i = fp.Value1; i<fp.Value1+fp.Value2; i++, val >> 8)
                {
                    myBytes[i] = val & 0xFF;
                }
            }
        }
    }
}

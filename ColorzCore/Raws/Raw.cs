using ColorzCore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Raws
{
    class Raw
    {
        public string Name { get; }
        public int Length;
        private short Code;
        private IList<IRawParam> myParams;
        private IList<Tuple<int, int, int>> fixedParams; //position, length, value
        
        //public static Raw ParseRaw() {} //TODO

        public bool Fits(IList<IParamNode> parameters)
        {
            //TODO
            throw new NotImplementedException();
        }
        
        /* Precondition: params fits the shape of this raw's params. */
        public byte[] GetBytes(IList<IParamNode> parameters)
        {
            //Represent a code's bytes as a list/array of its length.
            byte[] myBytes = new byte[Length];
            myBytes[0] = (byte) Code;
            myBytes[1] = (byte) (Code >> 0x8);
            for(int i=0; i<myParams.Count; i++)
            {
                IList<byte> fit = new List<byte>(myParams[i].Fit(parameters[i]));
                for(int j = 0; j < myParams[i].Length; j++)
                {
                    myBytes[myParams[i].Position + j] = fit[j];
                }
            }
            foreach(Tuple<int, int, int> fp in fixedParams)
            {
                int val = fp.Item3;
                for(int i = fp.Item1; i<fp.Item1+fp.Item2; i++, val >>= 8)
                {
                    myBytes[i] = (byte) val;
                }
            }
            return myBytes;
        }
    }
}

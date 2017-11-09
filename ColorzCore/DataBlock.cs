using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore
{
    class DataBlock
    {
        public int Offset { get; private set; }
        public IList<IWritable> Data { get; private set; }

        public void Write(byte[] target)
        {
            int runningOffset = Offset;
            for(int i=0; i<Data.Count; i++)
            {
                byte[] toWrite = Data[i].Bytes;
                for(int j=0; j<toWrite.Length; j++)
                {
                    target[runningOffset + j] = toWrite[j]
                }
                runningOffset += toWrite.Length;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore
{
    class WritableInt : IWritable
    {
        private int myInt;

        public WritableInt(int value)
        {
            myInt = value;
        }

        public byte[] Bytes { get {
                byte[] myBytes = { (byte)myInt, (byte)(myInt>>8), (byte)(myInt>>16), (byte)(myInt>>32) };
                return myBytes; } }
    }
}

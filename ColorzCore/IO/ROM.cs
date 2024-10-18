using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ColorzCore.IO
{
    public class ROM : IOutput
    {
        private readonly BufferedStream myStream;
        private readonly byte[] myData;
        private int size;

        public byte this[int offset] => myData[offset];

        public ROM(Stream myROM, int maximumSize)
        {
            myStream = new BufferedStream(myROM);
            myData = new byte[maximumSize];
            size = myStream.Read(myData, 0, maximumSize);
            myStream.Position = 0;
        }

        public void Commit()
        {
            myStream.Write(myData, 0, size);
            myStream.Flush();
        }

        public void WriteTo(int position, byte[] data)
        {
            Array.Copy(data, 0, myData, position, data.Length);
            if (data.Length + position > size)
                size = data.Length + position;
        }

        public void Close()
        {
            myStream.Close();
        }
    }
}

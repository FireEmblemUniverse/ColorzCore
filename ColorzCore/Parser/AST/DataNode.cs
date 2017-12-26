using ColorzCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class DataNode : ILineNode
    {
        private int offset;
        private byte[] data;

        public DataNode(int offset, byte[] data)
        {
            this.offset = offset;
            this.data = data;
        }

        public int Size => data.Length;

        public string PrettyPrint(int indentation)
        {
            return String.Format("Raw Data Block of Length {0}", Size);
        }
        
        public void WriteData(ROM rom)
        {
            rom.WriteTo(offset, data);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Parser.AST
{
    class DataNode : ILineNode
    {
        private byte[] data;

        public DataNode(byte[] data)
        {
            this.data = data;
        }

        public int Size => data.Length;
    }
}

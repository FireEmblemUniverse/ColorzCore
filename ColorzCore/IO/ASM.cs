using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.Parser;

namespace ColorzCore.IO
{
    class ASM : IOutput
    {
        private StreamWriter asmStream, ldsStream;

        public ASM(StreamWriter asmStream, StreamWriter ldsStream)
        {
            this.asmStream = asmStream;
            this.ldsStream = ldsStream;
        }

        private void WriteToASM(int position, byte[] data)
        {
            string sectionName = $".ea_{EAParser.ConvertToAddress(position):x}";
            asmStream.WriteLine($".section {sectionName},\"ax\",%progbits");
            asmStream.WriteLine($".global {sectionName}");
            asmStream.WriteLine($"{sectionName}:");
            asmStream.Write("\t.byte ");
            foreach (byte value in data)
                asmStream.Write($"0x{value:x}, ");
            asmStream.WriteLine();
        }

        private void WriteToLDS(int position)
        {
            string sectionName = $".ea_{EAParser.ConvertToAddress(position):x}";
            ldsStream.WriteLine($". = 0x{EAParser.ConvertToAddress(position):x};");
            ldsStream.WriteLine($"{sectionName} : {{*.o({sectionName})}}");
        }

        public void WriteTo(int position, byte[] data)
        {
            WriteToASM(position, data);
            WriteToLDS(position);
        }

        public void Commit()
        {
            asmStream.Flush();
            ldsStream.Flush();
        }

        public void Close()
        {
            asmStream.Close();
            ldsStream.Close();
        }
    }
}

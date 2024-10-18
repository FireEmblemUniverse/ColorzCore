using System;
using System.Collections.Generic;
using System.IO;

namespace ColorzCore.IO
{
    public class TblEncoding
    {
        // https://datacrystal.romhacking.net/wiki/Text_Table

        private class EncodingTableNode
        {
            public byte[]? encoding;
            public IDictionary<char, EncodingTableNode> nextTable = new Dictionary<char, EncodingTableNode>();
        }

        private readonly IDictionary<char, EncodingTableNode> rootTable;

        private TblEncoding()
        {
            rootTable = new Dictionary<char, EncodingTableNode>();
        }

        public byte[] ConvertToBytes(string inputString)
        {
            return ConvertToBytes(inputString, 0, inputString.Length);
        }

        public byte[] ConvertToBytes(string inputString, int start, int length)
        {
            using MemoryStream memoryStream = new MemoryStream();
            using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            int offset = start;
            int end = start + length;

            while (offset < end)
            {
                // find longest match by following tree branches and keeping track of last encodable match

                byte[]? currentMatch = null;
                int currentMatchLength = 0;

                IDictionary<char, EncodingTableNode> table = rootTable;

                for (int j = 0; offset + j < end; j++)
                {
                    if (!table.TryGetValue(inputString[offset + j], out EncodingTableNode? node))
                    {
                        break;
                    }

                    table = node.nextTable;

                    if (node.encoding != null)
                    {
                        currentMatch = node.encoding;
                        currentMatchLength = j + 1;
                    }
                }

                if (currentMatch == null)
                {
                    // TODO: better exception?
                    throw new Exception($"Could not encode character: '{inputString[offset]}'");
                }
                else
                {
                    binaryWriter.Write(currentMatch);
                    offset += currentMatchLength;
                }
            }

            return memoryStream.ToArray();
        }

        public void MapSequence(string sequence, byte[] bytes)
        {
            if (!rootTable.TryGetValue(sequence[0], out EncodingTableNode? mapping))
            {
                mapping = rootTable[sequence[0]] = new EncodingTableNode();
            }

            // Find node corresponding to character sequence
            for (int i = 1; i < sequence.Length; i++)
            {
                IDictionary<char, EncodingTableNode> thisTable = mapping.nextTable;

                if (!thisTable.TryGetValue(sequence[i], out mapping))
                {
                    mapping = thisTable[sequence[i]] = new EncodingTableNode();
                }
            }

            mapping.encoding = bytes;
        }

        // TODO: this could be elsewhere
        private static byte[] HexToBytes(string hex, int offset, int length)
        {
            byte[] result = new byte[length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                // this could be optimized...
                result[i] = Convert.ToByte(hex.Substring(offset + i * 2, 2), 16);
            }

            return result;
        }

        public static TblEncoding FromTextReader(TextReader reader)
        {
            TblEncoding result = new TblEncoding();
            string? line;

            char[] equalForSplit = new char[] { '=' };

            while ((line = reader.ReadLine()) != null)
            {
                string[] parts;

                // this is never null but might be white space
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                line = line.TrimStart(null);

                switch (line[0])
                {
                    case '*':
                        result.MapSequence("\n", HexToBytes(line, 1, line.Length - 1));
                        break;

                    case '/':
                        // What is this?
                        // not very well documented
                        break;

                    default:
                        parts = line.Split(equalForSplit, 2);
                        result.MapSequence(parts[1], HexToBytes(parts[0], 0, parts[0].Length));
                        break;
                }
            }

            return result;
        }
    }
}

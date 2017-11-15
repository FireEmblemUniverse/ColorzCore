using ColorzCore.DataTypes;
using ColorzCore.Parser.AST;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Raws
{
    class Raw
    {
        public string Name { get; }
        public int Length { get; }
        public short Code { get; }
        public int OffsetMod { get; } //TODO: Repeatable, terminating list
        public HashSet<string> Game { get; }
        private IList<IRawParam> myParams;
        private IList<Tuple<int, int, int>> fixedParams; //position, length, value

        /***
         * Flags: 
         priority
          Affects where disassembly uses the code. Existing priorities are:
          main, low, pointer, unit, moveManual, shopList, ballista, ASM,
          battleData, reinforcementData and unknown.

         repeatable
          Means that the last parameter can be repeated and for every 
          repetition a new code is made. Currently requires code to have
          only one parameter.

         unsafe
          EA normally checks for things like parameter collisions and
          other index errors. With this flag, you can bypass them.
          Do not use unless you know what you are doing.

         end
          Means that the code ends disassembly of particular branch in 
          chapter-wide disassembly or in disassembly to end. 

         indexMode
          Affect how many bits lengths and positions mean. 8 means lengths 
          and positions are in bytes. Default is 1.

         terminatingList
          Means that the code is a variable length array of parameters which
          ends in specified value. Requires for code to have only one parameter.

         offsetMod
          The modulus in which the beginning offset of the code has to be 0.
          Default is 4. 

         noAssembly
          Forbids code from participating in assembly.

         noDisassembly
          Forbids code from participating in disassembly.
  */
        
        public Raw(string name, int length, short code, int offsetMod, HashSet<string> game, IList<IRawParam> varParams, IList<Tuple<int, int, int>> fixedParams)
        {
            Name = name;
            Length = length;
            Code = code;
            Game = game;
            OffsetMod = offsetMod;
            myParams = varParams;
            this.fixedParams = fixedParams;
        }
        
        public static Raw CopyWithNewName(Raw baseRaw, string newName)
        {
            return new Raw(newName, baseRaw.Length, baseRaw.Code, baseRaw.OffsetMod, baseRaw.Game, baseRaw.myParams, baseRaw.fixedParams);
        }
        
        public static Raw ParseRaw(StreamReader r) {
            //Since the writer of the raws is expected to know what they're doing, I'm going to be a lot more lax with error messages and graceful failure.
            string rawLine;
            do
            {
                rawLine = r.ReadLine();
            } while(rawLine.Length == 0 || rawLine[0] == '#');
            string[] parts = rawLine.Split(',');
            string name = parts[0].Trim();
            string code = parts[1].Trim();
            string length = parts[2].Trim();
            string flags = parts.Length == 4 ? parts[3].Trim() : "";
            Dictionary<string, Flag> flagDict = ParseFlags(flags);
            int indexMode = flagDict.ContainsKey("indexMode") ? Int32.Parse(flagDict["indexMode"].Values.GetLeft[0]) : 1;
            int lengthVal = indexMode * Int32.Parse(name);
            IList<IRawParam> parameters = new List<IRawParam>();
            IList<Tuple<int, int, int>> fixedParams = new List<Tuple<int, int, int>>();
            while((rawLine = r.ReadLine()).Length > 0)
            {
                if (rawLine[0] == '#')
                    continue;
                Either<IRawParam, Tuple<int, int, int>> possiblyParam = ParseParam(rawLine, indexMode);
                if (possiblyParam.IsLeft)
                    parameters.Add(possiblyParam.GetLeft);
                else
                    fixedParams.Add(possiblyParam.GetRight);
            }
            if(!flagDict.ContainsKey("unsafe"))
            {
                //TODO: Check for parameter offset collisions
            }
            HashSet<string> game = flagDict.ContainsKey("game") ? new HashSet<string>(flagDict["game"].Values.GetLeft) : new HashSet<string>();
            int offsetMod = flagDict.ContainsKey("offsetMod") ? Int32.Parse(flagDict["offsetMod"].Values.GetLeft[0]) : 4;
            return new Raw(name, lengthVal, Int16.Parse(code), offsetMod, game, parameters, fixedParams);
        } 

        public static Either<IRawParam, Tuple<int, int, int>> ParseParam(string paramLine, int indexMode)
        {
            if (!Char.IsWhiteSpace(paramLine[0]))
                throw new Exception("Raw param does not start with whitespace.");
            string[] parts = paramLine.Trim().Split(',');
            string name = parts[0];
            string position = parts[1];
            string length = parts[2];
            string flags = parts.Length == 4 ? parts[3].Trim() : "";
            Dictionary<string, Flag> flagDict = ParseFlags(flags);
            int positionBits = Int32.Parse(position) * indexMode;
            int lengthBits = Int32.Parse(length);

            if(flagDict.ContainsKey("fixed"))
            {
                return new Right<IRawParam, Tuple<int, int, int>>(new Tuple<int, int, int>(positionBits, lengthBits, Int32.Parse(name)));
            }
            if(flagDict.ContainsKey("coordinate") || flagDict.ContainsKey("coordinates"))
            {
                Either<IList<string>, Tuple<int, int>> coordNum = (flagDict.ContainsKey("coordinate") ? flagDict["coordinate"] : flagDict["coordinates"]).Values;
                int nCoords = coordNum.IsLeft ? coordNum.GetLeft.Max((string s) => Int32.Parse(s)) : Math.Max(coordNum.GetRight.Item1, coordNum.GetRight.Item2);
                return new Left<IRawParam, Tuple<int, int, int>>(new ListParam(name, positionBits, lengthBits, nCoords));
            }
            bool pointer = flagDict.ContainsKey("pointer");
            return new Left<IRawParam, Tuple<int, int, int>>(new AtomicParam(name, positionBits, lengthBits, pointer));
        }

        private static Dictionary<string, Flag> ParseFlags(string flags)
        {
            Dictionary<string, Flag> temp = new Dictionary<string, Flag>();
            string[] parts = flags.Split(' ');
            foreach(string flag in parts)
            {
                if (flag[0] != '-')
                    throw new Exception("Flag does not start with '-'");
                string withoutDash = flag.Substring(1);
                if(withoutDash.Contains(':'))
                {
                    string[] parts2 = withoutDash.Split(':');
                    string flagName = parts2[0];
                    if(parts2.Length == 2 && withoutDash.Contains('-'))
                    {
                        string[] parts3 = parts2[1].Split('-');
                        temp[flagName] = new Flag(Int32.Parse(parts3[0]), Int32.Parse(parts3[1]));
                    }
                    else
                    {
                        temp[flagName] = new Flag(new List<string>(parts2.Skip(1)));
                    }
                }
                else
                {
                    temp[withoutDash] = new Flag();
                }
            }
            return temp;
        }

        public bool Fits(IList<IParamNode> parameters)
        {
            if (parameters.Count == myParams.Count)
            {
                for (int i = 0; i < parameters.Count; i++)
                    if (!myParams[i].Fits(parameters[i]))
                        return false;
                return true;
            }
            else
                return false;
        }
        
        /* Precondition: params fits the shape of this raw's params. */
        public byte[] GetBytes(IList<IParamNode> parameters)
        {
            //Represent a code's bytes as a list/array of its length.
            BitArray data = new BitArray(Length);
            if (Code != 0)
            {
                int temp = Code;
                for(int i = 0; i < 0x10; i++, temp >>= 1)
                {
                    data[i] = (temp & 1) == 1;
                }
            }
            for (int i=0; i<myParams.Count; i++)
            {
                myParams[i].Set(data, parameters[i]);
            }
            foreach(Tuple<int, int, int> fp in fixedParams)
            {
                int val = fp.Item3;
                for(int i = fp.Item1; i<fp.Item1+fp.Item2; i++, val >>= 1)
                {
                    data[i] = (val & 1) == 1;
                }
            }
            byte[] myBytes = new byte[(Length + 7) / 8];
            data.CopyTo(myBytes, 0);
            return myBytes;
        }
    }
}

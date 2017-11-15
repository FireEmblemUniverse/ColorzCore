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
        private int length { get; }
        public short Code { get; }
        public int OffsetMod { get; }
        public HashSet<string> Game { get; }
        private IList<IRawParam> myParams;
        private IList<Tuple<int, int, int>> fixedParams; //position, length, value
        private Maybe<int> terminatingList;
        private bool repeatable;

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
        
        public Raw(string name, int length, short code, int offsetMod, HashSet<string> game, IList<IRawParam> varParams, 
            IList<Tuple<int, int, int>> fixedParams, Maybe<int> terminatingList, bool repeatable)
        {
            Name = name;
            this.length = length;
            Code = code;
            Game = game;
            OffsetMod = offsetMod;
            myParams = varParams;
            this.fixedParams = fixedParams;
            this.terminatingList = terminatingList;
            this.repeatable = repeatable;
        }
        
        public static Raw CopyWithNewName(Raw baseRaw, string newName)
        {
            return new Raw(newName, baseRaw.length, baseRaw.Code, baseRaw.OffsetMod, baseRaw.Game, baseRaw.myParams, 
                baseRaw.fixedParams, baseRaw.terminatingList, baseRaw.repeatable);
        }
        
        public static IList<Raw> ParseAllRaws(FileStream fs)
        {
            StreamReader r = new StreamReader(fs);
            IList<Raw> myRaws = new List<Raw>();
            try
            {
                while (!r.EndOfStream)
                {
                    myRaws.Add(ParseRaw(r));
                }
            }
            catch (EndOfStreamException) { }
            catch (Exception e) { throw e; }
            return myRaws;
        }

        public static Raw ParseRaw(StreamReader r) {
            //Since the writer of the raws is expected to know what they're doing, I'm going to be a lot more lax with error messages and graceful failure.
            string rawLine;
            do
            {
                rawLine = r.ReadLine();
                if (rawLine != null && (rawLine.Trim().Length == 0 || rawLine[0] == '#'))
                    continue;
                else
                    break;
            } while(rawLine != null);
            if (rawLine == null)
                throw new EndOfStreamException();
            if (Char.IsWhiteSpace(rawLine[0]))
                throw new Exception("Raw not at start of line.");
            string[] parts = rawLine.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string name = parts[0].Trim();
            string code = parts[1].Trim();
            string length = parts[2].Trim();
            string flags = parts.Length == 4 ? parts[3].Trim() : "";
            Dictionary<string, Flag> flagDict = ParseFlags(flags);
            int indexMode = flagDict.ContainsKey("indexMode") ? flagDict["indexMode"].Values.GetLeft[0].ToInt() : 1;
            int lengthVal = indexMode * length.ToInt();
            IList<IRawParam> parameters = new List<IRawParam>();
            IList<Tuple<int, int, int>> fixedParams = new List<Tuple<int, int, int>>();
            while(!r.EndOfStream)
            {
                int nextChar = r.Peek();
                if (nextChar == '#')
                {
                    r.ReadLine();
                    continue;
                }
                if (Char.IsWhiteSpace((char)nextChar) && (rawLine = r.ReadLine()).Trim().Length > 0)
                {
                    Either<IRawParam, Tuple<int, int, int>> possiblyParam = ParseParam(rawLine, indexMode);
                    if (possiblyParam.IsLeft)
                        parameters.Add(possiblyParam.GetLeft);
                    else
                        fixedParams.Add(possiblyParam.GetRight);
                }
                else
                    break;
            }
            if(!flagDict.ContainsKey("unsafe"))
            {
                //TODO: Check for parameter offset collisions
            }
            HashSet<string> game = flagDict.ContainsKey("game") ? new HashSet<string>(flagDict["game"].Values.GetLeft) : 
                flagDict.ContainsKey("language") ? new HashSet<string>(flagDict["language"].Values.GetLeft) : new HashSet<string>();
            int offsetMod = flagDict.ContainsKey("offsetMod") ? flagDict["offsetMod"].Values.GetLeft[0].ToInt() : 4;
            Maybe<int> terminatingList = flagDict.ContainsKey("terminatingList") ? (Maybe<int>)new Just<int>(flagDict["offsetMod"].Values.GetLeft[0].ToInt()) : (Maybe<int>)new Nothing<int>();
            if(!terminatingList.IsNothing && code.ToInt() != 0)
            {
                throw new Exception("TerminatingList with code nonzero.");
            }
            bool repeatable = flagDict.ContainsKey("repeatable");
            if((repeatable || !terminatingList.IsNothing) && (parameters.Count > 1) && fixedParams.Count > 0)
            {
                throw new Exception("Repeatable or terminatingList code with multiple parameters or fixed parameters.");
            }
            return new Raw(name, lengthVal, (short)(code.ToInt()), offsetMod, game, parameters, fixedParams, terminatingList, repeatable);
        } 

        public static Either<IRawParam, Tuple<int, int, int>> ParseParam(string paramLine, int indexMode)
        {
            if (!Char.IsWhiteSpace(paramLine[0]))
                throw new Exception("Raw param does not start with whitespace.");
            string[] parts = paramLine.Trim().Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string name = parts[0];
            string position = parts[1];
            string length = parts[2];
            string flags = parts.Length == 4 ? parts[3].Trim() : "";
            Dictionary<string, Flag> flagDict = ParseFlags(flags);
            int positionBits = position.ToInt() * indexMode;
            int lengthBits = length.ToInt() * indexMode;

            if(flagDict.ContainsKey("fixed"))
            {
                return new Right<IRawParam, Tuple<int, int, int>>(new Tuple<int, int, int>(positionBits, lengthBits, name.ToInt()));
            }
            if(flagDict.ContainsKey("coordinate") || flagDict.ContainsKey("coordinates"))
            {
                Either<IList<string>, Tuple<int, int>> coordNum = (flagDict.ContainsKey("coordinate") ? flagDict["coordinate"] : flagDict["coordinates"]).Values;
                int nCoords = coordNum.IsLeft ? coordNum.GetLeft.Max((string s) => s.ToInt()) : Math.Max(coordNum.GetRight.Item1, coordNum.GetRight.Item2);
                return new Left<IRawParam, Tuple<int, int, int>>(new ListParam(name, positionBits, lengthBits, nCoords));
            }
            bool pointer = flagDict.ContainsKey("pointer");
            return new Left<IRawParam, Tuple<int, int, int>>(new AtomicParam(name, positionBits, lengthBits, pointer));
        }

        private static Dictionary<string, Flag> ParseFlags(string flags)
        {
            Dictionary<string, Flag> temp = new Dictionary<string, Flag>();
            if (flags.Length > 0)
            {
                string[] parts = flags.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string flag in parts)
                {
                    if (flag[0] != '-')
                        throw new Exception("Flag does not start with '-'");
                    string withoutDash = flag.Substring(1);
                    if (withoutDash.Contains(':'))
                    {
                        string[] parts2 = withoutDash.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        string flagName = parts2[0];
                        if (parts2.Length == 2 && withoutDash.Contains('-'))
                        {
                            string[] parts3 = parts2[1].Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                            temp[flagName] = new Flag(parts3[0].ToInt(), parts3[1].ToInt());
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
            }
            return temp;
        }

        public int LengthBits(int paramCount)
        {
            if (repeatable)
            {
                return length * paramCount;
            }
            else if (!terminatingList.IsNothing)
            {
                return myParams[0].Length * (paramCount + 1);
            }
            else
            {
                return length;
            }

        }
        public int LengthBytes(int paramCount)
        {
            return (LengthBits(paramCount) + 7) / 8;
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
            else if(repeatable || !terminatingList.IsNothing)
            {
                foreach (IParamNode p in parameters)
                    if (!myParams[0].Fits(p))
                        return false;
                return true;
            }
            return false;
        }
        
        /* Precondition: params fits the shape of this raw's params. */
        public byte[] GetBytes(IList<IParamNode> parameters)
        {
            BitArray data = new BitArray(0);
            //Represent a code's bytes as a list/array of its length.
            if (!repeatable && terminatingList.IsNothing)
            {
                data.Length = length;
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
            }
            else if(repeatable)
            {
                foreach(IParamNode p in parameters)
                {
                    BitArray localData = new BitArray(length);
                    if (Code != 0)
                    {
                        int temp = Code;
                        for (int i = 0; i < 0x10; i++, temp >>= 1)
                        {
                            data[i] = (temp & 1) == 1;
                        }
                    }
                    myParams[0].Set(localData, p);
                    data.Append(localData);
                }
            }
            else
            {
                //Is a terminatingList.
                int terminator = terminatingList.FromJust;
                for (int i=0; i<parameters.Count; i++)
                {
                    BitArray localData = new BitArray(myParams[0].Length);
                    myParams[0].Set(data, parameters[i]);
                    data.Append(localData);
                }
                BitArray term = new BitArray(myParams[0].Length);
                ((AtomicParam)myParams[0]).Set(term, terminator);
                data.Append(term);
            }
            byte[] myBytes = new byte[(data.Length + 7) / 8];
            data.CopyTo(myBytes, 0);
            return myBytes;
        }
        
    }
}

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
        private int length;
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
                            localData[i] = (temp & 1) == 1;
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
                    myParams[0].Set(localData, parameters[i]);
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

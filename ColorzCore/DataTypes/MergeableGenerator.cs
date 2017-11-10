using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    public class MergeableGenerator<T>
    {
        private Stack<IEnumerator<T>> myEnums;
        public bool EOS { get; private set; }
        public MergeableGenerator(IEnumerable<T> baseEnum)
        {
            myEnums = new Stack<IEnumerator<T>>();
            myEnums.Push(baseEnum.GetEnumerator());
            Prime();
        }

        public T Current { get { return myEnums.Peek().Current; } }
        public bool MoveNext()
        {
            if(!myEnums.Peek().MoveNext())
            {
                if(myEnums.Count > 1)
                {
                    myEnums.Pop();
                    return MoveNext();
                }
                else
                {
                    EOS = true;
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        public void PrependEnumerator(IEnumerator<T> nextEnum)
        {
            myEnums.Push(nextEnum);
            Prime();
            EOS = false;
        }
        private void Prime()
        {
            myEnums.Peek().MoveNext();
        }
    }
}

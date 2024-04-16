using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    public class ImmutableStack<T> : IEnumerable<T>
    {
        private readonly Tuple<T, ImmutableStack<T>>? member;
        private int? count;

        public ImmutableStack(T elem, ImmutableStack<T> tail)
        {
            member = new Tuple<T, ImmutableStack<T>>(elem, tail);
        }

        private ImmutableStack()
        {
            member = null;
            count = 0;
        }

        public static ImmutableStack<T> Nil { get; } = new ImmutableStack<T>();

        public bool IsEmpty => member == null;
        public T Head => member!.Item1;
        public ImmutableStack<T> Tail => member!.Item2;
        public int Count => count ?? (count = Tail.Count + 1).Value;

        public IEnumerator<T> GetEnumerator()
        {
            ImmutableStack<T> temp = this;

            while (!temp.IsEmpty)
            {
                yield return temp.Head;
                temp = temp.Tail;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ImmutableStack<T> temp = this;

            while (!temp.IsEmpty)
            {
                yield return temp.Head;
                temp = temp.Tail;
            }
        }

        public static ImmutableStack<T> FromEnumerable(IEnumerable<T> content)
        {
            return content.Reverse().Aggregate(Nil, (acc, elem) => new ImmutableStack<T>(elem, acc));
        }
    }
}

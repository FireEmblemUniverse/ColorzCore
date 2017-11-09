using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    class Empty { }
    class ImmutableStack<T> : Either<Tuple<T, ImmutableStack<T>>, Empty>
    {
        public ImmutableStack(T elem, ImmutableStack<T> tail) : base(Tuple.Create(elem, tail))
        { }
        private ImmutableStack() : base(new Empty()) { }

        private static ImmutableStack<T> emptyList = new ImmutableStack<T>();
        public static ImmutableStack<T> Nil { get { return emptyList;} }

        public bool IsEmpty { get { return IsRight; } }
        public T Head { get { return GetLeft.Item1; } }
        public ImmutableStack<T> Tail { get { return GetLeft.Item2; } }
    }
}

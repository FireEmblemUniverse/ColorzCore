using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    public delegate R UnaryFunction<T,R>(T val);
    public delegate Maybe<R> MaybeAction<T, R>(T val);

#pragma warning disable IDE1006 // Naming Styles
    public interface Maybe<T>
#pragma warning restore IDE1006 // Naming Styles
    {
        bool IsNothing { get; }
        T FromJust { get; }
        Maybe<R> Fmap<R>(UnaryFunction<T, R> f);
        Maybe<R> Bind<R>(MaybeAction<T, R> f);
    }
    public class Just<T> : Maybe<T>
    {
        public bool IsNothing { get { return false; } }
        public T FromJust { get; }

        public Just(T val)
        {
            FromJust = val;
        }

        public Maybe<R> Fmap<R>(UnaryFunction<T, R> f)
        {
            return new Just<R>(f(FromJust));
        }
        public Maybe<R> Bind<R>(MaybeAction<T, R> f)
        {
            return f(FromJust);
        }
    }
    public class Nothing<T> : Maybe<T>
    {
        public bool IsNothing { get { return true; } }
        public T FromJust { get { throw new MaybeException(); } }

        public Nothing() { }

        public Maybe<R> Fmap<R>(UnaryFunction<T, R> f)
        {
            return new Nothing<R>();
        }
        public Maybe<R> Bind<R>(MaybeAction<T, R> f)
        {
            return new Nothing<R>();
        }
    }

    public class MaybeException : Exception { }
}

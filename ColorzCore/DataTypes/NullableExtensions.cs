using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.DataTypes
{
    public static class NullableExtensions
    {
        public static R? Fmap<T, R>(this T? self, Func<T, R> f)
            where T : class
        {
            return self != null ? f(self) : default;
        }

        public static R IfJust<T, R>(this T? self, Func<T, R> just, Func<R> nothing)
            where T : class
        {
            if (self != null)
            {
                return just(self);
            }
            else
            {
                return nothing();
            }
        }

        public static R IfJust<T, R>(this T? self, Func<T, R> just, Func<R> nothing)
            where T : struct
        {
            if (self.HasValue)
            {
                return just(self.Value);
            }
            else
            {
                return nothing();
            }
        }

        public static void IfJust<T>(this T? self, Action<T> just, Action? nothing = null)
            where T : class
        {
            if (self != null)
            {
                just(self);
            }
            else
            {
                nothing?.Invoke();
            }
        }

        public static void IfJust<T>(this T? self, Action<T> just, Action? nothing = null)
            where T : struct
        {
            if (self.HasValue)
            {
                just(self.Value);
            }
            else
            {
                nothing?.Invoke();
            }
        }
    }
}

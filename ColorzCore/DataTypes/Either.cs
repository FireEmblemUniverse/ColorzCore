using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore
{
    public class Either<Left,Right>
    {
        private bool isLeft;
        private readonly Left l;
        private readonly Right r;
        public Either(Left l)
        {
            this.l = l;
            this.r = default(Right);
            isLeft = true;
        }
        public Either(Right r)
        {
            this.l = default(Left);
            this.r = r;
            isLeft = false;
        }

        public bool IsLeft { get { return isLeft; } }
        public bool IsRight { get { return !isLeft; } }
        public Left GetLeft { get {
                if (IsLeft)
                    return l;
                else
                    throw new WrongEitherException();
            }
        }
        public Right GetRight
        {
            get
            {
                if (IsRight)
                    return r;
                else
                    throw new WrongEitherException();
            }
        }
    }
    class WrongEitherException : Exception { }
}

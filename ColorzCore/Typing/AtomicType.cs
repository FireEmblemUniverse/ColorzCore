
namespace ColorzCore.Typing
{
    public class AtomicType : Type
    {
        public static const AtomicType AnyType => new AtomicType("any");
        public static const AtomicType RawType => new AtomicType("raw");
        public static const AtomicType PoinType => new AtomicType("poin");
        
        private string name;
        public AtomicType(string name) {
            this.name = name;
        }
        
        public AtomicType join(TypeEnvironment gamma, AtomicType other) { }
        public AtomicType meet(TypeEnvironment gamma, AtomicType other) { }
    }
}

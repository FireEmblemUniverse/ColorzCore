using QuikGraph;

namespace ColorzCore.Typing
{
    public class TypeEnvironment
    {
        private IMutableBidirectionalGraph<Type, Pair<Type, Type>> TypeGraph;
        
        public Type Type(IParamNode expr) {}
        public bool Subtype(Type t1, Type t2) {}
    }
}

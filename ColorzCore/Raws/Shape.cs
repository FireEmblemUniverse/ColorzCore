using ColorzCore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Raws
{
    class Shape
    {
        private interface IParamShape
        {
            int MinCoords { get; }
            int MaxCoords { get; }
            ParamType Type { get; }
            bool Fits(IParamShape target);
        }
        private class AtomShape : IParamShape
        {
            public int MinCoords => 1;
            public int MaxCoords => 1;
            public ParamType Type => ParamType.ATOM;
            public bool Fits(IParamShape target) { return target.Type == Type; }
        }
        private class ListShape : IParamShape
        {
            public int MinCoords { get; }
            public int MaxCoords { get; }
            public ParamType Type => ParamType.LIST;
            public bool Fits(IParamShape target)
            {
                return
            }
        }


        public bool Fits(Shape targetShape)
        {
            throw new NotImplementedException();
        }

        static Shape ComputeShape(IList<IParamNode> ns)
        {
            throw new NotImplementedException();
        }
    }
}

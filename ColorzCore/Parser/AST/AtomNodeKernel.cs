﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorzCore.DataTypes;
using ColorzCore.Lexer;

namespace ColorzCore.Parser.AST
{
    public abstract class AtomNodeKernel : IAtomNode
    {
        public abstract int Precedence { get; }

        public abstract int ToInt();

        public ParamType Type { get { return ParamType.ATOM; } }

        public override string ToString()
        {
            return "0x"+ToInt().ToString("X");
        }

        public virtual Maybe<string> GetIdentifier()
        {
            return new Nothing<string>();
        }

        public virtual string PrettyPrint()
        {
            return ToString(); //TODO: Mark abstract
        }
        public abstract IEnumerable<Token> ToTokens();
        public abstract Location MyLocation { get; }

        public Either<int, string> TryEvaluate()
        {
            try
            {
                int res = this.ToInt();
                return new Left<int, string>(res);
            }
            catch (IdentifierNode.UndefinedIdentifierException e)
            {
                return new Right<int, string>("Unrecognized identifier: " + e.CausedError.Content);
            }
            catch (DivideByZeroException)
            {
                return new Right<int, string>("Division by zero.");
            }
        }
        public abstract bool CanEvaluate();

        public abstract Maybe<int> Evaluate(ICollection<Token> undefinedIdentifiers);
        
        Maybe<IParamNode> IParamNode.Evaluate(ICollection<Token> undefinedIdentifiers)
        {
            return Evaluate(undefinedIdentifiers).Fmap((int a) => (IParamNode)new NumberNode(MyLocation, a));
        }

        public abstract IAtomNode Simplify();
    }
}

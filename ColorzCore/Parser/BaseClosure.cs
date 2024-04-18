namespace ColorzCore.Parser
{
    class BaseClosure : Closure
    {
        private EAParser enclosing;
        public BaseClosure(EAParser enclosing)
        {
            this.enclosing = enclosing;
        }
        public override bool HasLocalSymbol(string label)
        {
            return label.ToUpper() == "CURRENTOFFSET" || base.HasLocalSymbol(label);
        }
    }
}

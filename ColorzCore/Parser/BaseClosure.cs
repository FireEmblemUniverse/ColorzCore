namespace ColorzCore.Parser
{
    class BaseClosure : Closure
    {
        private EAParser enclosing;
        public BaseClosure(EAParser enclosing)
        {
            this.enclosing = enclosing;
        }
        public override bool HasLocalSymbolValue(string label)
        {
            return label.ToUpper() == "CURRENTOFFSET" || base.HasLocalSymbolValue(label);
        }
    }
}

namespace ColorzCore.Interpreter
{
    class BaseClosure : Closure
    {
        public override bool HasLocalSymbol(string label)
        {
            return label.ToUpperInvariant() switch
            {
                "CURRENTOFFSET" or "__LINE__" or "__FILE__" => true,
                _ => base.HasLocalSymbol(label),
            };
        }
    }
}

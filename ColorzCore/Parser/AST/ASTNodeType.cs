namespace ColorzCore.Parser.AST
{
    public enum ASTNodeType
    {
        EAPROGRAM, 
        LINE,
        BLOCK,
        PREPROCESSOR_COMMAND,
        LABEL,
        STATEMENTS,
        PARAM_LIST,
        ATOM,
        LIST,
        STRING,
        RAW,
        MATH_EXPR,
        EOS,
        ERROR
    }
}
 
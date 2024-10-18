using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorzCore.Lexer
{
    public enum TokenType
    {
        NEWLINE,
        SEMICOLON,
        COLON,
        PREPROCESSOR_DIRECTIVE,
        OPEN_BRACE,
        CLOSE_BRACE,
        OPEN_PAREN,
        CLOSE_PAREN,
        COMMA,
        MUL_OP,
        DIV_OP,
        MOD_OP,
        ADD_OP,
        SUB_OP,
        LSHIFT_OP,
        RSHIFT_OP,
        SIGNED_RSHIFT_OP,
        UNDEFINED_COALESCE_OP,
        NOT_OP,
        AND_OP,
        XOR_OP,
        OR_OP,
        LOGNOT_OP,
        LOGAND_OP,
        LOGOR_OP,
        COMPARE_EQ,
        COMPARE_NE,
        COMPARE_LT,
        COMPARE_LE,
        COMPARE_GT,
        COMPARE_GE,
        ASSIGN,
        NUMBER,
        OPEN_BRACKET,
        CLOSE_BRACKET,
        STRING,
        IDENTIFIER,
        MAYBE_MACRO,
        ERROR //Catch-all for invalid characters.
    }
}

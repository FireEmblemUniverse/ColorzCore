from ea_test import EATest as T


TESTS = [
    # =====================
    # = #define Directive =
    # =====================

    # '#define' traditional nominal behavior
    T("Define Basic object-like",
        '#define Value 0xFA \n ORG 0 ; BYTE Value',
        b"\xFA"),

    T("Define Basic function-like",
        '#define Macro(a) "0xFA + (a)" \n ORG 0 ; BYTE Macro(2)',
        b"\xFC"),

    # '#define' a second time overrides the first definition
    # NOTE: this is probably not something that we want to freeze
    # T("Define override",
    #     '#define Value 1 \n #define Value 2 \n ORG 0 ; BYTE Value',
    #     b"\x02"),

    # '#define' using a vector as argument (extra commas)
    T("Define vector argument",
        '#define Macro(a) "BYTE 1" \n ORG 0 ; Macro([1, 2, 3])',
        b"\x01"),

    # '#define ... "..."' with escaped newlines inside string
    T("Multi-line string define",
        '#define SomeLongMacro(A, B, C) "\\\n ALIGN 4 ; \\\n WORD C ; \\\n SHORT B ; \\\n BYTE A" \n'
            + 'ORG 0 ; SomeLongMacro(0xAA, 0xBB, 0xCC)',
        b"\xCC\x00\x00\x00\xBB\x00\xAA"),

    T("Define eager expansion",
        "#define Value 1 \n #define OtherValue Value \n #undef Value \n #define Value 2 \n ORG 0 ; BYTE OtherValue",
        b'\x01'),

    # '#define ...' multi-token without quotes
    T("Multi-token define 1",
        '#define Value (1 + 2) \n ORG 0 ; BYTE Value',
        b"\x03"),

    T("Multi-token define 2",
        '#define Macro(a, b) (a + b) \n ORG 0 ; BYTE Macro(1, 2)',
        b"\x03"),

    T("Multi-token define 2",
        '#define Macro(a, b) BYTE a a + b b \n ORG 0 ; Macro(1, 2)',
        b"\x01\x03\x02"),

    # Those would fail on 
    T("Define uinintuitive atomic 1",
        '#define Value 1 + 2 \n ORG 0 ; BYTE Value * 2',
        b"\x05"), # 1 * 2 * 2 = 5

    T("Define uinintuitive atomic 1",
        '#define Value "1 + 2" \n ORG 0 ; BYTE Value * 2',
        b"\x05"), # 1 * 2 * 2 = 5

    # '#define MyMacro MyMacro' (MyMacro shouldn't expand)
    T("Non-productive macros 1",
        '#define MyMacro MyMacro \n ORG 0 ; MyMacro: ; BYTE 1',
        b'\x01'),

    T("Non-productive macros 2",
        '#define MyMacro MyMacro \n ORG 0 ; BYTE IsDefined(MyMacro)',
        b'\x01'),

    T("Non-productive macros 3",
        '#define MyMacro MyMacro \n ORG 0 ; #ifdef MyMacro \n BYTE 1 \n #else \n BYTE 0 \n #endif',
        b'\x01'),

    # ====================
    # = #undef Directive =
    # ====================

    T("Undef 1",
        '#define Value 1 \n #undef Value \n ORG 0 ; BYTE Value',
        None),

    T("Undef 2",
        '#define Value 1 \n #undef Value \n #ifndef Value \n ORG 0 ; BYTE 1 \n #endif',
        b"\x01"),

    T("Undef multiple",
        '#define ValueA \n #define ValueB \n #undef ValueA ValueB \n #ifndef ValueA \n #ifndef ValueB \n'
            + 'BYTE 1 \n #endif \n #endif',
        b'\x01'),

    # ========================
    # = #if[n]def Directives =
    # ========================

    # '#ifdef'
    T("Ifdef", 'ORG 0 \n #define Value \n #ifdef Value \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x01"),

    # '#ifndef'
    T("Ifndef", 'ORG 0 \n #define Value \n #ifndef Value \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x00"),

    # TODO: #if, #include, #incbin, #incext, #inctext, #pool
]

import sys
from ea_test import EATestConfig as Config, EATest as T, run_tests

BASIC_TESTS = [
    T("Basic", "ORG 0 ; BYTE 1", b"\x01"),
    T("Addition", "ORG 0 ; BYTE 1 + 2", b"\x03"),
    T("Precedence 1", "ORG 0 ; BYTE 1 + 2 * 10", b"\x15"),

    # POIN
    T("POIN 1", "ORG 0 ; POIN 4", b"\x04\x00\x00\x08"),
    T("POIN 2", "ORG 0 ; POIN 0", b"\x00\x00\x00\x00"),
    T("POIN 3", "ORG 0 ; POIN 0x08000000", b"\x00\x00\x00\x08"),
    T("POIN 4", "ORG 0 ; POIN 0x02000000", b"\x00\x00\x00\x02"),
]


EXPRESSION_TESTS = [
    T("UNDCOERCE 1", 'A := 0 ; ORG 0 ; BYTE (A || 1) ?? 0', b"\x01"),
    T("UNDCOERCE 2", 'ORG 0 ; BYTE (A || 1) ?? 0', b"\x00"),
]


PREPROC_TESTS = [
    # '#define' traditional nominal behavior
    T("Define 1", '#define Value 0xFA \n ORG 0 ; BYTE Value', b"\xFA"),
    T("Define 2", '#define Macro(a) "0xFA + (a)" \n ORG 0 ; BYTE Macro(2)', b"\xFC"),
    T("Define 3", '#define Value \n #ifdef Value \n ORG 0 ; BYTE 1 \n #endif', b"\x01"),

    # '#define' a second time overrides the first definition
    T("Define override", '#define Value 1 \n #define Value 2 \n ORG 0 ; BYTE Value', b"\x02"),

    # '#define' using a vector as argument (extra commas)
    T("Define vector argument", '#define Macro(a) "BYTE 1" \n ORG 0 ; Macro([1, 2, 3])', b"\x01"),

    # '#define ... "..."' with escaped newlines inside string
    T("Multi-line string define", '#define SomeLongMacro(A, B, C) "\\\n ALIGN 4 ; \\\n WORD C ; \\\n SHORT B ; \\\n BYTE A" \n ORG 0 ; SomeLongMacro(0xAA, 0xBB, 0xCC)', b"\xCC\x00\x00\x00\xBB\x00\xAA"),

    # '#define ...' multi-token without quotes
    T("Multi-token define 1", '#define Value (1 + 2) \n ORG 0 ; BYTE Value', b"\x03"),
    T("Multi-token define 2", '#define Macro(a, b) (a + b) \n ORG 0 ; BYTE Macro(1, 2)', b"\x03"),
    T("Multi-token define 2", '#define Macro(a, b) BYTE a a + b b \n ORG 0 ; Macro(1, 2)', b"\x01\x03\x02"),

    # '#ifdef'
    T("Ifdef", 'ORG 0 \n #define Value \n #ifdef Value \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x01"),

    # '#ifndef'
    T("Ifndef", 'ORG 0 \n #define Value \n #ifndef Value \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x00"),

    # '#define MyMacro MyMacro' (MyMacro shouldn't expand)
    T("Non-productive macros 1", '#define MyMacro MyMacro \n ORG 0 ; MyMacro: ; BYTE 1', b'\x01'),
    T("Non-productive macros 2", '#define MyMacro MyMacro \n ORG 0 ; BYTE IsDefined(MyMacro)', b'\x01'),
    T("Non-productive macros 3", '#define MyMacro MyMacro \n ORG 0 ; #ifdef MyMacro \n BYTE 1 \n #else \n BYTE 0 \n #endif', b'\x01'),

    # T("IFDEF 2", 'ORG 0 \n #define A \n #define B \n #ifdef A B \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x01"),

    # '#undef'
    T("Undef 1", '#define Value 1 \n #undef Value \n ORG 0 ; BYTE Value', None),
    T("Undef 2", '#define Value 1 \n #undef Value \n #ifndef Value \n ORG 0 ; BYTE 1 \n #endif', b"\x01"),
]


import statements, symbols

ALL_TEST_CASES = BASIC_TESTS + statements.TESTS + symbols.TESTS + EXPRESSION_TESTS + PREPROC_TESTS

def main(args):
    import argparse

    arg_parse = argparse.ArgumentParser()

    arg_parse.add_argument("command")
    arg_parse.add_argument("--extra-params")

    args = arg_parse.parse_args(args[1:])

    command : str = args.command
    extra_params : str = args.extra_params

    test_cases = ALL_TEST_CASES

    config = Config(command, extra_params)
    run_tests(config, test_cases)


if __name__ == '__main__':
    sys.exit(main(sys.argv))

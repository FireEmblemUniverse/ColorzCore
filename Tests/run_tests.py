import sys
from ea_test import EATestConfig as Config, EATest as T, run_tests

BASIC_TESTS = [
    T("Basic", "ORG 0 ; BYTE 1", b"\x01"),
    T("Addition", "ORG 0 ; BYTE 1 + 2", b"\x03"),
    T("Precedence 1", "ORG 0 ; BYTE 1 + 2 * 10", b"\x15"),

    # POIN
    T("POIN Offset", "ORG 0 ; POIN 4", b"\x04\x00\x00\x08"),
    T("POIN NULL", "ORG 0 ; POIN 0", b"\x00\x00\x00\x00"),
    T("POIN Address", "ORG 0 ; POIN 0x08000000", b"\x00\x00\x00\x08"),
    T("POIN RAM", "ORG 0 ; POIN 0x02000000", b"\x00\x00\x00\x02"),
]


import statements, symbols, directives, expressions

ALL_TEST_CASES = BASIC_TESTS + statements.TESTS + symbols.TESTS + directives.TESTS + expressions.TESTS

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

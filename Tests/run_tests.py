import sys, os
import subprocess, tempfile


class Config:
    command : list[str]
    extra_params : list[str]

    def __init__(self, command : str, extra_params : str | None) -> None:
        self.command = command.split()
        self.extra_params = extra_params.split() if extra_params is not None else []


class Test:
    name : str
    script : str
    expected : bytes | None

    def __init__(self, name : str, script : str, expected : bytes | None) -> None:
        self.name = name
        self.script = script
        self.expected = expected

    def run_test(self, config : Config) -> bool:
        success = False

        with tempfile.NamedTemporaryFile(delete = False) as f:
            f.close()

            completed = subprocess.run(config.command + ["A", "FE6", f"-output:{f.name}"] + config.extra_params,
                text = True, input = self.script, stdout = subprocess.DEVNULL, stderr = subprocess.DEVNULL)

            if self.expected is None:
                # success on error
                success = completed.returncode != 0

            else:
                # success on resulting bytes matching
                with open(f.name, 'rb') as f2:
                    result_bytes = f2.read()

                success = result_bytes == self.expected

            os.remove(f.name)

        return success


class bcolors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'


def run_tests(config : Config, test_cases : list[Test]) -> None:
    success_count = 0
    test_count = len(test_cases)

    success_message = f"{bcolors.OKBLUE}SUCCESS{bcolors.ENDC}"
    failure_message = f"{bcolors.FAIL}FAILURE{bcolors.ENDC}"

    for i, test_case in enumerate(test_cases):
        success = test_case.run_test(config)

        message = success_message if success else failure_message
        print(f"[{i + 1}/{test_count}] {test_case.name}: {message}")

        if success:
            success_count = success_count + 1

    if success_count == test_count:
        print(f"{success_count}/{test_count} tests passed {success_message}")

    else:
        print(f"{success_count}/{test_count} tests passed {failure_message}")


BASIC_TESTS = [
    Test("Basic", "ORG 0 ; BYTE 1", b"\x01"),
    Test("Addition", "ORG 0 ; BYTE 1 + 2", b"\x03"),
    Test("Precedence 1", "ORG 0 ; BYTE 1 + 2 * 10", b"\x15"),

    # POIN
    Test("POIN 1", "ORG 0 ; POIN 4", b"\x04\x00\x00\x08"),
    Test("POIN 2", "ORG 0 ; POIN 0", b"\x00\x00\x00\x00"),
    Test("POIN 3", "ORG 0 ; POIN 0x08000000", b"\x00\x00\x00\x08"),
    Test("POIN 4", "ORG 0 ; POIN 0x02000000", b"\x00\x00\x00\x02"),

    # ORG
    Test("ORG 1", "ORG 1 ; BYTE 1 ; ORG 10 ; BYTE 10", b"\x00\x01" + b"\x00" * 8 + b"\x0A"),
    Test("ORG 2", "ORG 0x08000001 ; BYTE 1 ; ORG 0x0800000A ; BYTE 10", b"\x00\x01" + b"\x00" * 8 + b"\x0A"),
    Test("ORG 3", "ORG 0x10000000 ; BYTE 1", None),
    Test("ORG 4", "ORG -1 ; BYTE 1", None),

    # ALIGN
    Test("ALIGN 1", "ORG 1 ; ALIGN 4 ; WORD CURRENTOFFSET", b"\x00\x00\x00\x00\x04\x00\x00\x00"),
    Test("ALIGN 2", "ORG 4 ; ALIGN 4 ; WORD CURRENTOFFSET", b"\x00\x00\x00\x00\x04\x00\x00\x00"),
    Test("ALIGN 3", "ORG 1 ; ALIGN 0 ; WORD CURRENTOFFSET", None),
    Test("ALIGN 4", "ORG 1 ; ALIGN -1 ; WORD CURRENTOFFSET", None),

    # FILL
    Test("FILL 1", "ORG 0 ; FILL 0x10", b"\x00" * 0x10),
    Test("FILL 2", "ORG 4 ; FILL 0x10 0xFF", b"\x00\x00\x00\x00" + b"\xFF" * 0x10),

    # ASSERT
    Test("ASSERT 1", "ASSERT 0", b""),
    Test("ASSERT 2", "ASSERT -1", None),
    Test("ASSERT 3", "ASSERT 1 < 0", None),
    Test("ASSERT 4", "ASSERT 1 - 2", None)
]


EXPRESSION_TESTS = [
    Test("UNDCOERCE 1", 'A := 0 ; ORG 0 ; BYTE (A || 1) ?? 0', b"\x01"),
    Test("UNDCOERCE 2", 'ORG 0 ; BYTE (A || 1) ?? 0', b"\x00"),
]


PREPROC_TESTS = [
    # '#define' traditional nominal behavior
    Test("Define 1", '#define Value 0xFA \n ORG 0 ; BYTE Value', b"\xFA"),
    Test("Define 2", '#define Macro(a) "0xFA + (a)" \n ORG 0 ; BYTE Macro(2)', b"\xFC"),
    Test("Define 3", '#define Value \n #ifdef Value \n ORG 0 ; BYTE 1 \n #endif', b"\x01"),

    # '#define' a second time overrides the first definition
    Test("Define override", '#define Value 1 \n #define Value 2 \n ORG 0 ; BYTE Value', b"\x02"),

    # '#define' using a vector as argument (extra commas)
    Test("Define vector argument", '#define Macro(a) "BYTE 1" \n ORG 0 ; Macro([1, 2, 3])', b"\x01"),

    # '#define ... "..."' with escaped newlines inside string
    Test("Multi-line string define", '#define SomeLongMacro(A, B, C) "\\\n ALIGN 4 ; \\\n WORD C ; \\\n SHORT B ; \\\n BYTE A" \n ORG 0 ; SomeLongMacro(0xAA, 0xBB, 0xCC)', b"\xCC\x00\x00\x00\xBB\x00\xAA"),

    # '#define ...' multi-token without quotes
    Test("Multi-token define 1", '#define Value (1 + 2) \n ORG 0 ; BYTE Value', b"\x03"),
    Test("Multi-token define 2", '#define Macro(a, b) (a + b) \n ORG 0 ; BYTE Macro(1, 2)', b"\x03"),
    Test("Multi-token define 2", '#define Macro(a, b) BYTE a a + b b \n ORG 0 ; Macro(1, 2)', b"\x01\x03\x02"),

    # '#ifdef'
    Test("Ifdef", 'ORG 0 \n #define Value \n #ifdef Value \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x01"),

    # '#ifndef'
    Test("Ifndef", 'ORG 0 \n #define Value \n #ifndef Value \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x00"),

    # '#define MyMacro MyMacro' (MyMacro shouldn't expand)
    Test("Non-productive macros 1", '#define MyMacro MyMacro \n ORG 0 ; MyMacro: ; BYTE 1', b'\x01'),
    Test("Non-productive macros 2", '#define MyMacro MyMacro \n ORG 0 ; BYTE IsDefined(MyMacro)', b'\x01'),
    Test("Non-productive macros 3", '#define MyMacro MyMacro \n ORG 0 ; #ifdef MyMacro \n BYTE 1 \n #else \n BYTE 0 \n #endif', b'\x01'),

    # Test("IFDEF 2", 'ORG 0 \n #define A \n #define B \n #ifdef A B \n BYTE 1 \n #else \n BYTE 0 \n #endif', b"\x01"),

    # '#undef'
    Test("Undef 1", '#define Value 1 \n #undef Value \n ORG 0 ; BYTE Value', None),
    Test("Undef 2", '#define Value 1 \n #undef Value \n #ifndef Value \n ORG 0 ; BYTE 1 \n #endif', b"\x01"),
]


ALL_TEST_CASES = BASIC_TESTS + EXPRESSION_TESTS + PREPROC_TESTS

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

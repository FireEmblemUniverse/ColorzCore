import os, subprocess, tempfile


class EATestConfig:
    command : list[str]
    extra_params : list[str]

    def __init__(self, command : str, extra_params : str | None) -> None:
        self.command = command.split()
        self.extra_params = extra_params.split() if extra_params is not None else []


class EATest:
    name : str
    script : str
    expected : bytes | None

    def __init__(self, name : str, script : str, expected : bytes | None) -> None:
        self.name = name
        self.script = script
        self.expected = expected

    def run_test(self, config : EATestConfig) -> bool:
        success = False

        # TODO: this could be better than just "success/failure"
        # Failure here can be two causes: result doesn't match OR program crashed.

        with tempfile.NamedTemporaryFile(delete = False) as f:
            f.close()

            completed = subprocess.run(config.command + ["A", "FE6", f"-output:{f.name}"] + config.extra_params,
                text = True, input = self.script, stdout = subprocess.DEVNULL, stderr = subprocess.PIPE)

            if self.expected is None:
                # success on error
                success = completed.returncode != 0 and "Errors occurred; no changes written." in completed.stderr

            else:
                # success on resulting bytes matching
                with open(f.name, 'rb') as f2:
                    result_bytes = f2.read()

                success = result_bytes == self.expected

            os.remove(f.name)

        return success


HEADER = '\033[95m'
CC_OKBLUE = '\033[94m'
CC_OKCYAN = '\033[96m'
CC_OKGREEN = '\033[92m'
CC_WARNING = '\033[93m'
CC_FAIL = '\033[91m'
CC_ENDC = '\033[0m'
CC_BOLD = '\033[1m'
CC_UNDERLINE = '\033[4m'


SUCCESS_MESSAGE = f"{CC_OKBLUE}SUCCESS{CC_ENDC}"
FAILURE_MESSAGE = f"{CC_FAIL}FAILURE{CC_ENDC}"


def run_tests(config : EATestConfig, test_cases : list[EATest]) -> None:
    success_count = 0
    test_count = len(test_cases)

    for i, test_case in enumerate(test_cases):
        success = test_case.run_test(config)

        message = SUCCESS_MESSAGE if success else FAILURE_MESSAGE
        print(f"[{i + 1}/{test_count}] {test_case.name}: {message}")

        if success:
            success_count = success_count + 1

    if success_count == test_count:
        print(f"{success_count}/{test_count} tests passed {SUCCESS_MESSAGE}")

    else:
        print(f"{success_count}/{test_count} tests passed {FAILURE_MESSAGE}")


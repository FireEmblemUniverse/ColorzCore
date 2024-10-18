from ea_test import EATest as T

def byte(expr):
    return f"ORG 0 ; BYTE {expr} ;"

TESTS = [
    T("Operator '+'", byte('1 + 2'), b'\x03'),

    T("Operator '-' 1", byte('2 - 1'), b'\x01'),
    T("Operator '-' 2", byte('1 - 2'), b'\xFF'),

    T("Operator '*'", byte('3 * 2'), b'\x06'),

    T("Operator '/' 1", byte('6 / 2'), b'\x03'),
    T("Operator '/' 2", byte('5 / 2'), b'\x02'), # +2 (round towards zero)
    T("Operator '/' 3", byte('(-5) / 2'), b'\xFE'), # -2 (round towards zero)

    T("Operator '%' 1", byte('5 % 2'), b'\x01'), # +1
    T("Operator '%' 2", byte('(-5) % 2'), b'\xFF'), # -1

    T("Operator '<<'", byte('3 << 2'), b'\x0C'), # 12
    T("Operator '>>'", byte('3 >> 1'), b'\x01'), # 12
    T("Operator '>>>'", byte('0x80000000 >>> 25'), b'\xC0'), # 0b11000000

    T("Operator '<' 1", byte('1 < 2'), b'\x01'),
    T("Operator '<' 2", byte('2 < 1'), b'\x00'),
    T("Operator '<' 3", byte('2 < 2'), b'\x00'),

    T("Operator '<=' 1", byte('1 <= 2'), b'\x01'),
    T("Operator '<=' 2", byte('2 <= 1'), b'\x00'),
    T("Operator '<=' 3", byte('2 <= 2'), b'\x01'),

    T("Operator '==' 1", byte('1 == 2'), b'\x00'),
    T("Operator '==' 2", byte('2 == 1'), b'\x00'),
    T("Operator '==' 3", byte('2 == 2'), b'\x01'),

    T("Operator '!=' 1", byte('1 != 2'), b'\x01'),
    T("Operator '!=' 2", byte('2 != 1'), b'\x01'),
    T("Operator '!=' 3", byte('2 != 2'), b'\x00'),

    T("Operator '>=' 1", byte('1 >= 2'), b'\x00'),
    T("Operator '>=' 2", byte('2 >= 1'), b'\x01'),
    T("Operator '>=' 3", byte('2 >= 2'), b'\x01'),

    T("Operator '>' 1", byte('1 > 2'), b'\x00'),
    T("Operator '>' 2", byte('2 > 1'), b'\x01'),
    T("Operator '>' 3", byte('2 > 2'), b'\x00'),

    T("Operator '&' 1", byte('3 & 6'), b'\x02'),
    T("Operator '&' 2", byte('1 & 6'), b'\x00'),

    T("Operator '|' 1", byte('1 | 12'), b'\x0D'), # 0b1101
    T("Operator '|' 2", byte('1 | 1'), b'\x01'),

    T("Operator '^' 1", byte('3 ^ 6'), b'\x05'),
    T("Operator '^' 2", byte('1 ^ 6'), b'\x07'),

    T("Operator '&&' 1", byte('0 && 1'), b'\x00'),
    T("Operator '&&' 2", byte('1 && 1'), b'\x01'),
    T("Operator '&&' 3", byte('1 && 10'), b'\x0A'),

    T("Operator '||' 1", byte('0 || 1'), b'\x01'),
    T("Operator '||' 2", byte('1 || 1'), b'\x01'),
    T("Operator '||' 3", byte('1 || 10'), b'\x01'),
    T("Operator '||' 4", byte('0 || 10'), b'\x0A'),
    T("Operator '||' 5", byte('8 || 1'), b'\x08'),

    T("Operator '??' 1", f'A := 0 ;' + byte('(A || 1) ?? 0'), b"\x01"),
    T("Operator '??' 2", byte('(A || 1) ?? 0'), b"\x00"),

    T("Operator unary '-' 1", byte('-1'), b'\xFF'),
    T("Operator unary '-' 2", byte('-(1 + 2)'), b'\xFD'),

    T("Operator unary '~' 1", byte('~0'), b'\xFF'),
    T("Operator unary '~' 2", byte('~3'), b'\xFC'),
    T("Operator unary '~' 3", byte('~(-1)'), b'\x00'),

    T("Operator unary '!' 1", byte('!76'), b'\x00'),
    T("Operator unary '!' 2", byte('!0'), b'\x01'),
    T("Operator unary '!' 3", byte('!!7'), b'\x01'),

    T("Precedence 1 ('+', '*')", byte('1 + 2 * 3'), b'\x07'), # +7
    T("Precedence 2 ('-', '*')", byte('1 - 2 * 3'), b'\xFB'), # -5
    T("Precedence 3 ('+', '/')", byte('4 + 6 / 2'), b'\x07'), # +7 (not +5)
    T("Precedence 4 ('+', '%')", byte('5 + 5 % 2'), b'\x06'), # +6 (not +0)
    T("Precedence 5 ('<<', '+')", byte('2 << 1 + 5'), bytes((128,))), # not 9
    T("Precedence 6 ('>>', '+')", byte('0xFF >> 1 + 5'), b'\x03'),
    T("Precedence 7 ('>>>', '+')", byte('0x80000000 >>> 20 + 5'), b'\xC0'),
    # TODO: more
]

from ea_test import EATest as T


TESTS = [
    # =================
    # = ORG Statement =
    # =================

    # Nominal behavior
    T("ORG Basic",
        "ORG 1 ; BYTE 1 ; ORG 10 ; BYTE 10",
        b"\x00\x01" + b"\x00" * 8 + b"\x0A"),

    # Also works backwards
    T("ORG Backwards",
        "ORG 10 ; BYTE 10 ; ORG 1 ; BYTE 1",
        b"\x00\x01" + b"\x00" * 8 + b"\x0A"),

    # Addresses are Offsets
    T("ORG Addresses",
        "ORG 0x08000001 ; BYTE 1 ; ORG 0x0800000A ; BYTE 10",
        b"\x00\x01" + b"\x00" * 8 + b"\x0A"),

    # Error on offset too big
    T("ORG Overflow",
        "ORG 0x10000000 ; BYTE 1",
        None),

    # Error on offset too small/negative
    T("ORG Underflow",
        "ORG -1 ; BYTE 1",
        None),

    # =======================
    # = PUSH/POP Statements =
    # =======================

    # Nominal behavior
    T("PUSH POP Basic",
        "ORG 4 ; PUSH ; ORG 1 ; POP ; BYTE CURRENTOFFSET",
        b"\x00\x00\x00\x00\x04"),

    T("PUSH POP Override",
        "ORG 0 ; PUSH ; BYTE 0xAA ; POP ; BYTE 0xBB",
        b"\xBB"),

    T("POP Naked",
        "ORG 0 ; BYTE 0 ; POP",
        None),

    # ===================
    # = ALIGN Statement =
    # ===================

    T("ALIGN Basic",
        "ORG 1 ; ALIGN 4 ; BYTE CURRENTOFFSET",
        b"\x00\x00\x00\x00\x04"),

    T("ALIGN Aligned",
        "ORG 4 ; ALIGN 4 ; BYTE CURRENTOFFSET",
        b"\x00\x00\x00\x00\x04"),

    T("ALIGN Zero",
        "ORG 1 ; ALIGN 0 ; BYTE CURRENTOFFSET",
        None),

    T("ALIGN Negative",
        "ORG 1 ; ALIGN -1 ; BYTE CURRENTOFFSET",
        None),

    T("ALIGN Offset",
        "ORG 2 ; ALIGN 4 1 ; BYTE CURRENTOFFSET",
        b"\x00\x00\x00\x00\x00\x05"),

    T("ALIGN Offset Aligned",
        "ORG 1 ; ALIGN 4 1 ; BYTE CURRENTOFFSET",
        b"\x00\x01"),

    # ==================
    # = FILL Statement =
    # ==================

    T("FILL Basic",
        "ORG 0 ; FILL 0x10",
        b"\x00" * 0x10),

    T("FILL Value",
        "ORG 4 ; FILL 0x10 0xFF",
        b"\x00\x00\x00\x00" + b"\xFF" * 0x10),

    T("FILL Zero",
        "ORG 0 ; FILL 0",
        None),

    T("FILL Negative",
        "ORG -1 ; FILL 0",
        None),

    # ====================
    # = ASSERT Statement =
    # ====================

    T("ASSERT Traditional",
        "ASSERT 0",
        b""),

    T("ASSERT Traditional Failure",
        "ASSERT -1",
        None),

    T("ASSERT Conditional",
        "ASSERT 1 > 0",
        b""),

    T("ASSERT Conditional Failure",
        "ASSERT 1 < 0",
        None),

    T("ASSERT Traditional Expression Failure",
        "ASSERT 1 - 2",
        None),

    # ====================
    # = STRING Statement =
    # ====================
    # incomplete

    T("STRING Basic",
        "ORG 0 ; STRING \"Hello World\"",
        b"Hello World"),

    # ====================
    # = BASE64 Statement =
    # ====================

    T("BASE64",
        " BASE64 \"RXZlbnQgQXNzZW1ibGVy\"",
        b"Event Assembler"),

    # =====================
    # = PROTECT Statement =
    # =====================

    T("PROTECT Basic",
        "PROTECT 0 4 ; ORG 0 ; BYTE 1",
        None),

    T("PROTECT Edge 1",
        "PROTECT 0 4 ; ORG 3 ; BYTE 1",
        None),

    T("PROTECT Edge 2",
        "PROTECT 0 4 ; ORG 4 ; BYTE 1",
        b"\x00\x00\x00\x00\x01"),

    T("PROTECT Late",
        "ORG 0 ; BYTE 1 ; PROTECT 0 4",
        b"\x01"),

    # default PROTECT end is start + 4
    T("PROTECT Default range 1",
        "PROTECT 0 ; ORG 4 ; BYTE 1",
        b"\x00\x00\x00\x00\x01"),

    # default PROTECT end is start + 4
    T("PROTECT Default range 2",
        "PROTECT 0 ; ORG 3 ; BYTE 1",
        None),

]

from ea_test import EATest as T


TESTS = [
    T("Label Basic",
        "ORG 4 ; MyLabel: ; ORG 0 ; BYTE MyLabel",
        b'\x04'),

    T("Label None",
        "ORG 0 ; BYTE MyLabel",
        None),

    T("Label Address",
        "ORG 4 ; MyLabel: ; ORG 0 ; WORD MyLabel",
        b'\x04\x00\x00\x08'),

    T("Label POIN",
        "ORG 4 ; MyLabel: ; ORG 0 ; POIN MyLabel",
        b'\x04\x00\x00\x08'),

    T("Label Forward",
        "ORG 0 ; WORD MyLabel ; MyLabel:",
        b'\x04\x00\x00\x08'),

    T("Symbol Basic",
        "MySymbol := 0xBEEF ; ORG 0 ; SHORT MySymbol",
        b'\xEF\xBE'),

    T("Symbol Reference Forward",
        "ORG 0 ; SHORT MySymbol ; MySymbol := 0xBEEF",
        b'\xEF\xBE'),

    T("Symbol Evaluate Forward",
        "MySymbol := MyLabel + 0xA0 ; ORG 0 ; BYTE MySymbol ; MyLabel:",
        b'\xA1'),

    T("Scope Basic",
        "ORG 0 ; { MyLabel: BYTE MyLabel + 1 ; }",
        b'\x01'),

    T("Scope Failure",
        "ORG 0 ; { MyLabel: BYTE 0 ; } BYTE MyLabel",
        None),

    T("Scope Up",
        "ORG 0 ; MyLabel: { BYTE MyLabel + 1 ; }",
        b'\x01'),
]

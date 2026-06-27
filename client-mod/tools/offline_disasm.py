"""Disassemble whole functions from the unpacked image dump (ddon_image.bin).

File offset == VA - 0x400000 (the dump's section headers were rewritten to memory layout).
Reliable full-function disassembly (no live fragments / misalignment).

Usage:
    python offline_disasm.py <va_hex> [count]          # disasm `count` instructions (default 80)
    python offline_disasm.py <va_hex> --until-ret       # disasm until a ret/jmp tail
"""
import sys
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

IMG = r"D:\DDON\client-mod\ddon_image.bin"
BASE = 0x400000

with open(IMG, "rb") as f:
    DATA = f.read()


def disasm(va, count=80, until_ret=False):
    off = va - BASE
    if off < 0 or off >= len(DATA):
        print(f"VA {va:#x} out of range")
        return
    code = DATA[off: off + 0x4000]
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    n = 0
    for ins in md.disasm(code, va):
        print(f"{ins.address:#010x}  {ins.bytes.hex():<20} {ins.mnemonic} {ins.op_str}")
        n += 1
        if until_ret and ins.mnemonic in ("ret", "retn"):
            break
        if not until_ret and n >= count:
            break


if __name__ == "__main__":
    va = int(sys.argv[1], 16)
    if len(sys.argv) > 2 and sys.argv[2] == "--until-ret":
        disasm(va, until_ret=True)
    else:
        cnt = int(sys.argv[2]) if len(sys.argv) > 2 else 80
        disasm(va, cnt)

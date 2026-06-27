"""Disassemble live-dumped bytes captured by the probe's `dump` command.

The probe writes lines like:
    ... [probe]   0x0043b2c0: c5 27 fa 02 8b 44 24 0c ... | ascii
This script reconstructs an address->byte map from those lines (live, unpacked
memory) and disassembles a requested VA range with Capstone (x86-32).

Usage:
    python live_disasm.py <start_va_hex> <len> [logfile]
"""
import re
import sys
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

LOG = r"C:\Users\Public\ddon_mod.log"
LINE = re.compile(r"(0x[0-9a-fA-F]{8}):\s+((?:[0-9a-fA-F]{2}\s+){1,16})")


def load_bytes(path):
    mem = {}
    with open(path, "r", errors="ignore") as f:
        for ln in f:
            m = LINE.search(ln)
            if not m:
                continue
            base = int(m.group(1), 16)
            hexs = m.group(2).split()
            for i, h in enumerate(hexs):
                mem[base + i] = int(h, 16)
    return mem


def contiguous(mem, start, length):
    out = bytearray()
    for a in range(start, start + length):
        if a not in mem:
            break
        out.append(mem[a])
    return bytes(out)


def main():
    start = int(sys.argv[1], 16)
    length = int(sys.argv[2], 0) if len(sys.argv) > 2 else 0x80
    path = sys.argv[3] if len(sys.argv) > 3 else LOG
    mem = load_bytes(path)
    code = contiguous(mem, start, length)
    if not code:
        print(f"no bytes for {start:#010x} in {path} (dump it first)")
        return
    print(f"# {len(code)} bytes @ {start:#010x}")
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    md.detail = False
    for ins in md.disasm(code, start):
        print(f"{ins.address:#010x}  {ins.bytes.hex():<20} {ins.mnemonic} {ins.op_str}")


if __name__ == "__main__":
    main()

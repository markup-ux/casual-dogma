"""
disasm_off.py - OFFLINE disassembly from the unpacked dump (no game needed).

Usage:
  python disasm_off.py vtable <vtableVA> [count]      # dump vtable fn ptrs + disasm each
  python disasm_off.py code <VA> [len]                # disasm a range

Flags memory writes to globals (mov [imm32], ...) and field writes [reg+off], which is how
we spot where the active-palette / slot state is stored.
Output: disasm_off.log
"""
import os, sys, struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

HERE = os.path.dirname(os.path.abspath(__file__))
DUMP = os.path.join(HERE, "..", "tools", "re", "dump", "ddo_mem.bin")
BASE = 0x400000
LOG = open(os.path.join(HERE, "disasm_off.log"), "w", encoding="utf-8")
def out(s=""):
    print(s); LOG.write(s + "\n"); LOG.flush()

data = open(DUMP, "rb").read()
SIZE = len(data)
LO, HI = BASE, BASE + SIZE
md = Cs(CS_ARCH_X86, CS_MODE_32)
md.detail = True

def off(va): return va - BASE

def disasm(va, n, label=""):
    o = off(va)
    if o < 0 or o + n > SIZE:
        out(f"  [out of range {va:#x}]"); return
    code = data[o:o + n]
    if label: out(label)
    for ins in md.disasm(code, va):
        note = ""
        ops = ins.op_str
        if ins.mnemonic == "mov" and ops.startswith("dword ptr ["):
            # write to memory (global or field)
            note = "   <== WRITE"
        if ins.mnemonic in ("call", "jmp") and not ops.startswith("0x"):
            note = "   (indirect)"
        out(f"  {ins.address:#010x}: {ins.mnemonic:<7} {ops}{note}")
        if ins.mnemonic == "ret":
            break

def main():
    if len(sys.argv) < 3:
        out("usage: vtable <va> [count] | code <va> [len]"); return
    mode = sys.argv[1]
    va = int(sys.argv[2], 0)
    if mode == "vtable":
        count = int(sys.argv[3], 0) if len(sys.argv) > 3 else 12
        out(f"vtable @ {va:#x}")
        fns = []
        for i in range(count):
            o = off(va) + i * 4
            fp = struct.unpack_from("<I", data, o)[0]
            inrange = LO <= fp < HI
            out(f"  [{i:2}] -> {fp:#x}{'' if inrange else '  (not code)'}")
            if inrange:
                fns.append((i, fp))
        for i, fp in fns:
            disasm(fp, 0x140, f"\n--- vtbl[{i}] @ {fp:#x} ---")
    elif mode == "code":
        n = int(sys.argv[3], 0) if len(sys.argv) > 3 else 0x140
        disasm(va, n, f"code @ {va:#x}")
    out("\nDONE")

if __name__ == "__main__":
    main()

import struct, sys
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

EXE = r"D:\DDON\Client\Dragon's Dogma Online\DDO.exe"
IMAGE_BASE = 0x400000

def parse_sections(data):
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    assert data[pe:pe+4] == b"PE\0\0"
    coff = pe + 4
    nsec = struct.unpack_from("<H", data, coff+2)[0]
    opt = struct.unpack_from("<H", data, coff+16)[0]
    sec = coff + 20 + opt
    secs = []
    for i in range(nsec):
        off = sec + i*40
        name = data[off:off+8].rstrip(b"\0").decode("latin1")
        vsize, vaddr, rsize, raddr = struct.unpack_from("<IIII", data, off+8)
        secs.append((name, vaddr, vsize, raddr, rsize))
    return secs

def rva_to_off(secs, rva):
    for name, vaddr, vsize, raddr, rsize in secs:
        if vaddr <= rva < vaddr + max(vsize, rsize):
            return raddr + (rva - vaddr)
    raise ValueError("rva not mapped")

def main():
    fault_va = int(sys.argv[1], 16) if len(sys.argv) > 1 else 0x016d3498
    rva = fault_va - IMAGE_BASE
    data = open(EXE, "rb").read()
    secs = parse_sections(data)
    off = rva_to_off(secs, rva)

    print(f"fault_va={fault_va:#x} rva={rva:#x} file_off={off:#x}")
    print("bytes@fault:", data[off:off+16].hex())
    print("expect live: f30f1102 f30f1044 2418f30f 1142...")
    print()

    md = Cs(CS_ARCH_X86, CS_MODE_32)
    # alignment search: find the largest back-offset that decodes cleanly and lands on fault
    best = None
    for k in range(0, 0x100):
        start = off - k
        addr = IMAGE_BASE + (rva - k)
        ok = True
        landed = False
        for ins in md.disasm(data[start:off+0x60], addr):
            if ins.address == fault_va:
                landed = True
                break
            if ins.address > fault_va:
                ok = False
                break
        if ok and landed:
            best = k
    if best is None:
        best = 0
    start = off - best
    addr = IMAGE_BASE + (rva - best)
    print(f"aligned start back {best} bytes -> {addr:#010x}\n")
    for ins in md.disasm(data[start:off+0x60], addr):
        marker = "   <<==== FAULT" if ins.address == fault_va else ""
        print(f"{ins.address:#010x}  {ins.bytes.hex():<18} {ins.mnemonic} {ins.op_str}{marker}")

main()

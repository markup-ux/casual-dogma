import sys, struct
from minidump.minidumpfile import MinidumpFile

DMP = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\Public\ddon_mod_crash_17432_20260617_182757.dmp"

mf = MinidumpFile.parse(DMP)
reader = mf.get_reader()

DDO_LO, DDO_HI = 0x00400000, 0x045a3c00

def try_read(addr, size):
    try:
        reader.move(addr)
        return reader.read(size)
    except Exception:
        return None

def hexdump(addr, b):
    out = []
    for i in range(0, len(b), 16):
        chunk = b[i:i+16]
        hexs = " ".join(f"{x:02x}" for x in chunk)
        out.append(f"  {addr+i:#010x}: {hexs}")
    return "\n".join(out)

print("=== THREADS ===")
for t in mf.threads.threads:
    print(f"tid={t.ThreadId}  stack_base={t.Stack.StartOfMemoryRange:#x}  size={t.Stack.DataSize:#x}")

# code around fault + source object
for label, addr, size in [("code 0x16d3440..", 0x016d3440, 0x80),
                          ("srcobj 0x20e18060", 0x20e18060, 0x80)]:
    b = try_read(addr, size)
    print(f"\n=== {label} ===")
    print("  (not in dump)" if b is None else hexdump(addr, b))

# walk each thread stack, list values that point into DDO.exe (return addrs / call chain)
print("\n=== STACK RETURN-ADDRESS CANDIDATES (into DDO.exe) ===")
for t in mf.threads.threads:
    base = t.Stack.StartOfMemoryRange
    size = t.Stack.DataSize
    data = try_read(base, size)
    if not data:
        continue
    hits = []
    for i in range(0, len(data) - 4, 4):
        v = struct.unpack_from("<I", data, i)[0]
        if DDO_LO <= v < DDO_HI:
            hits.append((base + i, v))
    if hits:
        print(f"\n-- tid={t.ThreadId} ({len(hits)} candidates) --")
        for sp, v in hits[:40]:
            print(f"  [sp {sp:#010x}] -> DDO.exe+{v-0x400000:#08x}  ({v:#010x})")

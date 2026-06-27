"""
find_skill_static.py - OFFLINE hunt for the custom-skill / palette code in DDO.exe.

Runs against the unpacked memory image (tools/re/dump/ddo_mem.bin), where
file_offset == VA - 0x400000. No running game needed.

It does three things:
  1) Lists interesting ASCII strings (skill / acquirement / palette / action / etc.)
     with their VAs - MT Framework class names and asserts live here.
  2) Finds code cross-references (push imm32 / mov-imm) to those strings and
     disassembles a window around each, so we can spot the functions that touch
     the custom-skill system.
  3) Dumps the player-actor vtable (RVA 0x16f89d0) function pointers.

Output: find_skill_static.log
"""
import os, re, struct, sys

HERE = os.path.dirname(os.path.abspath(__file__))
DUMP = os.path.join(HERE, "..", "tools", "re", "dump", "ddo_mem.bin")
IMAGE_BASE = 0x400000
PLAYER_VTABLE_RVA = 0x16f89d0

LOG = open(os.path.join(HERE, "find_skill_static.log"), "w", encoding="utf-8")
def out(s=""):
    print(s)
    LOG.write(s + "\n")
    LOG.flush()

KEYWORDS = [
    "CustomSkill", "Acquire", "Acquirement", "Palette", "Skill",
    "Combo", "Shell", "ShlReq", "ShotReq", "UseSkill", "SetSkill",
    "ActiveSkill", "cAcq", "cPlAction", "cActionEnt", "cShell",
    "cPlSkill", "cPlManager", "PlSet", "SkillSlot", "PaletteSwitch",
]

def va_to_off(va): return va - IMAGE_BASE
def off_to_va(off): return off + IMAGE_BASE

def find_strings(data):
    """Return list of (va, text) for printable ASCII strings >=4 matching a keyword."""
    rx = re.compile(rb"[\x20-\x7e]{4,}")
    hits = []
    lowkw = [k.lower().encode() for k in KEYWORDS]
    for m in rx.finditer(data):
        s = m.group()
        ls = s.lower()
        if any(k in ls for k in lowkw):
            hits.append((off_to_va(m.start()), s.decode("ascii", "replace")))
    return hits

def build_dword_index(data, lo, hi):
    """Map dword value (in [lo,hi)) -> list of file offsets, scanning every byte offset.
    Uses numpy if available for speed; else a bounded pure-python pass."""
    try:
        import numpy as np
        a = np.frombuffer(data, dtype=np.uint8).astype(np.uint32)
        v = a[0:-3] | (a[1:-2] << 8) | (a[2:-1] << 16) | (a[3:] << 24)
        mask = (v >= lo) & (v < hi)
        offs = np.nonzero(mask)[0]
        idx = {}
        vals = v[offs]
        for o, val in zip(offs.tolist(), vals.tolist()):
            idx.setdefault(val, []).append(o)
        return idx
    except ImportError:
        out("(numpy not available; slower scan)")
        idx = {}
        n = len(data) - 4
        for o in range(n):
            val = data[o] | (data[o+1] << 8) | (data[o+2] << 16) | (data[o+3] << 24)
            if lo <= val < hi:
                idx.setdefault(val, []).append(o)
        return idx

def disasm_window(data, center_off, before=0x30, after=0x30):
    from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    start = max(0, center_off - before)
    code = data[start:center_off + after]
    lines = []
    for ins in md.disasm(code, off_to_va(start)):
        mark = " <==" if off_to_va(start) <= ins.address <= off_to_va(center_off) + 4 and ins.address >= off_to_va(center_off) - 6 else ""
        lines.append(f"    {ins.address:#010x}: {ins.mnemonic:<7} {ins.op_str}{mark}")
    return lines

def main():
    if not os.path.exists(DUMP):
        out(f"dump not found: {DUMP}"); return
    data = open(DUMP, "rb").read()
    size = len(data)
    img_lo, img_hi = IMAGE_BASE, IMAGE_BASE + size
    out(f"loaded {DUMP} ({size} bytes), image {img_lo:#x}..{img_hi:#x}")

    out("\n=== interesting strings ===")
    strings = find_strings(data)
    # de-dup by text, keep first VA
    seen = {}
    for va, txt in strings:
        if txt not in seen:
            seen[txt] = va
    for txt in sorted(seen, key=lambda t: t.lower()):
        out(f"  {seen[txt]:#010x}  {txt}")
    out(f"({len(seen)} unique strings)")

    out("\n=== building dword index (xref scan) ===")
    idx = build_dword_index(data, img_lo, img_hi)
    out(f"indexed {len(idx)} distinct in-image dword values")

    # Focus xrefs on the highest-signal class-name-ish strings.
    focus = [(va, txt) for txt, va in seen.items()
             if any(k.lower() in txt.lower() for k in
                    ["customskill", "acquirement", "palette", "useskill", "setskill", "activeskill"])]
    out(f"\n=== xrefs to {len(focus)} focus strings ===")
    for va, txt in sorted(focus, key=lambda x: x[1].lower()):
        refs = idx.get(va, [])
        if not refs:
            continue
        out(f"\n  '{txt}' @ {va:#x}  ({len(refs)} refs)")
        for r in refs[:4]:
            out(f"   ref @ {off_to_va(r):#x}:")
            for line in disasm_window(data, r):
                out(line)

    out("\n=== player-actor vtable (RVA 0x16f89d0) ===")
    voff = PLAYER_VTABLE_RVA  # file offset == RVA here (base 0x400000 -> off = VA-base; RVA given is already VA-base)
    # NOTE: 0x16f89d0 is the RVA; the VA is 0x400000+0x16f89d0 = 0x1af89d0.
    for i in range(0, 96):
        o = voff + i * 4
        if o + 4 > size: break
        fp = struct.unpack_from("<I", data, o)[0]
        if img_lo <= fp < img_hi:
            out(f"  vtbl[{i:3}] @ {off_to_va(o):#x} -> {fp:#x}")

    out("\nDONE")

if __name__ == "__main__":
    main()

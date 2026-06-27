"""
find_classreg.py - OFFLINE: build a class registry (name -> DTI singleton, vtable, size)
from DDO.exe's MT Framework DTI init code in the unpacked dump.

DTI init pattern (seen for cHumanActCustomSkillChange / cPaletteParams):
    push <size>
    push <parentDti>
    push <nameStr>
    mov  ecx, <thisDti>
    call <registerDti>
    ...
    mov  dword [<thisDti>], <vtable>

We locate every ref to a class-name string, then parse the small window after it to
recover thisDti, vtable, and size. Prints skill/action/acquirement-related classes and
also resolves which class owns the known player-actor vtable (VA 0x1af89d0).

Output: find_classreg.log
"""
import os, re, struct

HERE = os.path.dirname(os.path.abspath(__file__))
DUMP = os.path.join(HERE, "..", "tools", "re", "dump", "ddo_mem.bin")
BASE = 0x400000
PLAYER_VTABLE = 0x1af89d0

LOG = open(os.path.join(HERE, "find_classreg.log"), "w", encoding="utf-8")
def out(s=""):
    print(s); LOG.write(s + "\n"); LOG.flush()

def off(va): return va - BASE
def va(o): return o + BASE

def main():
    data = open(DUMP, "rb").read()
    size = len(data)
    lo, hi = BASE, BASE + size

    # 1) All class-name-looking ASCII strings (start with a lowercase tag char + uppercase),
    #    e.g. cFoo, rFoo, uFoo, sFoo. Record va->text and text->va.
    rx = re.compile(rb"[a-z][A-Za-z0-9_]*(?:::[A-Za-z0-9_]+)*")
    name_at = {}
    for m in re.finditer(rb"[\x20-\x7e]{3,}", data):
        s = m.group()
        # class names are typically the entire token; require it look like cXxx / rXxx / uXxx
        if re.fullmatch(rb"[crusuip][A-Z][A-Za-z0-9_]*(::[A-Za-z0-9_]+)*", s):
            name_at[va(m.start())] = s.decode()

    # 2) Index in-image dword occurrences (every byte offset) using numpy.
    import numpy as np
    a = np.frombuffer(data, dtype=np.uint8).astype(np.uint32)
    v = a[0:-3] | (a[1:-2] << 8) | (a[2:-1] << 16) | (a[3:] << 24)

    name_vas = np.array(sorted(name_at.keys()), dtype=np.uint32)
    # offsets where a name VA appears
    mask = np.isin(v, name_vas)
    ref_offs = np.nonzero(mask)[0]

    registry = {}  # name -> dict(dti, vtable, size, init_va)
    for ro in ref_offs.tolist():
        nva = int(v[ro])
        name = name_at.get(nva)
        if not name:
            continue
        # parse a window [ro-8 .. ro+0x40] for: mov ecx,imm (B9) ; mov [imm],imm (C7 05)
        w0 = max(0, ro - 12); w1 = min(size, ro + 0x48)
        win = data[w0:w1]
        dti = None; vtable = None; csize = None
        # size: a 'push imm8/imm32' shortly before the name push. Look back a little.
        back = data[max(0, ro - 0x14):ro]
        # mov ecx, imm32  == B9 <imm32>
        i = win.find(b"\xb9")
        # search all B9 in window for one followed by an in-image dword (dti pointer, often >image into data seg)
        for mm in re.finditer(b"\xb9", win):
            p = mm.start()
            if p + 5 <= len(win):
                cand = struct.unpack_from("<I", win, p + 1)[0]
                if lo <= cand < hi:
                    dti = cand
                    # find 'C7 05 <dti> <vtable>' anywhere in image near here (vtable store)
                    break
        if dti is not None:
            # vtable store: C7 05 <dti(==thisDti)> <vtable>
            patt = b"\xc7\x05" + struct.pack("<I", dti)
            j = data.find(patt, ro, ro + 0x80)
            if j != -1 and j + 10 <= size:
                vt = struct.unpack_from("<I", data, j + 6)[0]
                if lo <= vt < hi:
                    vtable = vt
        if dti or vtable:
            prev = registry.get(name)
            if not prev or (vtable and not prev.get("vtable")):
                registry[name] = {"dti": dti, "vtable": vtable, "init_va": va(ro)}

    out(f"parsed {len(registry)} classes with a DTI/vtable")

    # 3) Print skill/action/acquirement-related classes.
    KW = ["skill", "acquire", "acquirement", "palette", "humanact", "action", "shell", "combo", "act"]
    out("\n=== skill/action/acquirement classes ===")
    for name in sorted(registry):
        ln = name.lower()
        if any(k in ln for k in KW):
            r = registry[name]
            dti = f"{r['dti']:#x}" if r['dti'] else "?"
            vt = f"{r['vtable']:#x}" if r['vtable'] else "?"
            out(f"  {name:<40} dti={dti:<12} vtable={vt:<12} init@{r['init_va']:#x}")

    # 4) Which class owns the player-actor vtable?
    out(f"\n=== owner of player-actor vtable {PLAYER_VTABLE:#x} ===")
    owner = [n for n, r in registry.items() if r.get("vtable") == PLAYER_VTABLE]
    out(f"  exact vtable match: {owner}")
    # also nearby vtables (multiple-inheritance secondary vtables)
    near = [(n, r) for n, r in registry.items()
            if r.get("vtable") and abs(r['vtable'] - PLAYER_VTABLE) <= 0x200]
    for n, r in sorted(near, key=lambda x: x[1]['vtable']):
        out(f"  near: {n:<40} vtable={r['vtable']:#x} (delta {r['vtable']-PLAYER_VTABLE:+#x})")

    out("\nDONE")

if __name__ == "__main__":
    main()

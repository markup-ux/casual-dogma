"""
apply_sync.py - DDON zone level-sync applier (client side, external, non-invasive).

While the local player is in a zone the server flagged for level sync, this process lowers the
player's (and own pawns') live combat ATTACK values in the game's memory so an over-leveled
character fights fairly. It also patches the Status menu parameter block(s) so Phys./Magick
Attack and related offensive totals show the synced values instead of real gear-inflated stats. It uses only external ReadProcessMemory/WriteProcessMemory (no code
injection, no debug registers) - the one method reverse-engineering showed the client does not fight.

Identification (robust, no value guessing)
------------------------------------------
The player's combat-stat object is a C++ object whose first dword is a fixed vtable in DDO.exe.
RE found:
    vtable RVA      = 0x16f89d0   (DDO.exe loads at fixed base 0x400000, no ASLR)
    PhysAtk field   = base + 0x64
    MagAtk  field   = base + 0x68
There is a pool of ~100 of these objects; all are zero except the live combat actors. So the
player (and own pawns) are exactly the instances with non-zero attack values. We scale those and
remember each one's true/scaled values so we never double-scale, re-apply after the game recomputes
(equip/zone), and restore when sync ends.

Sync factor comes from the server via a JSON signal file
(default D:\\DDON\\client-mod\\sync\\sync_state.json), written by LevelSyncManager.

Recoverable HP (gray bar) pinning for characters below max level uses a separate signal file
(default D:\\DDON\\client-mod\\sync\\recoverable_hp_state.json), written by RecoverableHpManager.
The applier scans for the player's HP param block (+0x00 cur, +0x08 recoverable, +0x10 max) and
keeps recoverable pinned to max while the signal is active.

Run elevated (the game runs elevated):
    python apply_sync.py
Options:  --signal <path>   --hp-signal <path>   --interval <sec>   --once   --verbose
"""
import ctypes as C
from ctypes import wintypes as W
import sys, os, json, struct, time
import urllib.request, urllib.parse

PQI=0x0400; PVR=0x0010; PVW=0x0020; PVO=0x0008; TH32CS_SNAPPROCESS=0x2
TH32CS_SNAPMODULE=0x8; TH32CS_SNAPMODULE32=0x10
MEM_COMMIT=0x1000; PAGE_NOACCESS=0x01; PAGE_GUARD=0x100; MEM_PRIVATE=0x20000
k32=C.WinDLL("kernel32", use_last_error=True)
k32.OpenProcess.restype = W.HANDLE
k32.ReadProcessMemory.argtypes = [W.HANDLE, W.LPCVOID, W.LPVOID, C.c_size_t, C.POINTER(C.c_size_t)]
k32.ReadProcessMemory.restype = W.BOOL
k32.WriteProcessMemory.argtypes = [W.HANDLE, W.LPVOID, W.LPCVOID, C.c_size_t, C.POINTER(C.c_size_t)]
k32.WriteProcessMemory.restype = W.BOOL

def app_dir():
    # When frozen by PyInstaller, files live next to the .exe, not the temp _MEIPASS extraction dir.
    if getattr(sys, "frozen", False):
        return os.path.dirname(sys.executable)
    return os.path.dirname(os.path.abspath(__file__))

DEFAULT_SIGNAL = os.path.join("D:\\","DDON","client-mod","sync","sync_state.json")
DEFAULT_HP_SIGNAL = os.path.join("D:\\","DDON","client-mod","sync","recoverable_hp_state.json")
VTABLE_RVA = 0x16f89d0
CHAR_VTABLE_RVA = 0x193d3a0   # char/status component (RE: absolute 0x01d3d3a0 @ base 0x400000)
OFF_PHYS   = 0x64
OFF_MAG    = 0x68
# Status > Parameters tab: contiguous u32 block (phys, mag, def, mdef, str, magick, knock, end, chance, exhaust, stun).
STATUS_PARAM_OFFENSE = (
    (0x00, "phys"),
    (0x04, "mag"),
    (0x18, "phys"),
    (0x20, "phys"),
    (0x24, "phys"),
    (0x28, "phys"),
)
STATUS_SCAN_MIN_ATK = 50
STATUS_RESCAN_SEC = 1.0
STATUS_SCAN_MAX_HITS = 128
MAG_OFF_CANDIDATES = (4, 8, 12, 16)
# Loss gauge / recoverable HP (RE summary)
#
# HUD layout (left -> right on the HP bar):
#   Green  = current HP        (char+0x7e8, param block +0x00)
#   Gray   = healable gap      (between green and the white cap line)
#   Dark   = permanent loss    (between white cap and absolute max)
#
# The white vertical tick on the bar is the recoverable HP *ceiling*:
#   char+0x7f0 / param block +0x08  ("recover" in RE notes)
# Absolute max HP:
#   char+0x7f8 / param block +0x10
#
# When the loss gauge activates, the game lowers +0x7f0 so the dark zone grows.
# Priests/rest can heal green back up to +0x7f0 but not past it.
#
# Pinning for sub-cap characters: keep +0x7f0 (and param+8) == max every frame.
# Server RPC echo updates other players; local HUD needs the elevated applier.
CHAR_OFF_CUR = 0x7e8
CHAR_OFF_REC = 0x7f0
CHAR_OFF_MAX = 0x7f8
PALETTE_VTABLE_RVA = 0x1819140  # cPaletteParams (status/equip UI), abs 0x1c19140 @ 0x400000
PARAM_OFF_REC = 8
PARAM_OFF_MAX = 0x10
APPLIER_BUILD = "2026-06-27-status-scan2"
HEARTBEAT_SEC = 15.0
_log_fp = None

def _setup_log(once, dry_run):
    global _log_fp
    name = "applier_test.log" if (once or dry_run) else "applier.log"
    mode = "w" if (once or dry_run) else "a"
    try:
        _log_fp = open(os.path.join(app_dir(), name), mode, encoding="utf-8")
    except Exception:
        _log_fp = None

def applog(msg):
    line = msg if msg.endswith("\n") else msg + "\n"
    if _log_fp:
        try:
            _log_fp.write(line)
            _log_fp.flush()
        except Exception:
            pass
    try:
        sys.__stdout__.write(line)
        sys.__stdout__.flush()
    except Exception:
        pass

class PE32(C.Structure):
    _fields_=[("dwSize",W.DWORD),("cntUsage",W.DWORD),("th32ProcessID",W.DWORD),
              ("th32DefaultHeapID",C.c_void_p),("th32ModuleID",W.DWORD),("cntThreads",W.DWORD),
              ("th32ParentProcessID",W.DWORD),("pcPriClassBase",C.c_long),("dwFlags",W.DWORD),
              ("szExeFile",C.c_char*260)]
class ME32(C.Structure):
    _fields_=[("dwSize",W.DWORD),("th32ModuleID",W.DWORD),("th32ProcessID",W.DWORD),
              ("GlblcntUsage",W.DWORD),("ProccntUsage",W.DWORD),("modBaseAddr",C.POINTER(C.c_byte)),
              ("modBaseSize",W.DWORD),("hModule",C.c_void_p),("szModule",C.c_char*256),("szExePath",C.c_char*260)]
class MBI(C.Structure):
    _fields_=[("BaseAddress",C.c_ulonglong),("AllocationBase",C.c_ulonglong),
              ("AllocationProtect",W.DWORD),("__a1",W.DWORD),("RegionSize",C.c_ulonglong),
              ("State",W.DWORD),("Protect",W.DWORD),("Type",W.DWORD),("__a2",W.DWORD)]

def find_pid(name=b"ddo.exe"):
    snap=k32.CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS,0); pe=PE32(); pe.dwSize=C.sizeof(pe); pid=None
    if k32.Process32First(snap,C.byref(pe)):
        while True:
            if pe.szExeFile.lower()==name: pid=pe.th32ProcessID; break
            if not k32.Process32Next(snap,C.byref(pe)): break
    k32.CloseHandle(snap); return pid

def module_base(pid, name=b"ddo.exe"):
    snap=k32.CreateToolhelp32Snapshot(TH32CS_SNAPMODULE|TH32CS_SNAPMODULE32,pid)
    me=ME32(); me.dwSize=C.sizeof(me); base=0
    if k32.Module32First(snap,C.byref(me)):
        while True:
            if me.szModule.lower()==name:
                base=C.cast(me.modBaseAddr,C.c_void_p).value or 0; break
            if not k32.Module32Next(snap,C.byref(me)): break
    k32.CloseHandle(snap); return base

def open_proc(pid):
    return k32.OpenProcess(PQI|PVR|PVW|PVO, False, pid)

def read_u32(h, addr):
    buf=(C.c_char*4)(); got=C.c_size_t(0)
    if k32.ReadProcessMemory(h,C.c_void_p(addr),buf,4,C.byref(got)) and got.value==4:
        return struct.unpack("<I",buf.raw)[0]
    return None

def write_u32(h, addr, val):
    nb=struct.pack("<I",val); gw=C.c_size_t(0)
    return bool(k32.WriteProcessMemory(h,C.c_void_p(addr),nb,4,C.byref(gw)))

def read_f32(h, addr):
    v=read_u32(h, addr)
    if v is None:
        return None
    return struct.unpack("<f", struct.pack("<I", v))[0]

def write_f32(h, addr, val):
    return write_u32(h, addr, struct.unpack("<I", struct.pack("<f", float(val)))[0])

def _region_scannable(mbi):
    if mbi.State != MEM_COMMIT or mbi.Protect in (0, PAGE_NOACCESS) or (mbi.Protect & PAGE_GUARD):
        return False
    return mbi.Type in (MEM_PRIVATE, 0x40000)  # private heap + mapped (UI buffers)

def scan_committed_u32_pattern(h, pattern, max_hits=STATUS_SCAN_MAX_HITS):
    """Return aligned addresses matching a 4-byte pattern in private/mapped committed memory."""
    if not pattern or len(pattern) != 4:
        return []
    out=[]; mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
    while addr<limit and len(out)<max_hits:
        if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
        rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
        if _region_scannable(mbi):
            pos=0
            while pos<rs and len(out)<max_hits:
                n=min(CHUNK,rs-pos); buf=(C.c_char*n)(); got=C.c_size_t(0)
                if k32.ReadProcessMemory(h,C.c_void_p(rb+pos),buf,n,C.byref(got)) and got.value>=4:
                    raw=buf.raw[:got.value]; s=0
                    while len(out)<max_hits:
                        j=raw.find(pattern,s)
                        if j==-1: break
                        s=j+4
                        if j%4==0:
                            out.append(rb+pos+j)
                pos+=n
        addr=rb+rs
    return out

def scan_actors(h, vtable):
    """Return [(base, physAtk, magAtk)] for objects whose first dword == vtable and attack != 0."""
    pat=struct.pack("<I",vtable); out=[]; mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
    while addr<limit:
        if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
        rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
        if (mbi.State==MEM_COMMIT and mbi.Type==MEM_PRIVATE
            and mbi.Protect not in (0,PAGE_NOACCESS) and not(mbi.Protect&PAGE_GUARD)):
            pos=0
            while pos<rs:
                n=min(CHUNK,rs-pos); buf=(C.c_char*n)(); got=C.c_size_t(0)
                if k32.ReadProcessMemory(h,C.c_void_p(rb+pos),buf,n,C.byref(got)) and got.value>=OFF_MAG+4:
                    raw=buf.raw[:got.value]; s=0
                    while True:
                        j=raw.find(pat,s)
                        if j==-1: break
                        s=j+4
                        if j%4 or j+OFF_MAG+4>len(raw): continue
                        pa=struct.unpack_from("<I",raw,j+OFF_PHYS)[0]
                        ma=struct.unpack_from("<I",raw,j+OFF_MAG)[0]
                        if pa or ma:
                            out.append((rb+pos+j, pa, ma))
                pos+=n
        addr=rb+rs
    return out

def scan_private_u32_pattern(h, pattern, max_hits=STATUS_SCAN_MAX_HITS):
    return scan_committed_u32_pattern(h, pattern, max_hits=max_hits)

def count_u32_hits(h, val, exclude=None):
    """Diagnostic: count committed-memory hits for one u32 value."""
    exclude = exclude or set()
    pat = struct.pack("<I", val)
    return sum(1 for a in scan_committed_u32_pattern(h, pat, max_hits=9999) if a not in exclude)

def _scan_object_for_atk_pairs(h, obj, tp, tm, exclude, found, span=0x1200):
    if not obj or obj < 0x10000:
        return
    for off in range(0, span, 4):
        base = obj + off
        if base in exclude:
            continue
        for mag_off in MAG_OFF_CANDIDATES:
            if read_u32(h, base) == tp and read_u32(h, base + mag_off) == tm:
                _try_add_status_block(found, h, base, tp, tm, mag_off, False, exclude, loose=True)
            fv = read_f32(h, base)
            if fv is not None and int(round(fv)) == tp:
                mv = read_f32(h, base + mag_off)
                if mv is not None and int(round(mv)) == tm:
                    _try_add_status_block(found, h, base, tp, tm, mag_off, True, exclude, loose=True)

def char_base_from_hp_block(hp_block_base):
    """HP param block starts at char+CHAR_OFF_CUR when inline on the char object."""
    return hp_block_base - CHAR_OFF_CUR

def _read_stat_u32(h, addr, as_float=False):
    if as_float:
        fv=read_f32(h, addr)
        if fv is None:
            return None
        return int(round(fv))
    return read_u32(h, addr)

def _write_stat_u32(h, addr, val, as_float=False):
    if as_float:
        return write_f32(h, addr, val)
    return write_u32(h, addr, val)

def looks_like_status_param_block(h, base, tp, tm, mag_off=4, as_float=False, allow_scaled=False, loose=False):
    """Heuristic: Parameters-tab total-stat block starting at phys atk."""
    p=_read_stat_u32(h, base, as_float)
    m=_read_stat_u32(h, base+mag_off, as_float)
    if p is None or m is None:
        return False
    if allow_scaled:
        if not (1 <= p <= tp and 1 <= m <= tm):
            return False
    else:
        if p != tp or m != tm:
            return False
    if loose:
        return True
    def_p=_read_stat_u32(h, base+8, as_float)
    def_m=_read_stat_u32(h, base+12, as_float)
    if def_p is None or def_m is None:
        return False
    if not (1 <= def_p <= 20_000 and 1 <= def_m <= 20_000):
        return False
    knock=_read_stat_u32(h, base+0x18, as_float)
    if knock is not None and knock > max(tp, tm) * 2:
        return False
    return True

def _try_add_status_block(found, h, base, tp, tm, mag_off, as_float, exclude, loose=False):
    if base in exclude:
        return
    if looks_like_status_param_block(
        h, base, tp, tm, mag_off=mag_off, as_float=as_float, loose=loose,
    ):
        found.add((base, mag_off, as_float))

def find_status_param_blocks(h, tp, tm, exclude_addrs, char_vtable=0, char_base=0, palette_vtable=0):
    """Locate Status-menu parameter blocks holding the player's true total atk pair."""
    if tp < STATUS_SCAN_MIN_ATK and tm < STATUS_SCAN_MIN_ATK:
        return []
    found=set()
    exclude=set(exclude_addrs or ())

    # Strategy 1: phys/mag adjacent or with a small gap (u32).
    pat=struct.pack("<I", tp)
    for addr in scan_committed_u32_pattern(h, pat):
        if addr in exclude:
            continue
        for mag_off in MAG_OFF_CANDIDATES:
            if read_u32(h, addr+mag_off) == tm:
                _try_add_status_block(found, h, addr, tp, tm, mag_off, False, exclude)
                _try_add_status_block(found, h, addr, tp, tm, mag_off, False, exclude, loose=True)

    # Strategy 2: same search using float storage (some UI paths use floats).
    patf=struct.pack("<f", float(tp))
    for addr in scan_committed_u32_pattern(h, patf):
        if addr in exclude:
            continue
        for mag_off in MAG_OFF_CANDIDATES:
            mv=_read_stat_u32(h, addr+mag_off, True)
            if mv == tm:
                _try_add_status_block(found, h, addr, tp, tm, mag_off, True, exclude)
                _try_add_status_block(found, h, addr, tp, tm, mag_off, True, exclude, loose=True)

    # Strategy 3: full header anchor phys/mag/pdef/mdef when we can read plausible defs.
    for addr in scan_committed_u32_pattern(h, pat):
        if addr in exclude:
            continue
        if read_u32(h, addr+4) != tm:
            continue
        def_p=read_u32(h, addr+8); def_m=read_u32(h, addr+12)
        if def_p and def_m and 1 <= def_p <= 5000 and 1 <= def_m <= 5000:
            _try_add_status_block(found, h, addr, tp, tm, 4, False, exclude)

    # Strategy 4: local player char object (+ sibling components) from HP anchor.
    if char_base:
        _scan_object_for_atk_pairs(h, char_base, tp, tm, exclude, found)
        for rel in (0x18, 0x1c):
            comp = read_u32(h, char_base + rel)
            if comp:
                _scan_object_for_atk_pairs(h, comp, tp, tm, exclude, found)

    # Strategy 5: char/status vtable instances on heap.
    if char_vtable:
        vt_pat=struct.pack("<I", char_vtable)
        mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
        while addr<limit and len(found)<STATUS_SCAN_MAX_HITS:
            if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
            rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
            if _region_scannable(mbi):
                pos=0
                while pos<rs and len(found)<STATUS_SCAN_MAX_HITS:
                    n=min(CHUNK,rs-pos); buf=(C.c_char*n)(); got=C.c_size_t(0)
                    if k32.ReadProcessMemory(h,C.c_void_p(rb+pos),buf,n,C.byref(got)) and got.value>=0x2c:
                        raw=buf.raw[:got.value]; s=0
                        while len(found)<STATUS_SCAN_MAX_HITS:
                            j=raw.find(vt_pat,s)
                            if j==-1: break
                            s=j+4
                            if j%4: continue
                            obj=rb+pos+j
                            _scan_object_for_atk_pairs(h, obj, tp, tm, exclude, found, span=0x900)
                    pos+=n
            addr=rb+rs

    # Strategy 6: palette UI objects (status/equip menu).
    if palette_vtable:
        vt_pat=struct.pack("<I", palette_vtable)
        mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
        while addr<limit and len(found)<STATUS_SCAN_MAX_HITS:
            if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
            rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
            if _region_scannable(mbi):
                pos=0
                while pos<rs and len(found)<STATUS_SCAN_MAX_HITS:
                    n=min(CHUNK,rs-pos); buf=(C.c_char*n)(); got=C.c_size_t(0)
                    if k32.ReadProcessMemory(h,C.c_void_p(rb+pos),buf,n,C.byref(got)) and got.value>=0x40:
                        raw=buf.raw[:got.value]; s=0
                        while len(found)<STATUS_SCAN_MAX_HITS:
                            j=raw.find(vt_pat,s)
                            if j==-1: break
                            s=j+4
                            if j%4: continue
                            obj=rb+pos+j
                            _scan_object_for_atk_pairs(h, obj, tp, tm, exclude, found, span=0x2000)
                    pos+=n
            addr=rb+rs
    return sorted(found)

def status_offense_fields(mag_off):
    fields=[(0x00, "phys"), (mag_off, "mag")]
    if mag_off == 4:
        fields.extend([(0x18, "phys"), (0x20, "phys"), (0x24, "phys"), (0x28, "phys")])
    return fields

def scale_factor_for(field_kind, physF, magF):
    return magF if field_kind == "mag" else physF

def apply_status_param_block(h, block_key, tp, tm, physF, magF, status_state, dry_run=False, verbose=False):
    """Scale offensive fields in one Parameters-tab stat block; return number of fields written."""
    base, mag_off, as_float = block_key
    st=status_state.get(block_key)
    allow_scaled=bool(st)
    if not looks_like_status_param_block(
        h, base, tp, tm, mag_off=mag_off, as_float=as_float, allow_scaled=allow_scaled, loose=bool(st),
    ):
        return 0
    sp = max(1, int(round(tp * physF)))
    sm = max(1, int(round(tm * magF)))
    if st and _read_stat_u32(h, base, as_float) == st["sp"] and _read_stat_u32(h, base+mag_off, as_float) == st["sm"]:
        return 0
    wrote=0
    fields={}
    for off, kind in status_offense_fields(mag_off):
        cur=_read_stat_u32(h, base+off, as_float)
        if cur is None or cur == 0:
            continue
        prev=(st.get("fields") or {}).get(off) if st else None
        if prev and cur == prev["s"]:
            fields[off]=prev
            continue
        if prev and cur == prev["t"]:
            true_v=prev["t"]
        else:
            true_v=cur
        fac=scale_factor_for(kind, physF, magF)
        scaled=max(1, int(round(true_v*fac)))
        fields[off]={"t": true_v, "s": scaled, "k": kind}
        if scaled == cur:
            continue
        if dry_run:
            print(f"[applier]   DRY-RUN status {base:#x}+{off:#x} {true_v} -> {scaled}")
            wrote+=1
            continue
        if _write_stat_u32(h, base+off, scaled, as_float):
            wrote+=1
            if verbose:
                print(f"[applier]   status {base:#x}+{off:#x} {true_v} -> {scaled}")
    if fields:
        status_state[block_key]={
            "tp": tp,
            "tm": tm,
            "sp": fields.get(0x00, {}).get("s", sp),
            "sm": fields.get(mag_off, {}).get("s", sm),
            "mag_off": mag_off,
            "as_float": as_float,
            "fields": fields,
        }
    return wrote

def restore_status_param_blocks(h, status_state):
    restored=0
    for block_key, st in list(status_state.items()):
        as_float = st.get("as_float", False)
        for off, meta in st.get("fields", {}).items():
            cur=_read_stat_u32(h, block_key[0]+off, as_float)
            if cur is not None and cur == meta["s"]:
                if _write_stat_u32(h, block_key[0]+off, meta["t"], as_float):
                    restored+=1
    status_state.clear()
    return restored

def player_true_atk_from_actors(actors, actor_state):
    """Pick the highest total-atk live actor as the local player (use true atk from state when scaled)."""
    best=None
    for base, pa, ma in actors:
        st=actor_state.get(base)
        tp= st["tp"] if st else pa
        tm= st["tm"] if st else ma
        score=tp+tm
        if best is None or score > best[0]:
            best=(score, tp, tm)
    if best:
        return best[1], best[2]
    return None, None

def combat_actor_exclude_addrs(actor_bases):
    ex=set()
    for base in actor_bases:
        ex.add(base+OFF_PHYS)
        ex.add(base+OFF_MAG)
    return ex

def _hp_ok(cur, rec, mx, relaxed=False):
    if not (1 <= cur <= 200_000 and 1 <= mx <= 200_000):
        return False
    if relaxed:
        return 1 <= rec <= mx and cur <= mx
    return cur <= rec <= mx

def scan_hp_blocks(h, maxhits=64, relaxed=False):
    """Return [(base, cur, rec, mx, stc, stm)] matching the live RE HP param signature."""
    out=[]; mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
    while addr<limit and len(out)<maxhits:
        if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
        rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
        if (mbi.State==MEM_COMMIT and mbi.Type==MEM_PRIVATE
            and mbi.Protect not in (0,PAGE_NOACCESS) and not(mbi.Protect&PAGE_GUARD)):
            pos=0
            while pos<rs and len(out)<maxhits:
                n=min(CHUNK,rs-pos); buf=(C.c_char*n)(); got=C.c_size_t(0)
                if k32.ReadProcessMemory(h,C.c_void_p(rb+pos),buf,n,C.byref(got)) and got.value>=0x20:
                    raw=buf.raw[:got.value]; i=0
                    while i+0x20<=len(raw) and len(out)<maxhits:
                        if (struct.unpack_from("<I",raw,i+4)[0]==0
                            and struct.unpack_from("<I",raw,i+0xc)[0]==0
                            and struct.unpack_from("<I",raw,i+0x14)[0]==0):
                            cur=struct.unpack_from("<I",raw,i)[0]
                            rec=struct.unpack_from("<I",raw,i+8)[0]
                            mx=struct.unpack_from("<I",raw,i+0x10)[0]
                            stc=struct.unpack_from("<f",raw,i+0x18)[0]
                            stm=struct.unpack_from("<f",raw,i+0x1c)[0]
                            st_ok=(stc==stc and stm==stm and 1.0<=stc<=200_000.0 and stc<=stm<=200_000.0)
                            if _hp_ok(cur, rec, mx, relaxed) and st_ok:
                                out.append((rb+pos+i, cur, rec, mx, stc, stm))
                        i+=4
                pos+=n
        addr=rb+rs
    return out

def read_hp_mirror(h, player_base):
    """Live cur/rec/mx at param block and char object mirrors."""
    cur=read_u32(h, player_base)
    rec=read_u32(h, player_base+PARAM_OFF_REC)
    mx=read_u32(h, player_base+PARAM_OFF_MAX)
    char_base = player_base - CHAR_OFF_CUR if player_base > CHAR_OFF_CUR else 0
    char_cur = read_u32(h, char_base) if char_base > 0x10000 else None
    char_rec = read_u32(h, char_base + CHAR_OFF_REC) if char_base > 0x10000 else None
    char_mx = read_u32(h, char_base + CHAR_OFF_MAX) if char_base > 0x10000 else None
    return {
        "param": player_base, "char": char_base,
        "cur": cur, "rec": rec, "mx": mx,
        "char_cur": char_cur, "char_rec": char_rec, "char_mx": char_mx,
        "needs_pin": (
            rec is not None and mx is not None and mx > 0
            and (rec != mx or (char_rec is not None and char_rec != mx))
        ),
    }

def log_hp_diag(h, rows, pin_name, tag="heartbeat", relaxed=False):
    """Summarize HP scan/pin state for applier.log troubleshooting."""
    mode = "relaxed" if relaxed else "strict"
    if not rows:
        print(f"[applier] {tag} HP scan ({mode}): 0 blocks matched")
        return
    dup_max = {}
    for r in rows:
        dup_max[r[3]] = dup_max.get(r[3], 0) + 1
    loss_rows = [r for r in rows if r[2] < r[3] and dup_max.get(r[3], 0) < 3 and 0 < r[3] < 60_000]
    tiers = {}
    for r in loss_rows:
        tiers.setdefault(r[3], []).append(r)
    tier_txt = ", ".join(f"mx={mx}:{len(v)}" for mx, v in sorted(tiers.items()))
    player_base = pick_player_hp_block(rows)
    if player_base is None:
        print(f"[applier] {tag} HP scan ({mode}): {len(rows)} blocks, no player pick loss=[{tier_txt}]")
        return
    player_row = next((r for r in rows if r[0] == player_base), None)
    d = read_hp_mirror(h, player_base)
    if player_row:
        d["cur"], d["rec"], d["mx"] = player_row[1], player_row[2], player_row[3]
        d["needs_pin"] = d["rec"] != d["mx"]
    print(
        f"[applier] {tag} pin='{pin_name or '?'}' blocks={len(rows)} loss=[{tier_txt}] "
        f"hud_pick@{d['param']:#x} {d['cur']}/{d['rec']}/{d['mx']} needs_pin={d['needs_pin']}"
    )

def pick_player_hp_block(rows):
    """Pick the HUD-relevant mirror: prefer displayed max tier with active loss gauge."""
    if not rows:
        return None
    dup_max = {}
    for r in rows:
        dup_max[r[3]] = dup_max.get(r[3], 0) + 1

    def ok(r):
        return 0 < r[3] < 60_000 and dup_max.get(r[3], 0) < 3

    candidates = [r for r in rows if ok(r)]
    if not candidates:
        return None

    def score(r):
        rec_loss = 1 if r[2] < r[3] else 0
        st_full = 1 if abs(r[4] - r[5]) < 0.5 else 0
        # Prefer the lower max tier (760 HUD) over buff-inflated mirrors (1060).
        return (rec_loss, st_full, -r[3])

    return max(candidates, key=score)[0]

def pin_all_player_recoverable(h, rows, dry_run=False, verbose=False):
    """Pin every non-cluster HP mirror at its own absolute max."""
    if not rows:
        return False
    dup_max = {}
    for r in rows:
        dup_max[r[3]] = dup_max.get(r[3], 0) + 1
    pinned = False
    seen = set()
    for r in rows:
        base, mx = r[0], r[3]
        if base in seen or mx <= 0 or mx >= 60_000 or dup_max.get(mx, 0) >= 3:
            continue
        seen.add(base)
        if pin_player_recoverable(h, base, dry_run=dry_run, verbose=verbose, target_mx=mx):
            pinned = True
    return pinned

def pin_player_recoverable(h, player_base, dry_run=False, verbose=False, target_mx=None):
    """Keep the loss-gauge ceiling (recoverable cap) pinned to absolute max."""
    cur = read_u32(h, player_base)
    rec = read_u32(h, player_base + PARAM_OFF_REC)
    mx = read_u32(h, player_base + PARAM_OFF_MAX)
    if target_mx is not None:
        mx = target_mx
    if cur is None or rec is None or mx is None or mx == 0 or cur == 0:
        return False
    char_base = player_base - CHAR_OFF_CUR
    char_rec = read_u32(h, char_base + CHAR_OFF_REC) if char_base > 0x10000 else None
    needs = rec != mx or (char_rec is not None and char_rec != mx)
    if not needs:
        return False
    if dry_run:
        print(f"[applier] DRY-RUN loss-gauge cap {rec}/{char_rec} -> {mx} param@{player_base:#x} char@{char_base:#x}")
        return True
    ok = write_u32(h, player_base + PARAM_OFF_REC, mx)
    if char_base > 0x10000:
        ok = write_u32(h, char_base + CHAR_OFF_REC, mx) and ok
    if ok:
        print(f"[applier] loss-gauge cap {rec} -> {mx} (char {char_rec}) param@{player_base:#x}")
    elif verbose:
        print(f"[applier] loss-gauge pin write failed param@{player_base:#x}")
    return ok

def load_signal(path):
    try:
        with open(path,"r") as f: return json.load(f)
    except Exception:
        return {}

def fetch_factors_network(server_url, char_name):
    """Network mode: GET <server>/rpc/levelsync?name=... Returns (physF, magF, name) or None."""
    sync, _, _ = fetch_network_state(server_url, char_name)
    return sync

def fetch_network_state(server_url, char_name):
    """Network mode: fetch level-sync and recoverable-HP pin state in one request."""
    try:
        url = server_url.rstrip("/") + "/rpc/levelsync?name=" + urllib.parse.quote(char_name)
        with urllib.request.urlopen(url, timeout=4) as r:
            e = json.load(r)
        le = {str(k).lower():v for k,v in e.items()}
        sync = None
        if le.get("synced"):
            sync = (float(le.get("physfactor",1.0)), float(le.get("magfactor",1.0)), char_name)
        pin = bool(le.get("pinrecoverablehp"))
        return sync, pin, char_name
    except Exception as ex:
        print(f"[applier] network fetch error: {ex!r}")
        return None, False, char_name

def pick_pin_recoverable(sig, name=None):
    """File mode: read recoverable_hp_state.json. Returns (pin, name) or (False, None)."""
    if name and name in sig:
        e=sig[name]
        if isinstance(e,dict):
            le={str(k).lower():v for k,v in e.items()}
            return bool(le.get("pinrecoverablehp")), name
    for n,e in sig.items():
        if not isinstance(e,dict): continue
        le={str(k).lower():v for k,v in e.items()}
        if le.get("pinrecoverablehp"):
            return True, n
    return False, None

def pick_factors(sig):
    """File mode: returns (physF, magF, name) when combat sync is active, else None."""
    for name,e in sig.items():
        if not isinstance(e,dict): continue
        le={str(k).lower():v for k,v in e.items()}
        if le.get("synced"):
            return (
                float(le.get("physfactor",1.0)),
                float(le.get("magfactor",1.0)),
                name,
            )
    return None

def main():
    signal_path = DEFAULT_SIGNAL
    hp_signal_path = DEFAULT_HP_SIGNAL
    interval = 0.75
    once = "--once" in sys.argv
    verbose = "--verbose" in sys.argv
    dry_run = "--dry-run" in sys.argv
    server_url = None; char_name = None
    if "--signal" in sys.argv: signal_path = sys.argv[sys.argv.index("--signal")+1]
    if "--hp-signal" in sys.argv: hp_signal_path = sys.argv[sys.argv.index("--hp-signal")+1]
    if "--interval" in sys.argv: interval = float(sys.argv[sys.argv.index("--interval")+1])
    if "--server" in sys.argv: server_url = sys.argv[sys.argv.index("--server")+1]
    if "--char" in sys.argv: char_name = sys.argv[sys.argv.index("--char")+1]
    network_mode = bool(server_url and char_name)

    _setup_log(once, dry_run)
    print = applog  # pythonw swallows stdout after startup; always write to log file too

    if network_mode:
        print(f"[applier] build={APPLIER_BUILD} network mode server={server_url} char='{char_name}' interval={interval}s")
    else:
        print(f"[applier] build={APPLIER_BUILD} file mode signal={signal_path} hp_signal={hp_signal_path} interval={interval}s")
    state={}     # base -> {"tp":int,"tm":int,"sp":int,"sm":int}
    status_state={}  # (base,mag_off,as_float) -> scaled field map
    true_atk_cache={"tp": 0, "tm": 0}
    applied=False
    hp_pinned=False
    last_hb=0.0
    last_status_scan=0.0
    known_status_bases=set()

    while True:
      try:
        pid=find_pid()
        if not pid:
            state.clear(); status_state.clear(); known_status_bases.clear()
            true_atk_cache={"tp": 0, "tm": 0}
            applied=False; hp_pinned=False
            if once: break
            time.sleep(interval); continue
        h=open_proc(pid)
        if not h:
            print("[applier] OpenProcess failed (run elevated)")
            if once: break
            time.sleep(interval); continue
        mbase=module_base(pid)
        if not mbase:
            k32.CloseHandle(h); time.sleep(interval); continue
        vtable=mbase+VTABLE_RVA

        pin_recoverable=False
        pin_name=None
        if network_mode:
            picked, pin_recoverable, pin_name = fetch_network_state(server_url, char_name)
        else:
            picked=pick_factors(load_signal(signal_path))
            pin_recoverable, pin_name = pick_pin_recoverable(load_signal(hp_signal_path))

        if picked is None:
            if applied:
                for base,st in list(state.items()):
                    cp=read_u32(h,base+OFF_PHYS); cm=read_u32(h,base+OFF_MAG)
                    if cp is not None and cp==st["sp"]: write_u32(h,base+OFF_PHYS,st["tp"])
                    if cm is not None and cm==st["sm"]: write_u32(h,base+OFF_MAG,st["tm"])
                restored=restore_status_param_blocks(h, status_state)
                known_status_bases.clear()
                print(f"[applier] sync cleared -> restored combat + {restored} status field(s)")
                state.clear(); applied=False
        else:
            physF, magF, name = picked
            actors=scan_actors(h, vtable)
            wrote=0
            live=set()
            for base, pa, ma in actors:
                live.add(base)
                st=state.get(base)
                if st and pa==st["sp"] and ma==st["sm"]:
                    continue
                if st:
                    tp, tm = st["tp"], st["tm"]
                else:
                    tp, tm = pa, ma
                if tp > true_atk_cache["tp"]:
                    true_atk_cache["tp"] = tp
                if tm > true_atk_cache["tm"]:
                    true_atk_cache["tm"] = tm
                sp = max(1, int(round(tp*physF)))
                sm = max(1, int(round(tm*magF)))
                if dry_run:
                    print(f"[applier]   DRY-RUN actor {base:#x} would scale {tp}/{tm} -> {sp}/{sm}")
                    wrote+=1
                    continue
                okp = (sp==tp) or write_u32(h, base+OFF_PHYS, sp)
                okm = (sm==tm) or write_u32(h, base+OFF_MAG, sm)
                if okp and okm:
                    state[base]={"tp":tp,"tm":tm,"sp":sp,"sm":sm}; wrote+=1
                    if verbose: print(f"[applier]   actor {base:#x} {tp}/{tm} -> {sp}/{sm}")
            for base in list(state.keys()):
                if base not in live: del state[base]

            tp, tm = player_true_atk_from_actors(actors, state)
            if true_atk_cache["tp"]:
                tp = max(tp or 0, true_atk_cache["tp"])
            if true_atk_cache["tm"]:
                tm = max(tm or 0, true_atk_cache["tm"])
            status_wrote=0
            now=time.time()
            if tp and tm:
                if not known_status_bases or (now - last_status_scan) >= STATUS_RESCAN_SEC:
                    char_vt = mbase + CHAR_VTABLE_RVA
                    palette_vt = mbase + PALETTE_VTABLE_RVA
                    exclude = combat_actor_exclude_addrs(live)
                    char_base = 0
                    hp_rows = scan_hp_blocks(h, maxhits=8, relaxed=True)
                    player_hp = pick_player_hp_block(hp_rows)
                    if player_hp:
                        char_base = char_base_from_hp_block(player_hp)
                    known_status_bases = set(find_status_param_blocks(
                        h, tp, tm, exclude,
                        char_vtable=char_vt,
                        char_base=char_base,
                        palette_vtable=palette_vt,
                    ))
                    last_status_scan = now
                for block_key in list(known_status_bases):
                    status_wrote += apply_status_param_block(
                        h, block_key, tp, tm, physF, magF, status_state,
                        dry_run=dry_run, verbose=verbose,
                    )
                for block_key in list(status_state.keys()):
                    if block_key not in known_status_bases:
                        del status_state[block_key]

            if not applied or wrote or status_wrote:
                exclude = combat_actor_exclude_addrs(live)
                raw_hits = count_u32_hits(h, tp, exclude) if tp else 0
                print(
                    f"[applier] SYNC '{name}' physF={physF:.3f} magF={magF:.3f} "
                    f"actors={len(actors)} combat={wrote} status={status_wrote} "
                    f"statusBlocks={len(known_status_bases)} trueAtk={tp}/{tm} rawHits={raw_hits}"
                )
            applied=True

        if pin_recoverable:
            rows=scan_hp_blocks(h)
            used_relaxed=False
            if not rows:
                rows=scan_hp_blocks(h, relaxed=True)
                used_relaxed=bool(rows)
            now=time.time()
            if verbose or once or (now - last_hb) >= HEARTBEAT_SEC:
                log_hp_diag(h, rows, pin_name or char_name, tag="diag" if (verbose or once) else "heartbeat", relaxed=used_relaxed)
                last_hb=now
            if pin_all_player_recoverable(h, rows, dry_run=dry_run, verbose=verbose):
                if not hp_pinned:
                    player=pick_player_hp_block(rows)
                    print(f"[applier] RECOVERABLE_HP loss-gauge pin active for '{pin_name or char_name or '?'}' @{player:#x}")
                hp_pinned=True
            elif not rows:
                print(f"[applier] RECOVERABLE_HP pin requested but HP scan found 0 blocks (strict+relaxed)")
            elif verbose or once:
                log_hp_diag(h, rows, pin_name or char_name, tag="pin-skip", relaxed=used_relaxed)
        elif hp_pinned:
            print("[applier] RECOVERABLE_HP pin cleared")
            hp_pinned=False

        k32.CloseHandle(h)
        if once: break
        time.sleep(interval)
      except Exception as ex:
        print(f"[applier] loop error: {ex!r}")
        if once: break
        time.sleep(interval)

if __name__=="__main__": main()

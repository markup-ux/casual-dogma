"""
find_level.py - locate the player's Level field inside the combat-actor object.

The applier already identifies combat-actor objects by a fixed vtable in DDO.exe and edits
PhysAtk(+0x64)/MagAtk(+0x68). This tool dumps each live actor and reports every offset whose
value equals a level you pass in, so we can find a "display level" field that is separate from
the EXP/status data that drives the XP bar.

Usage (run elevated, game running, you in-game):
    python find_level.py --level 60
    python find_level.py --level 60 --max-offset 0x800 --verbose

Pass --level as your CURRENT true character level. If your pawns are different levels, the actor
where the match equals YOUR level is your player object.
"""
import ctypes as C
from ctypes import wintypes as W
import sys, struct, os

# Tee all prints to find_level.log next to this script, so an elevated run (no stdout capture) is readable.
_LOG = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "find_level.log"), "w")
class _Tee:
    def __init__(s,*fs): s.fs=fs
    def write(s,t):
        for f in s.fs:
            try: f.write(t); f.flush()
            except Exception: pass
    def flush(s):
        for f in s.fs:
            try: f.flush()
            except Exception: pass
sys.stdout=_Tee(sys.__stdout__,_LOG)

PQI=0x0400; PVR=0x0010; TH32CS_SNAPPROCESS=0x2
TH32CS_SNAPMODULE=0x8; TH32CS_SNAPMODULE32=0x10
MEM_COMMIT=0x1000; PAGE_NOACCESS=0x01; PAGE_GUARD=0x100; MEM_PRIVATE=0x20000
k32=C.WinDLL("kernel32", use_last_error=True)

VTABLE_RVA = 0x16f89d0
OFF_PHYS   = 0x64
OFF_MAG    = 0x68

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

def read(h, addr, n):
    buf=(C.c_char*n)(); got=C.c_size_t(0)
    if k32.ReadProcessMemory(h,C.c_void_p(addr),buf,n,C.byref(got)) and got.value:
        return buf.raw[:got.value]
    return b""

def scan_actor_bases(h, vtable, dump_len):
    """Return [(base, physAtk, magAtk, raw_bytes)] for live combat actors."""
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
                            base=rb+pos+j
                            out.append((base, pa, ma, read(h, base, dump_len)))
                pos+=n
        addr=rb+rs
    return out

def main():
    if "--level" not in sys.argv:
        print("Pass your current level, e.g.: python find_level.py --level 60"); return
    level=int(sys.argv[sys.argv.index("--level")+1])
    max_off=0x600
    if "--max-offset" in sys.argv: max_off=int(sys.argv[sys.argv.index("--max-offset")+1],0)
    verbose="--verbose" in sys.argv

    pid=find_pid()
    if not pid: print("DDO.exe not running"); return
    h=k32.OpenProcess(PQI|PVR, False, pid)
    if not h: print("OpenProcess failed (run elevated)"); return
    mbase=module_base(pid)
    vtable=mbase+VTABLE_RVA
    print(f"pid={pid} base={mbase:#x} vtable={vtable:#x} searching for level={level} up to +{max_off:#x}")

    actors=scan_actor_bases(h, vtable, max_off)
    print(f"live combat actors: {len(actors)}")
    # Tally which offsets hold `level` across actors, to spot a consistent Level field.
    offset_hits={}
    for idx,(base,pa,ma,raw) in enumerate(actors):
        u32_hits=[o for o in range(0,min(len(raw),max_off)-3,4) if struct.unpack_from("<I",raw,o)[0]==level]
        u16_hits=[o for o in range(0,min(len(raw),max_off)-1,2) if struct.unpack_from("<H",raw,o)[0]==level]
        for o in u32_hits: offset_hits[("u32",o)]=offset_hits.get(("u32",o),0)+1
        for o in u16_hits: offset_hits[("u16",o)]=offset_hits.get(("u16",o),0)+1
        print(f"  actor[{idx}] base={base:#x} phys={pa} mag={ma} "
              f"u32@{['%#x'%o for o in u32_hits]} u16@{['%#x'%o for o in u16_hits]}")
        if verbose:
            # Show a window around each u32 hit for context.
            for o in u32_hits:
                lo=max(0,o-8); hi=min(len(raw),o+12)
                print(f"      ctx +{o:#x}: "+" ".join(f"{b:02x}" for b in raw[lo:hi]))

    print("\noffsets holding the level across MULTIPLE actors (likely a real Level field):")
    for (kind,o),cnt in sorted(offset_hits.items(), key=lambda kv:-kv[1]):
        if cnt>=2:
            print(f"  {kind} +{o:#x}  in {cnt}/{len(actors)} actors")
    k32.CloseHandle(h)

if __name__=="__main__": main()

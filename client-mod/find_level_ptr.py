"""
find_level_ptr.py - follow pointers out of the player's combat-actor object to find the
character/status structure that holds the Level (and likely EXP) the HUD reads.

The combat-actor object (vtable in DDO.exe) does not contain Level within +0x4000, but it
should hold pointers to the owning character/context object that does. This walks every
heap-looking pointer inside the actor object (depth 1 and 2) and reports targets that contain
the level value, so we can locate a Level field that is separate from combat stats.

Usage (elevated, in-game):
    python find_level_ptr.py --level 20
Output is also written to find_level_ptr.log next to this script.
"""
import ctypes as C
from ctypes import wintypes as W
import sys, struct, os

_LOG = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "find_level_ptr.log"), "w")
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
HEAP_LO = 0x01000000
HEAP_HI = 0x7fff0000

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

def find_player_actor(h, vtable):
    pat=struct.pack("<I",vtable); best=None; mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
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
                            # Prefer the actor with the largest attack as "the player".
                            if best is None or (pa+ma)>(best[1]+best[2]):
                                best=(rb+pos+j, pa, ma)
                pos+=n
        addr=rb+rs
    return best

def find_level_in(raw, level, max_off):
    u32=[o for o in range(0,min(len(raw),max_off)-3,4) if struct.unpack_from("<I",raw,o)[0]==level]
    u16=[o for o in range(0,min(len(raw),max_off)-1,2) if struct.unpack_from("<H",raw,o)[0]==level]
    return u32,u16

def ptrs_in(raw, span):
    out=[]
    for o in range(0,min(len(raw),span)-3,4):
        v=struct.unpack_from("<I",raw,o)[0]
        if HEAP_LO<=v<HEAP_HI:
            out.append((o,v))
    return out

def main():
    if "--level" not in sys.argv:
        print("usage: python find_level_ptr.py --level 20"); return
    level=int(sys.argv[sys.argv.index("--level")+1])

    pid=find_pid()
    if not pid: print("DDO.exe not running"); return
    h=k32.OpenProcess(PQI|PVR, False, pid)
    if not h: print("OpenProcess failed (run elevated)"); return
    vtable=module_base(pid)+VTABLE_RVA

    actor=find_player_actor(h, vtable)
    if not actor: print("no player actor found"); return
    base,pa,ma=actor
    print(f"pid={pid} vtable={vtable:#x}")
    print(f"player actor base={base:#x} phys={pa} mag={ma}")

    obj=read(h, base, 0x1000)
    p1=ptrs_in(obj, 0x1000)
    print(f"depth-1 pointers in actor[0..0x1000]: {len(p1)}")

    seen=set([base])
    hits=[]
    for off1,t1 in p1:
        if t1 in seen: continue
        seen.add(t1)
        d1=read(h, t1, 0x2000)
        if not d1: continue
        u32,u16=find_level_in(d1, level, 0x2000)
        if u32 or u16:
            hits.append((1, f"actor+{off1:#x}", t1, u32, u16))
        # depth 2
        for off2,t2 in ptrs_in(d1, 0x800):
            if t2 in seen: continue
            seen.add(t2)
            d2=read(h, t2, 0x1000)
            if not d2: continue
            u32b,u16b=find_level_in(d2, level, 0x1000)
            if u32b or u16b:
                hits.append((2, f"actor+{off1:#x}->+{off2:#x}", t2, u32b, u16b))

    print(f"\ntargets containing level={level}: {len(hits)}")
    for depth,path,addr,u32,u16 in hits:
        print(f"  d{depth} {path} @ {addr:#x}  u32@{['%#x'%o for o in u32]} u16@{['%#x'%o for o in u16]}")
    k32.CloseHandle(h)

if __name__=="__main__": main()

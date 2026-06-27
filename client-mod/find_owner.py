"""
find_owner.py - find the object that owns the player's combat actor, by locating the actor now
and reverse-scanning for pointers to it. The owner/character object should hold Level/EXP.

Usage (elevated, in-game):
    python find_owner.py --level 20
Output also written to find_owner.log.
"""
import ctypes as C
from ctypes import wintypes as W
import sys, struct, os

_LOG = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "find_owner.log"), "w")
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
VTABLE_RVA=0x16f89d0; OFF_PHYS=0x64; OFF_MAG=0x68

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

def iter_regions(h):
    mbi=MBI(); addr=0x10000; limit=0x7fff0000
    while addr<limit:
        if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
        rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
        if (mbi.State==MEM_COMMIT and mbi.Type==MEM_PRIVATE
            and mbi.Protect not in (0,PAGE_NOACCESS) and not(mbi.Protect&PAGE_GUARD)):
            yield rb,rs
        addr=rb+rs

def find_player_actor(h, vtable):
    pat=struct.pack("<I",vtable); best=None; CHUNK=1<<20
    for rb,rs in iter_regions(h):
        pos=0
        while pos<rs:
            n=min(CHUNK,rs-pos); raw=read(h,rb+pos,n)
            s=0
            while raw:
                j=raw.find(pat,s)
                if j==-1: break
                s=j+4
                if j%4 or j+OFF_MAG+4>len(raw): continue
                pa=struct.unpack_from("<I",raw,j+OFF_PHYS)[0]; ma=struct.unpack_from("<I",raw,j+OFF_MAG)[0]
                if pa or ma:
                    if best is None or (pa+ma)>(best[1]+best[2]): best=(rb+pos+j,pa,ma)
            pos+=n
    return best

def find_refs(h, target):
    pat=struct.pack("<I",target); refs=[]; CHUNK=1<<20
    for rb,rs in iter_regions(h):
        pos=0
        while pos<rs:
            n=min(CHUNK,rs-pos); raw=read(h,rb+pos,n)
            s=0
            while raw:
                j=raw.find(pat,s)
                if j==-1: break
                s=j+1
                if j%4==0: refs.append(rb+pos+j)
            pos+=n
    return refs

def main():
    level=int(sys.argv[sys.argv.index("--level")+1]) if "--level" in sys.argv else 20
    pid=find_pid()
    if not pid: print("DDO.exe not running"); return
    h=k32.OpenProcess(PQI|PVR, False, pid)
    if not h: print("OpenProcess failed (run elevated)"); return
    vtable=module_base(pid)+VTABLE_RVA
    actor=find_player_actor(h, vtable)
    if not actor: print("no player actor"); return
    base,pa,ma=actor
    print(f"player actor base={base:#x} phys={pa} mag={ma}")

    refs=find_refs(h, base)
    print(f"referrers to actor: {len(refs)}")
    for r in refs:
        # The owner object usually starts a bit before the pointer field. Dump a window before/after.
        start=(r-0x80)&~0x3
        blk=read(h, start, 0x140)
        lv=[start+o for o in range(0,len(blk)-3,4) if struct.unpack_from("<I",blk,o)[0]==level]
        lv16=[start+o for o in range(0,len(blk)-1,2) if struct.unpack_from("<H",blk,o)[0]==level]
        tag=" <==has level!" if (lv or lv16) else ""
        print(f"  ref@{r:#x} (ptr field) level u32@{['%#x'%a for a in lv]} u16@{['%#x'%a for a in lv16]}{tag}")
    k32.CloseHandle(h)

if __name__=="__main__": main()

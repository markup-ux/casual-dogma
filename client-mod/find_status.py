"""
find_status.py - locate the player's character/status struct by anchoring on several unique
on-screen values at once and finding where they cluster together in memory.

We pass known values read off the status screen (Next-XP, HP, JP, job progress). Any of these
individually appears in many places, but the spot where 3+ of them sit within a small window is
almost certainly the status struct. We then report nearby small ints (level/job candidates).

Usage (elevated, in-game, values from the status screen):
    python find_status.py --vals 17188,652,9900,4012,12120 --level 20
Output also written to find_status.log next to this script.
"""
import ctypes as C
from ctypes import wintypes as W
import sys, struct, os

_LOG = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "find_status.log"), "w")
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
MEM_COMMIT=0x1000; PAGE_NOACCESS=0x01; PAGE_GUARD=0x100; MEM_PRIVATE=0x20000
k32=C.WinDLL("kernel32", use_last_error=True)

class PE32(C.Structure):
    _fields_=[("dwSize",W.DWORD),("cntUsage",W.DWORD),("th32ProcessID",W.DWORD),
              ("th32DefaultHeapID",C.c_void_p),("th32ModuleID",W.DWORD),("cntThreads",W.DWORD),
              ("th32ParentProcessID",W.DWORD),("pcPriClassBase",C.c_long),("dwFlags",W.DWORD),
              ("szExeFile",C.c_char*260)]
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

def scan_values(h, vals):
    """Return dict val -> [addresses] across committed private memory (u32 little-endian)."""
    pats={v:struct.pack("<I",v) for v in vals}
    found={v:[] for v in vals}
    mbi=MBI(); addr=0x10000; limit=0x7fff0000; CHUNK=1<<20
    while addr<limit:
        if k32.VirtualQueryEx(h,C.c_void_p(addr),C.byref(mbi),C.sizeof(mbi))==0: break
        rb=int(mbi.BaseAddress); rs=int(mbi.RegionSize)
        if (mbi.State==MEM_COMMIT and mbi.Type==MEM_PRIVATE
            and mbi.Protect not in (0,PAGE_NOACCESS) and not(mbi.Protect&PAGE_GUARD)):
            pos=0
            while pos<rs:
                n=min(CHUNK,rs-pos); buf=(C.c_char*n)(); got=C.c_size_t(0)
                if k32.ReadProcessMemory(h,C.c_void_p(rb+pos),buf,n,C.byref(got)) and got.value:
                    raw=buf.raw[:got.value]
                    for v,pat in pats.items():
                        s=0
                        while True:
                            j=raw.find(pat,s)
                            if j==-1: break
                            s=j+1
                            if j%4==0:
                                found[v].append(rb+pos+j)
                pos+=n
        addr=rb+rs
    return found

def read(h, addr, n):
    buf=(C.c_char*n)(); got=C.c_size_t(0)
    if k32.ReadProcessMemory(h,C.c_void_p(addr),buf,n,C.byref(got)) and got.value:
        return buf.raw[:got.value]
    return b""

def main():
    if "--vals" not in sys.argv:
        print("usage: python find_status.py --vals 17188,652,9900,4012,12120 --level 20"); return
    vals=[int(x) for x in sys.argv[sys.argv.index("--vals")+1].split(",") if x]
    level=int(sys.argv[sys.argv.index("--level")+1]) if "--level" in sys.argv else None

    pid=find_pid()
    if not pid: print("DDO.exe not running"); return
    h=k32.OpenProcess(PQI|PVR, False, pid)
    if not h: print("OpenProcess failed (run elevated)"); return
    print(f"pid={pid} scanning for {vals}")

    found=scan_values(h, vals)
    for v in vals:
        print(f"  value {v}: {len(found[v])} hits")

    # For low-frequency anchors, dump dword context around each hit to eyeball nearby fields.
    for v in vals:
        if 0 < len(found[v]) <= 12:
            print(f"\ncontext around each hit of {v}:")
            for a in found[v]:
                start=(a-0x40) & ~0x3
                blk=read(h, start, 0xA0)
                if not blk: continue
                print(f"  hit {a:#x}:")
                for o in range(0, len(blk)-3, 4):
                    dw=struct.unpack_from("<I",blk,o)[0]
                    addr=start+o
                    mark=" <==ANCHOR" if addr==a else (f" <==level({level})" if (level is not None and dw==level) else "")
                    print(f"    {addr:#x} (+{addr-a:#06x}) = {dw}{mark}")

    # Build one sorted list of (addr, value) and find windows where >=MIN distinct values appear.
    allhits=sorted((a,v) for v in vals for a in found[v])
    WINDOW=0x1000; MIN=3
    print(f"\nclusters (>= {MIN} distinct anchor values within {WINDOW:#x} bytes):")
    n=len(allhits); i=0; reported=set()
    while i<n:
        a0=allhits[i][0]; j=i; distinct={}
        while j<n and allhits[j][0]-a0<=WINDOW:
            distinct.setdefault(allhits[j][1], allhits[j][0]); j+=1
        if len(distinct)>=MIN:
            lo=min(distinct.values()); hi=max(distinct.values())
            key=lo & ~0xFFF
            if key not in reported:
                reported.add(key)
                print(f"\n  cluster near {lo:#x}..{hi:#x}:")
                for v,a in sorted(distinct.items(), key=lambda kv:kv[1]):
                    print(f"    {a:#x}  = {v}")
                # Dump small ints around the cluster to spot level/job (e.g. 20).
                start=(lo-0x200) & ~0x3
                blk=read(h, start, (hi-start)+0x200)
                if level is not None and blk:
                    locs=[start+o for o in range(0,len(blk)-3,4) if struct.unpack_from("<I",blk,o)[0]==level]
                    locs16=[start+o for o in range(0,len(blk)-1,2) if struct.unpack_from("<H",blk,o)[0]==level]
                    print(f"    level({level}) u32@{['%#x'%a for a in locs]} u16@{['%#x'%a for a in locs16]}")
        i+=1
    k32.CloseHandle(h)

if __name__=="__main__": main()

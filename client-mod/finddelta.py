"""
finddelta.py - find the player's current-EXP (and adjacent Level) fields by a before/after diff.

Workflow (elevated, in-game):
  1) Stand still (not gaining XP):   python finddelta.py --snapshot
  2) Kill ONE enemy; note the "<N> XP obtained" amount in chat.
  3) python finddelta.py --diff <N>     (and optionally --level 20)

The diff reports addresses whose value increased by exactly <N> (current/total EXP) and ones
that decreased by exactly <N> (XP-to-next). Level sits next to current-EXP; we dump context and
flag your level value.

Snapshot is saved to finddelta.snap next to this script; logs to finddelta.log.
"""
import ctypes as C
from ctypes import wintypes as W
import sys, struct, os, pickle

_HERE=os.path.dirname(os.path.abspath(__file__))
_LOG=open(os.path.join(_HERE,"finddelta.log"),"w")
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

SNAP=os.path.join(_HERE,"finddelta.snap")
PQI=0x0400; PVR=0x0010; TH32CS_SNAPPROCESS=0x2
MEM_COMMIT=0x1000; PAGE_NOACCESS=0x01; PAGE_GUARD=0x100; MEM_PRIVATE=0x20000
k32=C.WinDLL("kernel32", use_last_error=True)
LO=16; HI=5_000_000  # plausible EXP-field value range, to bound snapshot size

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

def build_map(h):
    m={}; CHUNK=1<<20
    for rb,rs in iter_regions(h):
        pos=0
        while pos<rs:
            n=min(CHUNK,rs-pos); raw=read(h,rb+pos,n)
            for o in range(0,len(raw)-3,4):
                v=struct.unpack_from("<I",raw,o)[0]
                if LO<=v<=HI:
                    m[rb+pos+o]=v
            pos+=n
    return m

def main():
    pid=find_pid()
    if not pid: print("DDO.exe not running"); return
    h=k32.OpenProcess(PQI|PVR, False, pid)
    if not h: print("OpenProcess failed (run elevated)"); return

    if "--read" in sys.argv:
        a=int(sys.argv[sys.argv.index("--read")+1],0)
        level=int(sys.argv[sys.argv.index("--level")+1]) if "--level" in sys.argv else 20
        start=(a-0x80)&~0x3; blk=read(h,start,0x140)
        print(f"context around {a:#x} (flag level={level}):")
        for o in range(0,len(blk)-3,4):
            dw=struct.unpack_from("<I",blk,o)[0]; addr=start+o
            u16=struct.unpack_from("<H",blk,o)[0]
            mark=" <==ADDR" if addr==a else (" <==level" if (dw==level or u16==level) else "")
            print(f"  {addr:#x} (+{addr-a:#06x}) u32={dw} u16={u16}{mark}")
        k32.CloseHandle(h); return

    if "--snapshot" in sys.argv:
        m=build_map(h)
        with open(SNAP,"wb") as f: pickle.dump({"pid":pid,"map":m}, f)
        print(f"snapshot: {len(m)} candidate dwords saved. Now gain a known XP amount, then --diff <N>.")
        k32.CloseHandle(h); return

    if "--diff" in sys.argv:
        g=int(sys.argv[sys.argv.index("--diff")+1])
        level=int(sys.argv[sys.argv.index("--level")+1]) if "--level" in sys.argv else None
        try:
            with open(SNAP,"rb") as f: snap=pickle.load(f)
        except Exception as e:
            print(f"no snapshot ({e}); run --snapshot first"); return
        if snap["pid"]!=pid:
            print(f"WARNING: pid changed ({snap['pid']} -> {pid}); addresses may be stale")
        old=snap["map"]
        inc=[]; dec=[]
        # Re-read regions in bulk, compare to old by address.
        CHUNK=1<<20
        for rb,rs in iter_regions(h):
            pos=0
            while pos<rs:
                n=min(CHUNK,rs-pos); raw=read(h,rb+pos,n)
                for o in range(0,len(raw)-3,4):
                    a=rb+pos+o
                    ov=old.get(a)
                    if ov is None: continue
                    nv=struct.unpack_from("<I",raw,o)[0]
                    d=nv-ov
                    if d==g: inc.append((a,ov,nv))
                    elif d==-g: dec.append((a,ov,nv))
                pos+=n
        print(f"diff for gain={g}: increased-by-{g}: {len(inc)}  decreased-by-{g}: {len(dec)}")

        # Candidate intersection across rounds (Cheat-Engine style "next scan").
        candfile=os.path.join(_HERE,"finddelta.cand")
        inc_set={a for a,_,_ in inc}; dec_set={a for a,_,_ in dec}
        if "--reset" not in sys.argv and os.path.exists(candfile):
            try:
                with open(candfile,"rb") as f: prev=pickle.load(f)
                inc_set &= set(prev.get("inc",[])); dec_set &= set(prev.get("dec",[]))
                print(f"after intersect with previous round(s): inc={len(inc_set)} dec={len(dec_set)}")
            except Exception as e:
                print(f"(could not load prior candidates: {e})")
        with open(candfile,"wb") as f: pickle.dump({"inc":list(inc_set),"dec":list(dec_set)}, f)

        cur={a:nv for a,_,nv in inc}; cur.update({a:nv for a,_,nv in dec})
        print("\nSURVIVING increased candidates (current/total EXP):")
        for a in sorted(inc_set)[:60]:
            print(f"  {a:#x}: now {cur.get(a)}")
        print("\nSURVIVING decreased candidates (XP-to-next):")
        for a in sorted(dec_set)[:60]:
            print(f"  {a:#x}: now {cur.get(a)}")
        # For the strongest candidates, dump context to find the adjacent Level field.
        cands=sorted(inc_set)+sorted(dec_set)
        if level is not None and cands:
            print(f"\ncontext around EXP candidates, flagging level={level}:")
            for a in cands[:20]:
                start=(a-0x40)&~0x3; blk=read(h,start,0xA0)
                lv=[start+o for o in range(0,len(blk)-3,4) if struct.unpack_from("<I",blk,o)[0]==level]
                lv16=[start+o for o in range(0,len(blk)-1,2) if struct.unpack_from("<H",blk,o)[0]==level]
                if lv or lv16:
                    print(f"  near {a:#x}: level u32@{['%#x'%x for x in lv]} u16@{['%#x'%x for x in lv16]} <==")
        k32.CloseHandle(h); return

    print("usage: --snapshot  |  --diff <gainedXP> [--level 20]")

if __name__=="__main__": main()

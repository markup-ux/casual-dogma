"""
disasm.py - read N bytes at a VA from the live DDO.exe and disassemble (x86-32, capstone).
Used to understand how a known function reads character fields (e.g. level) so we can find a
stable handle to the level value.

Usage (elevated): python disasm.py 0x5fd680 0x200
Output also to disasm.log.
"""
import ctypes as C
from ctypes import wintypes as W
import sys, os
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

_HERE=os.path.dirname(os.path.abspath(__file__))
_LOG=open(os.path.join(_HERE,"disasm.log"),"w")
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
k32=C.WinDLL("kernel32", use_last_error=True)
class PE32(C.Structure):
    _fields_=[("dwSize",W.DWORD),("cntUsage",W.DWORD),("th32ProcessID",W.DWORD),
              ("th32DefaultHeapID",C.c_void_p),("th32ModuleID",W.DWORD),("cntThreads",W.DWORD),
              ("th32ParentProcessID",W.DWORD),("pcPriClassBase",C.c_long),("dwFlags",W.DWORD),
              ("szExeFile",C.c_char*260)]
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

def main():
    va=int(sys.argv[1],0) if len(sys.argv)>1 else 0x5fd680
    n =int(sys.argv[2],0) if len(sys.argv)>2 else 0x200
    pid=find_pid()
    if not pid: print("DDO.exe not running"); return
    h=k32.OpenProcess(PQI|PVR, False, pid)
    if not h: print("OpenProcess failed (run elevated)"); return
    code=read(h, va, n)
    print(f"disasm {va:#x} ({len(code)} bytes)")
    md=Cs(CS_ARCH_X86, CS_MODE_32)
    for ins in md.disasm(code, va):
        note=""
        op=ins.mnemonic
        if op.startswith("movzx") or op in ("mov","cmp","movsx") :
            if "byte" in ins.op_str or "word ptr" in ins.op_str:
                note="   <-- small field load (level/exp candidate)"
        print(f"  {ins.address:#010x}: {ins.mnemonic:<7} {ins.op_str}{note}")
    k32.CloseHandle(h)

if __name__=="__main__": main()

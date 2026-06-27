"""Resolve MSVC RTTI class names for vtables in the unpacked image dump.

32-bit MSVC layout:
  vtable[-4] -> RTTICompleteObjectLocator (VA)
  COL+0x0c   -> TypeDescriptor (VA)
  TypeDescriptor+0x08 -> mangled name string (".?AV<Class>@@")
"""
import sys

IMG = r"D:\DDON\client-mod\ddon_image.bin"
BASE = 0x400000
with open(IMG, "rb") as f:
    D = f.read()


def u32(va):
    o = va - BASE
    if o < 0 or o + 4 > len(D):
        return None
    return int.from_bytes(D[o:o + 4], "little")


def cstr(va):
    o = va - BASE
    if o < 0 or o >= len(D):
        return None
    e = D.find(b"\x00", o)
    return D[o:e].decode("latin1", "replace")


def name_of(vtable):
    col = u32(vtable - 4)
    if not col:
        return None
    td = u32(col + 0x0c)
    if not td:
        return None
    return cstr(td + 8)


for a in sys.argv[1:]:
    v = int(a, 16)
    print(f"{v:#010x}  {name_of(v)}")

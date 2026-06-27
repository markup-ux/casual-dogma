//! Runtime function-call thunk.
//!
//! Builds a tiny trampoline in memory we allocate ourselves and invokes a game
//! function through it. We NEVER patch existing game code -- we only execute our
//! own freshly-allocated page -- so this stays clear of the code-integrity
//! anti-tamper that tripped on inline hooks and debug registers.
//!
//! Supports thiscall/stdcall/cdecl: `ecx` is loaded into ECX (0 = plain call),
//! `args` are pushed right-to-left, and `cleanup` bytes are added back to ESP
//! after the call (0 for callees that self-clean via `ret N`, i.e. stdcall /
//! thiscall; `args.len()*4` for cdecl).

use windows_sys::Win32::System::Memory::{
    VirtualAlloc, VirtualFree, MEM_COMMIT, MEM_RELEASE, MEM_RESERVE, PAGE_EXECUTE_READWRITE,
};

/// Invoke `func`. Returns EAX. `ecx` set into ECX before the call (0 = none).
///
/// # Safety
/// Calls into arbitrary game code with a caller-supplied signature; a wrong
/// address / arg count / convention will corrupt state or crash the process.
pub unsafe fn raw_call(func: u32, ecx: u32, args: &[u32], cleanup: u32) -> u32 {
    let mut code: Vec<u8> = Vec::with_capacity(16 + args.len() * 5);
    // push args, right-to-left so args[0] ends up at [esp] (first stack arg)
    for &a in args.iter().rev() {
        code.push(0x68); // push imm32
        code.extend_from_slice(&a.to_le_bytes());
    }
    code.push(0xB9); // mov ecx, imm32
    code.extend_from_slice(&ecx.to_le_bytes());
    code.push(0xB8); // mov eax, imm32 (func)
    code.extend_from_slice(&func.to_le_bytes());
    code.push(0xFF); // call eax
    code.push(0xD0);
    if cleanup != 0 {
        code.push(0x81); // add esp, imm32
        code.push(0xC4);
        code.extend_from_slice(&cleanup.to_le_bytes());
    }
    code.push(0xC3); // ret

    let mem = VirtualAlloc(
        core::ptr::null(),
        code.len(),
        MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE,
    );
    if mem.is_null() {
        return 0xDEAD_0001;
    }
    core::ptr::copy_nonoverlapping(code.as_ptr(), mem as *mut u8, code.len());
    let f: extern "C" fn() -> u32 = core::mem::transmute(mem);
    let r = f();
    VirtualFree(mem, 0, MEM_RELEASE);
    r
}

//! Crash diagnostics for the in-process mod.
//!
//! Arms three independent safety nets so that any failure is captured to the log
//! (and a minidump) instead of silently killing the game:
//!   1. Rust panic hook        - catches panics in our own code.
//!   2. Vectored handler (VEH) - first-chance; logs faults whose instruction pointer
//!                               lands inside OUR module (i.e. bugs in our hooks),
//!                               even if the game would otherwise swallow them.
//!   3. Unhandled filter (UEF) - last-chance; any real crash -> full report + minidump.
//!
//! All crash-path logging uses `log::raw_line` (no global lock) to avoid deadlocking
//! if the crash happened while the normal log lock was held.

use core::sync::atomic::{AtomicUsize, Ordering};
use std::os::windows::io::AsRawHandle;

use windows_sys::Win32::Foundation::{HANDLE, HMODULE};
use windows_sys::Win32::System::Diagnostics::Debug::{
    AddVectoredExceptionHandler, MiniDumpWriteDump, SetUnhandledExceptionFilter,
    EXCEPTION_POINTERS, MINIDUMP_EXCEPTION_INFORMATION,
};
use windows_sys::Win32::System::LibraryLoader::{GetModuleFileNameW, GetModuleHandleExW};
use windows_sys::Win32::System::Memory::{
    VirtualQuery, MEMORY_BASIC_INFORMATION, MEM_COMMIT, PAGE_EXECUTE_READ, PAGE_EXECUTE_READWRITE,
    PAGE_EXECUTE_WRITECOPY, PAGE_GUARD, PAGE_NOACCESS, PAGE_READONLY, PAGE_READWRITE, PAGE_WRITECOPY,
};
use windows_sys::Win32::System::SystemInformation::GetLocalTime;
use windows_sys::Win32::Foundation::SYSTEMTIME;
use windows_sys::Win32::System::Threading::{
    GetCurrentProcess, GetCurrentProcessId, GetCurrentThreadId,
};

use crate::log;

const EXCEPTION_CONTINUE_SEARCH: i32 = 0;
const EXCEPTION_EXECUTE_HANDLER: i32 = 1;

const STATUS_ACCESS_VIOLATION: u32 = 0xC000_0005;
const STATUS_ILLEGAL_INSTRUCTION: u32 = 0xC000_001D;
const STATUS_PRIV_INSTRUCTION: u32 = 0xC000_0096;
const STATUS_STACK_OVERFLOW: u32 = 0xC000_00FD;
const STATUS_INT_DIVIDE_BY_ZERO: u32 = 0xC000_0094;

const GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS: u32 = 0x0000_0004;
const GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT: u32 = 0x0000_0002;

// MINIDUMP_TYPE bits (kept local so we don't depend on the enum constants).
// IMPORTANT: richer flags (PrivateReadWriteMemory / FullMemory) make MiniDumpWriteDump
// FAIL here, because it walks the heap and the heap is corrupted at crash time. We keep
// the minimal set that actually succeeds; the real diagnostic value now comes from the
// in-process crash-context dump below (heap-corruption-proof targeted reads).
const MINIDUMP_WITH_HANDLE_DATA: i32 = 0x0000_0004;
const MINIDUMP_WITH_UNLOADED_MODULES: i32 = 0x0000_0020;
const MINIDUMP_WITH_INDIRECTLY_REFERENCED_MEMORY: i32 = 0x0000_0040;
const MINIDUMP_WITH_THREAD_INFO: i32 = 0x0000_1000;
const DUMP_FLAGS: i32 = MINIDUMP_WITH_HANDLE_DATA
    | MINIDUMP_WITH_UNLOADED_MODULES
    | MINIDUMP_WITH_INDIRECTLY_REFERENCED_MEMORY
    | MINIDUMP_WITH_THREAD_INFO;

// Address range of our own module, for the VEH first-chance filter.
static OUR_BASE: AtomicUsize = AtomicUsize::new(0);
static OUR_END: AtomicUsize = AtomicUsize::new(0);

/// Compact timestamp for filenames: YYYYMMDD_HHMMSS.
fn compact_ts() -> String {
    unsafe {
        let mut st: SYSTEMTIME = core::mem::zeroed();
        GetLocalTime(&mut st);
        format!(
            "{:04}{:02}{:02}_{:02}{:02}{:02}",
            st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond
        )
    }
}

/// Resolve an address to `module.dll+0xoffset` (best effort).
fn module_of(addr: usize) -> String {
    unsafe {
        let mut hmod: HMODULE = core::mem::zeroed();
        let ok = GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            addr as *const u16,
            &mut hmod,
        );
        if ok == 0 {
            return format!("{:#010x} <unknown module>", addr);
        }
        let base = hmod as usize;
        let mut buf = [0u16; 260];
        let n = GetModuleFileNameW(hmod, buf.as_mut_ptr(), buf.len() as u32) as usize;
        let full = String::from_utf16_lossy(&buf[..n]);
        let name = full.rsplit(['\\', '/']).next().unwrap_or(&full).to_string();
        format!("{}+{:#x} ({:#010x})", name, addr.wrapping_sub(base), addr)
    }
}

/// Record our own module's [base, end) so the VEH can tell our faults apart.
fn record_self_range() {
    unsafe {
        let mut hmod: HMODULE = core::mem::zeroed();
        let probe = record_self_range as usize; // any address inside this module
        let ok = GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            probe as *const u16,
            &mut hmod,
        );
        if ok == 0 {
            return;
        }
        let base = hmod as usize;
        // Parse SizeOfImage out of the PE header (PE32: NT + 0x50).
        let e_lfanew = core::ptr::read_unaligned((base + 0x3C) as *const u32) as usize;
        let nt = base + e_lfanew;
        let size_of_image = core::ptr::read_unaligned((nt + 0x50) as *const u32) as usize;
        OUR_BASE.store(base, Ordering::Relaxed);
        OUR_END.store(base + size_of_image, Ordering::Relaxed);
        log::line(&format!(
            "[diag] self module {:#010x}..{:#010x} (size {:#x})",
            base,
            base + size_of_image,
            size_of_image
        ));
    }
}

fn code_name(code: u32) -> &'static str {
    match code {
        STATUS_ACCESS_VIOLATION => "ACCESS_VIOLATION",
        STATUS_ILLEGAL_INSTRUCTION => "ILLEGAL_INSTRUCTION",
        STATUS_PRIV_INSTRUCTION => "PRIV_INSTRUCTION",
        STATUS_STACK_OVERFLOW => "STACK_OVERFLOW",
        STATUS_INT_DIVIDE_BY_ZERO => "INT_DIVIDE_BY_ZERO",
        _ => "EXCEPTION",
    }
}

/// Build a human report from the exception pointers. `full` adds register state.
unsafe fn describe(p: *mut EXCEPTION_POINTERS, full: bool) -> String {
    if p.is_null() {
        return "<null EXCEPTION_POINTERS>".to_string();
    }
    let rec = (*p).ExceptionRecord;
    if rec.is_null() {
        return "<null ExceptionRecord>".to_string();
    }
    let code = (*rec).ExceptionCode as u32;
    let at = (*rec).ExceptionAddress as usize;
    let mut s = format!(
        "code={:#010x} ({}) at {}",
        code,
        code_name(code),
        module_of(at)
    );

    if code == STATUS_ACCESS_VIOLATION && (*rec).NumberParameters >= 2 {
        let kind = match (*rec).ExceptionInformation[0] {
            0 => "read",
            1 => "write",
            8 => "execute",
            _ => "?",
        };
        let target = (*rec).ExceptionInformation[1];
        s.push_str(&format!(" | {} of {:#010x}", kind, target));
    }

    if full {
        let ctx = (*p).ContextRecord;
        if !ctx.is_null() {
            #[cfg(target_arch = "x86")]
            {
                let c = &*ctx;
                s.push_str(&format!(
                    "\n    eip={:#010x} esp={:#010x} ebp={:#010x}\n    eax={:#010x} ebx={:#010x} ecx={:#010x} edx={:#010x}\n    esi={:#010x} edi={:#010x} eflags={:#010x}",
                    c.Eip, c.Esp, c.Ebp, c.Eax, c.Ebx, c.Ecx, c.Edx, c.Esi, c.Edi, c.EFlags
                ));
                s.push_str(&format!("\n    eip -> {}", module_of(c.Eip as usize)));
            }
        }
    }
    s
}

/// Faulting instruction pointer, or 0 if unavailable.
unsafe fn fault_ip(p: *mut EXCEPTION_POINTERS) -> usize {
    if p.is_null() {
        return 0;
    }
    let ctx = (*p).ContextRecord;
    if ctx.is_null() {
        return 0;
    }
    #[cfg(target_arch = "x86")]
    {
        (*ctx).Eip as usize
    }
    #[cfg(not(target_arch = "x86"))]
    {
        0
    }
}

fn write_minidump(p: *mut EXCEPTION_POINTERS) -> Option<String> {
    unsafe {
        let pid = GetCurrentProcessId();
        let path = format!("C:\\Users\\Public\\ddon_mod_crash_{}_{}.dmp", pid, compact_ts());
        let file = std::fs::File::create(&path).ok()?;
        let h = file.as_raw_handle() as HANDLE;
        let mei = MINIDUMP_EXCEPTION_INFORMATION {
            ThreadId: GetCurrentThreadId(),
            ExceptionPointers: p,
            ClientPointers: 0,
        };
        let ok = MiniDumpWriteDump(
            GetCurrentProcess(),
            pid,
            h,
            DUMP_FLAGS,
            &mei as *const MINIDUMP_EXCEPTION_INFORMATION,
            core::ptr::null(),
            core::ptr::null(),
        );
        drop(file);
        if ok != 0 {
            Some(path)
        } else {
            None
        }
    }
}

/// True if `addr` is in a committed, readable page (guard pages excluded).
unsafe fn addr_readable(addr: usize) -> bool {
    if addr < 0x1000 {
        return false;
    }
    let mut mbi: MEMORY_BASIC_INFORMATION = core::mem::zeroed();
    let n = VirtualQuery(
        addr as *const _,
        &mut mbi,
        core::mem::size_of::<MEMORY_BASIC_INFORMATION>(),
    );
    if n == 0 || mbi.State != MEM_COMMIT {
        return false;
    }
    let p = mbi.Protect;
    if p & PAGE_GUARD != 0 || p & PAGE_NOACCESS != 0 {
        return false;
    }
    let readable = PAGE_READONLY | PAGE_READWRITE | PAGE_WRITECOPY | PAGE_EXECUTE_READ
        | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY;
    p & readable != 0
}

/// Read a dword only if the whole 4 bytes are readable.
unsafe fn read_u32_safe(addr: usize) -> Option<u32> {
    if addr_readable(addr) && addr_readable(addr + 3) {
        Some(core::ptr::read_unaligned(addr as *const u32))
    } else {
        None
    }
}

/// Hex+ascii dump of up to `len` bytes at `addr`, skipping unreadable spans.
unsafe fn dump_mem(label: &str, addr: usize, len: usize) {
    log::raw_line(&format!("[CRASH] {} @ {:#010x}:", label, addr));
    let mut off = 0usize;
    while off < len {
        let row = addr + off;
        let mut hexs = String::new();
        let mut asc = String::new();
        let mut any = false;
        for i in 0..16 {
            if addr_readable(row + i) {
                let b = core::ptr::read_unaligned((row + i) as *const u8);
                hexs.push_str(&format!("{:02x} ", b));
                asc.push(if (0x20..0x7f).contains(&b) { b as char } else { '.' });
                any = true;
            } else {
                hexs.push_str("?? ");
                asc.push('.');
            }
        }
        if any {
            log::raw_line(&format!("[CRASH]   {:#010x}: {}| {}", row, hexs, asc));
        } else {
            log::raw_line(&format!("[CRASH]   {:#010x}: <unreadable>", row));
        }
        off += 16;
    }
}

/// Heap-corruption-proof crash context: faulting object, its vtable/type, the corrupted
/// field, and a stack scan for return addresses (the call chain).
unsafe fn crash_context(p: *mut EXCEPTION_POINTERS) {
    if p.is_null() {
        return;
    }
    let ctx = (*p).ContextRecord;
    if ctx.is_null() {
        return;
    }
    #[cfg(target_arch = "x86")]
    {
        let c = &*ctx;
        let ecx = c.Ecx as usize;
        let esp = c.Esp as usize;
        let ebp = c.Ebp as usize;

        // The faulting object (ecx) and the corrupted array base at [ecx+0x40].
        if addr_readable(ecx) {
            if let Some(vt) = read_u32_safe(ecx) {
                log::raw_line(&format!("[CRASH] obj ecx={:#010x} vtable={}", ecx, module_of(vt as usize)));
                if let Some(first) = read_u32_safe(vt as usize) {
                    log::raw_line(&format!("[CRASH]   vtable[0] -> {}", module_of(first as usize)));
                }
            }
            match read_u32_safe(ecx + 0x40) {
                Some(v) => log::raw_line(&format!("[CRASH]   [ecx+0x40] (array base) = {:#010x}", v)),
                None => log::raw_line("[CRASH]   [ecx+0x40] unreadable"),
            }
            dump_mem("obj ecx bytes", ecx, 0x80);
        } else {
            log::raw_line(&format!("[CRASH] obj ecx={:#010x} <unreadable>", ecx));
        }

        // Caller index argument (the function does mov eax,[ebp+8]).
        if let Some(arg) = read_u32_safe(ebp + 8) {
            log::raw_line(&format!("[CRASH] arg [ebp+8] (index) = {:#010x}", arg));
        }

        // Stack scan: dwords that point into DDO.exe = return-address / call-chain candidates.
        log::raw_line("[CRASH] stack call-chain candidates (DDO.exe):");
        let (lo, hi) = ddo_range();
        let mut shown = 0;
        let mut a = esp;
        while a < esp + 0x300 && shown < 24 {
            if let Some(v) = read_u32_safe(a) {
                let v = v as usize;
                if v >= lo && v < hi {
                    log::raw_line(&format!("[CRASH]   [esp+{:#05x}] -> {}", a - esp, module_of(v)));
                    shown += 1;
                }
            }
            a += 4;
        }
    }
}

/// [base, base+SizeOfImage) of DDO.exe (the main module).
fn ddo_range() -> (usize, usize) {
    unsafe {
        let base = windows_sys::Win32::System::LibraryLoader::GetModuleHandleW(core::ptr::null())
            as usize;
        if base == 0 {
            return (0, 0);
        }
        let e_lfanew = core::ptr::read_unaligned((base + 0x3C) as *const u32) as usize;
        let nt = base + e_lfanew;
        let size = core::ptr::read_unaligned((nt + 0x50) as *const u32) as usize;
        (base, base + size)
    }
}

/// First-chance handler. Only reports faults inside our own module to stay quiet.
unsafe extern "system" fn veh(p: *mut EXCEPTION_POINTERS) -> i32 {
    let ip = fault_ip(p);
    let base = OUR_BASE.load(Ordering::Relaxed);
    let end = OUR_END.load(Ordering::Relaxed);
    if base != 0 && ip >= base && ip < end {
        log::raw_line(&format!("[CRASH][first-chance in ddon_mod] {}", describe(p, true)));
    }
    EXCEPTION_CONTINUE_SEARCH
}

/// Last-chance handler: a genuine unhandled crash. Full report + minidump, then die.
unsafe extern "system" fn uef(p: *const EXCEPTION_POINTERS) -> i32 {
    let p = p as *mut EXCEPTION_POINTERS;
    log::raw_line("[CRASH] ===== UNHANDLED EXCEPTION =====");
    log::raw_line(&format!("[CRASH] {}", describe(p, true)));
    crash_context(p);
    match write_minidump(p) {
        Some(path) => log::raw_line(&format!("[CRASH] minidump written: {}", path)),
        None => log::raw_line("[CRASH] minidump FAILED"),
    }
    log::raw_line("[CRASH] ================================");
    EXCEPTION_EXECUTE_HANDLER
}

fn install_panic_hook() {
    std::panic::set_hook(Box::new(|info| {
        let loc = info
            .location()
            .map(|l| format!("{}:{}:{}", l.file(), l.line(), l.column()))
            .unwrap_or_else(|| "<unknown>".to_string());
        let msg = if let Some(s) = info.payload().downcast_ref::<&str>() {
            (*s).to_string()
        } else if let Some(s) = info.payload().downcast_ref::<String>() {
            s.clone()
        } else {
            "<non-string panic payload>".to_string()
        };
        log::raw_line(&format!("[PANIC] at {} : {}", loc, msg));
    }));
}

/// Arm all crash diagnostics. Safe to call once at startup.
pub fn arm() {
    record_self_range();
    install_panic_hook();
    unsafe {
        AddVectoredExceptionHandler(1, Some(veh));
        SetUnhandledExceptionFilter(Some(uef));
    }
    log::line("[diag] crash diagnostics armed (panic hook + VEH + unhandled filter)");
}

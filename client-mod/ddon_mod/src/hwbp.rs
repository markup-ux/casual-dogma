//! Hardware (debug-register) data breakpoints for discovery RE.
//!
//! Why this exists: to find the *code* that touches a value (e.g. the function that
//! decrements MP / reads a palette slot when you press a skill) without patching any
//! game code. Patching the game's hot swap path is exactly what crashed it; debug
//! registers watch data with zero code modification.
//!
//! Mechanism:
//!   * DR0 = watched address, DR7 enables a local data breakpoint (len 1/2/4, write or
//!     read-write), set on every thread of the process via SetThreadContext.
//!   * A vectored handler catches the resulting STATUS_SINGLE_STEP, logs the faulting
//!     EIP (resolved to DDO.exe+off) plus all registers = the accessing instruction and
//!     its operands, then resumes. After `maxhits` it self-disarms.
//!
//! Anti-debug caveat: a packed target *can* detect/clear DR registers. If so, the BP
//! simply never fires (no crash). We validate on the HP field first to confirm it works.

use core::sync::atomic::{AtomicBool, AtomicU32, AtomicUsize, Ordering};

use windows_sys::Win32::Foundation::{CloseHandle, FALSE, FILETIME, INVALID_HANDLE_VALUE};
use windows_sys::Win32::System::Diagnostics::Debug::{
    AddVectoredExceptionHandler, GetThreadContext, SetThreadContext, CONTEXT, EXCEPTION_POINTERS,
};
use windows_sys::Win32::System::Diagnostics::ToolHelp::{
    CreateToolhelp32Snapshot, Thread32First, Thread32Next, TH32CS_SNAPTHREAD, THREADENTRY32,
};
use windows_sys::Win32::System::Threading::{
    GetCurrentProcessId, GetCurrentThreadId, GetThreadTimes, OpenThread, ResumeThread,
    SuspendThread, THREAD_GET_CONTEXT, THREAD_QUERY_INFORMATION, THREAD_SET_CONTEXT,
    THREAD_SUSPEND_RESUME,
};

use crate::log;

const STATUS_SINGLE_STEP: u32 = 0x8000_0004;
const EXCEPTION_CONTINUE_SEARCH: i32 = 0;
const EXCEPTION_CONTINUE_EXECUTION: i32 = -1;
// CONTEXT_i386 (0x10000) | CONTEXT_DEBUG_REGISTERS (0x10).
const CONTEXT_DEBUG_REGISTERS_X86: u32 = 0x0001_0010;
const IMAGE_BASE: usize = 0x0040_0000;

static VEH_INSTALLED: AtomicBool = AtomicBool::new(false);
static ARMED: AtomicBool = AtomicBool::new(false);
static DISARMING: AtomicBool = AtomicBool::new(false);
static HITS: AtomicU32 = AtomicU32::new(0);
static MAXHITS: AtomicU32 = AtomicU32::new(0);
static WATCH_ADDR: AtomicUsize = AtomicUsize::new(0);
static ARMED_TID: AtomicU32 = AtomicU32::new(0);

/// Build a DR7 value enabling DR0 as a local data breakpoint.
/// rw: 0b01 = write, 0b11 = read/write. len: 1, 2, or 4 bytes.
fn dr7_for(rw: u8, len: u8) -> u32 {
    let len_bits: u32 = match len {
        1 => 0b00,
        2 => 0b01,
        _ => 0b11, // 4
    };
    let rw_bits: u32 = if rw == 1 { 0b01 } else { 0b11 };
    // bit0 = L0 ; bits16-17 = R/W0 ; bits18-19 = LEN0
    1 | (rw_bits << 16) | (len_bits << 18)
}

fn filetime_u64(ft: &FILETIME) -> u64 {
    ((ft.dwHighDateTime as u64) << 32) | (ft.dwLowDateTime as u64)
}

/// Enumerate this process's thread IDs paired with their creation time (FILETIME ticks).
unsafe fn enum_threads() -> Vec<(u32, u64)> {
    let mut out: Vec<(u32, u64)> = Vec::new();
    let pid = GetCurrentProcessId();
    let snap = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
    if snap.is_null() || snap == INVALID_HANDLE_VALUE {
        return out;
    }
    let mut te: THREADENTRY32 = core::mem::zeroed();
    te.dwSize = core::mem::size_of::<THREADENTRY32>() as u32;
    if Thread32First(snap, &mut te) != FALSE {
        loop {
            if te.th32OwnerProcessID == pid {
                let tid = te.th32ThreadID;
                let mut created: u64 = u64::MAX;
                let h = OpenThread(THREAD_QUERY_INFORMATION, FALSE, tid);
                if !h.is_null() {
                    let mut ct: FILETIME = core::mem::zeroed();
                    let mut et: FILETIME = core::mem::zeroed();
                    let mut kt: FILETIME = core::mem::zeroed();
                    let mut ut: FILETIME = core::mem::zeroed();
                    if GetThreadTimes(h, &mut ct, &mut et, &mut kt, &mut ut) != FALSE {
                        created = filetime_u64(&ct);
                    }
                    CloseHandle(h);
                }
                out.push((tid, created));
            }
            if Thread32Next(snap, &mut te) == FALSE {
                break;
            }
        }
    }
    CloseHandle(snap);
    out
}

/// The main game thread = the earliest-created thread in the process.
pub unsafe fn main_thread_id() -> u32 {
    enum_threads()
        .into_iter()
        .filter(|&(_, c)| c != u64::MAX)
        .min_by_key(|&(_, c)| c)
        .map(|(tid, _)| tid)
        .unwrap_or(0)
}

/// Log a roster of threads (creation order) for picking a target thread.
pub fn list_threads() {
    unsafe {
        let mut v = enum_threads();
        v.sort_by_key(|&(_, c)| c);
        let main = v.first().map(|&(t, _)| t).unwrap_or(0);
        log::line(&format!("[BP] {} thread(s) (earliest first; main={:#x}):", v.len(), main));
        for (i, (tid, _)) in v.iter().enumerate().take(12) {
            let tag = if *tid == main { " <- main" } else { "" };
            log::line(&format!("[BP]   #{:<2} tid={:#x}{}", i, tid, tag));
        }
    }
}

/// Apply `dr0`/`dr7` to a SINGLE thread (suspend, set context, resume). This is the safe
/// debugger pattern: touching one known thread instead of rewriting the context of all ~63
/// threads (the all-thread sweep was corrupting worker contexts and crashing the game).
unsafe fn set_dr_one(tid: u32, dr0: usize, dr7: u32) -> bool {
    if tid == 0 {
        return false;
    }
    let h = OpenThread(
        THREAD_GET_CONTEXT | THREAD_SET_CONTEXT | THREAD_SUSPEND_RESUME,
        FALSE,
        tid,
    );
    if h.is_null() {
        return false;
    }
    let mut ok = false;
    SuspendThread(h);
    let mut ctx: CONTEXT = core::mem::zeroed();
    ctx.ContextFlags = CONTEXT_DEBUG_REGISTERS_X86;
    if GetThreadContext(h, &mut ctx) != FALSE {
        ctx.ContextFlags = CONTEXT_DEBUG_REGISTERS_X86;
        ctx.Dr0 = dr0 as u32;
        ctx.Dr7 = dr7;
        ctx.Dr6 = 0;
        ok = SetThreadContext(h, &ctx) != FALSE;
    }
    ResumeThread(h);
    CloseHandle(h);
    ok
}

unsafe extern "system" fn bp_veh(p: *mut EXCEPTION_POINTERS) -> i32 {
    if p.is_null() {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    let rec = (*p).ExceptionRecord;
    if rec.is_null() || (*rec).ExceptionCode as u32 != STATUS_SINGLE_STEP {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    let ctx = (*p).ContextRecord;
    if ctx.is_null() {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    let c = &mut *ctx;
    // Only act on a real DR0 data breakpoint (Dr6 bit 0). Anything else isn't ours.
    if c.Dr6 & 0x1 == 0 {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    // From here on this single-step is OURS: we must ALWAYS handle it (clear + continue),
    // never fall through, or it becomes an unhandled exception and kills the game.
    c.Dr6 = 0;
    // If we're disarmed/disarming, just self-clear this thread's DR and resume.
    if DISARMING.load(Ordering::Relaxed) || !ARMED.load(Ordering::Relaxed) {
        c.Dr0 = 0;
        c.Dr7 = 0;
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    let n = HITS.fetch_add(1, Ordering::Relaxed) + 1;
    let eip = c.Eip as usize;
    let off = eip.wrapping_sub(IMAGE_BASE);
    log::raw_line(&format!(
        "[BP] hit#{} @ DDO.exe+{:#x} (eip={:#010x}) watch={:#010x}",
        n,
        off,
        eip,
        WATCH_ADDR.load(Ordering::Relaxed)
    ));
    log::raw_line(&format!(
        "[BP]    eax={:#010x} ebx={:#010x} ecx={:#010x} edx={:#010x}",
        c.Eax, c.Ebx, c.Ecx, c.Edx
    ));
    log::raw_line(&format!(
        "[BP]    esi={:#010x} edi={:#010x} esp={:#010x} ebp={:#010x}",
        c.Esi, c.Edi, c.Esp, c.Ebp
    ));
    if n >= MAXHITS.load(Ordering::Relaxed) {
        // Self-clear this thread and flip DISARMING so every other thread self-clears on
        // its next hit. A proactive sweep ('bpoff') is optional, not required for safety.
        c.Dr0 = 0;
        c.Dr7 = 0;
        DISARMING.store(true, Ordering::Relaxed);
        ARMED.store(false, Ordering::Relaxed);
        log::raw_line("[BP] reached maxhits -> auto-disarming (self-clearing per thread)");
    }
    EXCEPTION_CONTINUE_EXECUTION
}

/// Arm a data breakpoint on a SINGLE thread. rw: 1=write, 3=read/write. len: 1/2/4.
/// `tid` 0 means "the main game thread". Returns the thread id armed, or 0 on failure.
pub fn arm(addr: usize, rw: u8, len: u8, maxhits: u32, tid: u32) -> u32 {
    unsafe {
        if !VEH_INSTALLED.swap(true, Ordering::Relaxed) {
            // First handler so we see the single-step before anyone else.
            AddVectoredExceptionHandler(1, Some(bp_veh));
        }
        let target = if tid != 0 { tid } else { main_thread_id() };
        if target == 0 {
            log::line("[BP] arm FAILED: could not resolve target thread");
            return 0;
        }
        if target == GetCurrentThreadId() {
            log::line("[BP] arm refused: target is the probe thread");
            return 0;
        }
        HITS.store(0, Ordering::Relaxed);
        MAXHITS.store(maxhits.max(1), Ordering::Relaxed);
        WATCH_ADDR.store(addr, Ordering::Relaxed);
        ARMED_TID.store(target, Ordering::Relaxed);
        DISARMING.store(false, Ordering::Relaxed);
        ARMED.store(true, Ordering::Relaxed);
        let dr7 = dr7_for(rw, len);
        if set_dr_one(target, addr, dr7) {
            log::line(&format!(
                "[BP] armed watch={:#010x} rw={} len={} maxhits={} on tid={:#x}",
                addr, rw, len, maxhits, target
            ));
            target
        } else {
            ARMED.store(false, Ordering::Relaxed);
            log::line(&format!("[BP] arm FAILED on tid={:#x}", target));
            0
        }
    }
}

/// Disarm by clearing the single armed thread's debug registers (one suspend = safe), and
/// flip the flags so the handler also self-clears if it fires once more in the meantime.
pub fn disarm() {
    DISARMING.store(true, Ordering::Relaxed);
    ARMED.store(false, Ordering::Relaxed);
    let tid = ARMED_TID.swap(0, Ordering::Relaxed);
    if tid != 0 {
        unsafe {
            let _ = set_dr_one(tid, 0, 0);
        }
    }
    log::line(&format!("[BP] disarmed (cleared tid={:#x})", tid));
}

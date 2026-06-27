//! Guard-page (PAGE_GUARD) software data breakpoint for discovery RE.
//!
//! Why: hardware breakpoints (DR0-7) and code hooks both trip this game's anti-tamper and
//! crash it. A guard page uses neither -- we just flip the normal memory protection of the
//! page holding a target object to add PAGE_GUARD. The next time the game's OWN code touches
//! that page, the CPU raises STATUS_GUARD_PAGE_VIOLATION; our vectored handler reads the
//! faulting instruction's registers (which reveal the accessing code + nearby object pointers)
//! and then lets execution continue (the OS already cleared the guard, so it's naturally
//! one-shot). No debug registers, no patched bytes.
//!
//! Safety: Windows also uses PAGE_GUARD for automatic thread-stack growth. We ONLY handle a
//! fault whose data address lies inside the single page we armed; everything else (stack
//! growth, etc.) is passed through untouched (EXCEPTION_CONTINUE_SEARCH).

use core::ffi::c_void;
use core::sync::atomic::{AtomicBool, AtomicU32, AtomicUsize, Ordering};

use windows_sys::Win32::System::Diagnostics::Debug::{
    AddVectoredExceptionHandler, ReadProcessMemory, WriteProcessMemory, EXCEPTION_POINTERS,
};
use windows_sys::Win32::System::Threading::GetCurrentProcess;
use windows_sys::Win32::System::Memory::{
    VirtualProtect, VirtualQuery, MEMORY_BASIC_INFORMATION, PAGE_GUARD,
};

use crate::log;

const STATUS_GUARD_PAGE_VIOLATION: u32 = 0x8000_0001;
const EXCEPTION_CONTINUE_SEARCH: i32 = 0;
const EXCEPTION_CONTINUE_EXECUTION: i32 = -1;
const IMAGE_BASE: usize = 0x0040_0000;
const PAGE: usize = 0x1000;

static VEH_INSTALLED: AtomicBool = AtomicBool::new(false);
static ARMED: AtomicBool = AtomicBool::new(false);
static PAGE_BASE: AtomicUsize = AtomicUsize::new(0);
static ORIG_PROTECT: AtomicU32 = AtomicU32::new(0);
static HITS: AtomicU32 = AtomicU32::new(0);
static MAXHITS: AtomicU32 = AtomicU32::new(0);
static RAW_HITS: AtomicU32 = AtomicU32::new(0);
// Safety ceiling on total faults per arming, in case the watchdog can't run. High enough to span
// a multi-second capture window on a hot page.
const RAW_CAP: u32 = 2_000_000;
// In catch mode, abort if this many faults accrue before the target is hit (hot-page protection).
const CATCH_STORM_CAP: u32 = 8_000;
// Normal data-guard: abort if this many faults accrue (hot page read every frame + re-arm wedged
// the game for 8-12s). Keep low; distinct call sites are capped by SEEN_MAX anyway.
const DATA_STORM_CAP: u32 = 64;
// Capture epoch: each arm bumps this so a stale watchdog thread can't disarm a newer arming.
static EPOCH: AtomicU32 = AtomicU32::new(0);
// De-dup distinct accessor EIPs so one hot loop doesn't eat all the slots; we want VARIETY of
// call sites to reveal the ownership chain. Plain array + count, written only inside the VEH.
const SEEN_MAX: usize = 16;
static mut SEEN: [usize; SEEN_MAX] = [0; SEEN_MAX];
static SEEN_N: AtomicU32 = AtomicU32::new(0);

// One-shot "catch function entry" mode. When TARGET_EIP != 0 we watch a specific code address:
// a guard fault whose Eip == TARGET_EIP captures registers + stack args (and, in fire mode,
// rewrites the args) then disarms. Any OTHER fault on the page just flags NEED_REARM; a background
// poller re-applies the guard off-thread (no single-step -> no cross-thread trap-flag races).
static TARGET_EIP: AtomicUsize = AtomicUsize::new(0);
static NEED_REARM: AtomicBool = AtomicBool::new(false);
// Optional filter for catch mode: only capture when stack arg1 (the command id @ esp+8) low word
// is >= this. Lets us skip the per-frame idle command (0x64) and grab a real skill cast.
static CATCH_MINCMD: AtomicU32 = AtomicU32::new(0);
// Optional filter for catch mode: only capture when the actor's entity id ([ecx+0x10]) low 24 bits
// == this. Lets us ignore every NPC and grab ONLY the local player's execCommand calls.
static CATCH_ENTITY: AtomicU32 = AtomicU32::new(0);
// Fire mode: when FIRE_CMD != 0, on a target hit we OVERWRITE the call's args (slot, cmd, force=1)
// so this in-flight execCommand fires our chosen skill -- on the game's own thread, no patching.
static FIRE_CMD: AtomicU32 = AtomicU32::new(0);
static FIRE_SLOT: AtomicU32 = AtomicU32::new(0);
// When non-zero, rewrite ECX (thiscall actor) on the hijacked execCommand call.
static FIRE_ACTOR: AtomicU32 = AtomicU32::new(0);
// One-shot cast delivery: fire pending skill on the next read of the armed global (game thread).
static CAST_DELIVERY: AtomicBool = AtomicBool::new(false);

/// Fault-proof read: ReadProcessMemory on our own process returns FALSE for unmapped addresses
/// instead of raising an access violation. Critical inside the VEH -- a nested fault there crashes
/// the game. Returns true only if the whole buffer was read.
unsafe fn rd_safe(addr: usize, out: &mut [u8]) -> bool {
    if addr == 0 {
        return false;
    }
    let mut got = 0usize;
    ReadProcessMemory(
        GetCurrentProcess(),
        addr as *const c_void,
        out.as_mut_ptr() as *mut c_void,
        out.len(),
        &mut got,
    ) != 0
        && got == out.len()
}

/// Fault-proof write via WriteProcessMemory on our own process (won't raise inside the VEH).
unsafe fn wr_safe(addr: usize, data: &[u8]) -> bool {
    if addr == 0 {
        return false;
    }
    let mut put = 0usize;
    WriteProcessMemory(
        GetCurrentProcess(),
        addr as *mut c_void,
        data.as_ptr() as *const c_void,
        data.len(),
        &mut put,
    ) != 0
        && put == data.len()
}

/// Does a real `call` instruction end exactly at `v`? Covers the common x86 encodings so we can
/// tell true return addresses from function pointers that merely happen to sit on the stack.
/// All reads are fault-proof (the bytes before `v` may be in an unmapped inter-section gap).
unsafe fn is_call_return(v: usize) -> bool {
    let mut b = [0u8; 6]; // b[j] = byte at (v-6+j): v-6,v-5,v-4,v-3,v-2,v-1
    if !rd_safe(v.wrapping_sub(6), &mut b) {
        return false;
    }
    // E8 rel32 (direct call): opcode at v-5
    if b[1] == 0xE8 {
        return true;
    }
    // 2-byte FF /2: FF D0..D7 (call reg) / FF 10..17 (call [reg]); opcode at v-2
    if b[4] == 0xFF && ((0xD0..=0xD7).contains(&b[5]) || (0x10..=0x17).contains(&b[5])) {
        return true;
    }
    // 3-byte FF 50..57 disp8 (call [reg+d8]); opcode at v-3
    if b[3] == 0xFF && (0x50..=0x57).contains(&b[4]) {
        return true;
    }
    // 6-byte FF 90..97 disp32 (call [reg+d32]) / FF 15 disp32 (call [abs]); opcode at v-6
    if b[0] == 0xFF && ((0x90..=0x97).contains(&b[1]) || b[1] == 0x15) {
        return true;
    }
    false
}

fn module_tag(addr: usize) -> &'static str {
    if (IMAGE_BASE..0x0200_0000).contains(&addr) {
        "DDO"
    } else {
        "?"
    }
}

unsafe extern "system" fn gp_veh(p: *mut EXCEPTION_POINTERS) -> i32 {
    if p.is_null() {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    let rec = (*p).ExceptionRecord;
    if rec.is_null() {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    let code = (*rec).ExceptionCode as u32;
    if code != STATUS_GUARD_PAGE_VIOLATION {
        return EXCEPTION_CONTINUE_SEARCH;
    }
    // ExceptionInformation[0]=access(0 read,1 write,8 exec), [1]=faulting data address.
    let access = (*rec).ExceptionInformation[0] as usize;
    let fault = (*rec).ExceptionInformation[1] as usize;
    let page = PAGE_BASE.load(Ordering::Relaxed);
    let in_page = page != 0 && fault >= page && fault < page + PAGE;
    if !in_page {
        // Not our guarded page (e.g. a real stack-growth guard) -> let the OS handle it.
        return EXCEPTION_CONTINUE_SEARCH;
    }
    // It IS our page. We must ALWAYS absorb it (continue execution) -- declining would make an
    // in-flight fault during disarm go unhandled and crash the game. If we're not actively armed
    // (e.g. a fault raced past gpoff), just continue without logging or re-arming.
    if !ARMED.load(Ordering::Relaxed) {
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    // Hot-loaded stale DLL copies must not touch PAGE_GUARD or drain casts.
    if !crate::probe::is_latest() {
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    // Cast delivery: run IssuePlayerCommand on the faulting game thread, then disarm silently.
    if CAST_DELIVERY.load(Ordering::Relaxed) {
        CAST_DELIVERY.store(false, Ordering::Relaxed);
        ARMED.store(false, Ordering::Relaxed);
        let prot = ORIG_PROTECT.load(Ordering::Relaxed);
        let mut old = 0u32;
        VirtualProtect(page as *mut _, PAGE, prot, &mut old);
        if crate::skill::has_pending_cast() {
            crate::skill::drain_pending();
            log::raw_line("[GP] cast delivery: IssuePlayerCommand fired");
        }
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    // Bound total faults so a hot loop can't storm forever.
    let raw = RAW_HITS.fetch_add(1, Ordering::Relaxed) + 1;
    if raw > RAW_CAP {
        ARMED.store(false, Ordering::Relaxed);
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    let ctx = (*p).ContextRecord;
    if ctx.is_null() {
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    // One-shot catch mode: watching a specific code address (e.g. execCommand entry).
    let target = TARGET_EIP.load(Ordering::Relaxed);
    // Data-guard on a page the game hits every frame (actor inner loop, HUD, etc.) used to
    // re-arm after every fault -> multi-second freeze. Abort quickly; fix below stops re-arming
    // on duplicate EIPs so this is mostly a backstop.
    if target == 0 && raw > DATA_STORM_CAP {
        ARMED.store(false, Ordering::Relaxed);
        let pg = PAGE_BASE.load(Ordering::Relaxed);
        if pg != 0 {
            let prot = ORIG_PROTECT.load(Ordering::Relaxed);
            let mut old = 0u32;
            VirtualProtect(pg as *mut _, PAGE, prot, &mut old);
        }
        log::raw_line("[GP] data ABORTED: page too hot (fault storm) -- disarmed");
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    if target != 0 {
        let eip = (*ctx).Eip as usize;
        let esp0 = (*ctx).Esp as usize;
        let mut cmd_ok = true;
        let mincmd = CATCH_MINCMD.load(Ordering::Relaxed);
        if eip == target && mincmd != 0 {
            let mut cb = [0u8; 4];
            if rd_safe(esp0 + 8, &mut cb) {
                cmd_ok = (u32::from_le_bytes(cb) & 0xffff) >= mincmd;
            }
        }
        let want_ent = CATCH_ENTITY.load(Ordering::Relaxed);
        if eip == target && cmd_ok && want_ent != 0 {
            let mut eb = [0u8; 4];
            let ecx = (*ctx).Ecx as usize;
            cmd_ok = rd_safe(ecx + 0x10, &mut eb)
                && (u32::from_le_bytes(eb) & 0x00ff_ffff) == want_ent;
        }
        if eip == target && cmd_ok {
            let esp = (*ctx).Esp as usize;
            let mut args = [0u32; 8];
            let mut k = 0;
            while k < 8 {
                let mut b = [0u8; 4];
                if rd_safe(esp + 4 + k * 4, &mut b) {
                    args[k] = u32::from_le_bytes(b);
                }
                k += 1;
            }
            log::raw_line(&format!(
                "[GP] CATCH {:#010x}: ecx={:#010x} (actor?) eax={:#010x} edx={:#010x} esi={:#010x} edi={:#010x}",
                target, (*ctx).Ecx, (*ctx).Eax, (*ctx).Edx, (*ctx).Esi, (*ctx).Edi
            ));
            log::raw_line(&format!("[GP] CATCH stack args @esp+4: {:#x?}", args));
            // Fire mode: rewrite this in-flight call to fire our skill (slot, cmd, force=1).
            let fire = FIRE_CMD.load(Ordering::Relaxed);
            if fire != 0 {
                let slot = FIRE_SLOT.load(Ordering::Relaxed);
                let actor_ov = FIRE_ACTOR.load(Ordering::Relaxed);
                if actor_ov != 0 {
                    (*ctx).Ecx = actor_ov;
                }
                let mut ok = true;
                ok &= wr_safe(esp + 4, &slot.to_le_bytes()); // arg0 slot
                ok &= wr_safe(esp + 8, &fire.to_le_bytes()); // arg1 cmd id
                ok &= wr_safe(esp + 0xc, &1u32.to_le_bytes()); // arg2 force = 1
                log::raw_line(&format!(
                    "[GP] FIRE -> actor={:#010x} slot={} cmd={:#x} force=1 ({})",
                    (*ctx).Ecx, slot, fire, if ok { "written" } else { "WRITE FAIL" }
                ));
                FIRE_CMD.store(0, Ordering::Relaxed);
                FIRE_ACTOR.store(0, Ordering::Relaxed);
            }
            TARGET_EIP.store(0, Ordering::Relaxed);
            ARMED.store(false, Ordering::Relaxed);
            let pg = PAGE_BASE.load(Ordering::Relaxed);
            if pg != 0 {
                let prot = ORIG_PROTECT.load(Ordering::Relaxed);
                let mut old = 0u32;
                VirtualProtect(pg as *mut _, PAGE, prot, &mut old);
            }
            return EXCEPTION_CONTINUE_EXECUTION;
        }
        // Hot-page guard: only a genuine NON-target instruction faulting in a tight loop signals a
        // hot page (the SetCommand case). Target-eip hits that were merely filtered out (NPC entity
        // mismatch) are legitimate and must NOT trip the abort, or a busy area starves the capture.
        if eip != target && RAW_HITS.load(Ordering::Relaxed) > CATCH_STORM_CAP {
            TARGET_EIP.store(0, Ordering::Relaxed);
            FIRE_CMD.store(0, Ordering::Relaxed);
            FIRE_ACTOR.store(0, Ordering::Relaxed);
            NEED_REARM.store(false, Ordering::Relaxed);
            ARMED.store(false, Ordering::Relaxed);
            let pg = PAGE_BASE.load(Ordering::Relaxed);
            if pg != 0 {
                let prot = ORIG_PROTECT.load(Ordering::Relaxed);
                let mut old = 0u32;
                VirtualProtect(pg as *mut _, PAGE, prot, &mut old);
            }
            log::raw_line("[GP] catch ABORTED: page too hot (fault storm) -- disarmed");
            return EXCEPTION_CONTINUE_EXECUTION;
        }
        // Not the target (or filtered out): the OS already cleared the guard for this one fault,
        // so this instruction proceeds normally. Flag for the background poller to re-arm shortly.
        NEED_REARM.store(true, Ordering::Relaxed);
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    let c = &*ctx;
    let eip = c.Eip as usize;
    // De-dup: if we've already logged this accessor instruction, just re-arm and move on so a
    // single hot loop doesn't consume all the capture slots. We want distinct call sites.
    let n_seen = SEEN_N.load(Ordering::Relaxed) as usize;
    let mut already = false;
    let mut k = 0usize;
    while k < n_seen && k < SEEN_MAX {
        if SEEN[k] == eip {
            already = true;
            break;
        }
        k += 1;
    }
    if already {
        // Do NOT re-arm: a per-frame reader would fault every tick and wedge the game. The OS
        // already cleared the guard for this fault; one sample per call site is enough.
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    if n_seen < SEEN_MAX {
        SEEN[n_seen] = eip;
        SEEN_N.store(n_seen as u32 + 1, Ordering::Relaxed);
    }
    let n = HITS.fetch_add(1, Ordering::Relaxed) + 1;
    let kind = match access {
        1 => "write",
        8 => "exec",
        _ => "read",
    };
    log::raw_line(&format!(
        "[GP] hit#{} {} {:#010x} (off {:#x}) by {}+{:#x}",
        n,
        kind,
        fault,
        fault.wrapping_sub(page),
        module_tag(eip),
        eip.wrapping_sub(IMAGE_BASE)
    ));
    log::raw_line(&format!(
        "[GP]   eax={:#010x} ebx={:#010x} ecx={:#010x} edx={:#010x} esi={:#010x} edi={:#010x}",
        c.Eax, c.Ebx, c.Ecx, c.Edx, c.Esi, c.Edi
    ));
    // Walk the stack for TRUE return addresses (preceded by a real call). These name the caller
    // chain so we can disasm how `this` is loaded by each parent -> up to the entity root.
    let esp = c.Esp as usize;
    let mut shown = 0;
    let mut i = 0usize;
    while i < 128 && shown < 8 {
        let mut sb = [0u8; 4];
        if !rd_safe(esp + i * 4, &mut sb) {
            break;
        }
        let v = u32::from_le_bytes(sb) as usize;
        if (IMAGE_BASE..0x0200_0000).contains(&v) && is_call_return(v) {
            log::raw_line(&format!(
                "[GP]   ret DDO+{:#x}  (esp+{:#x})",
                v.wrapping_sub(IMAGE_BASE),
                i * 4
            ));
            shown += 1;
        }
        i += 1;
    }
    // Walk the stack for OBJECT pointers (parent `this` values: actor, entity-mgr, etc). A slot
    // holds an object if its first dword is a real vtable (in .rdata) whose first method is .text.
    const TEXT_LO: usize = IMAGE_BASE + 0x1000;
    const TEXT_HI: usize = IMAGE_BASE + 0x16d5000;
    const RD_LO: usize = IMAGE_BASE + 0x16d5000;
    const RD_HI: usize = IMAGE_BASE + 0x2113000;
    let mut objs = 0;
    let mut j = 0usize;
    while j < 192 && objs < 12 {
        let mut sb = [0u8; 4];
        if !rd_safe(esp + j * 4, &mut sb) {
            break;
        }
        let p = u32::from_le_bytes(sb) as usize;
        // Heap pointer, aligned, not the faulting object's own page.
        if p & 3 == 0 && (0x0080_0000..0x7000_0000).contains(&p) && (p < page || p >= page + PAGE) {
            let mut vb = [0u8; 4];
            if rd_safe(p, &mut vb) {
                let vt = u32::from_le_bytes(vb) as usize;
                if (RD_LO..RD_HI).contains(&vt) {
                    let mut mb = [0u8; 4];
                    if rd_safe(vt, &mut mb) {
                        let m0 = u32::from_le_bytes(mb) as usize;
                        if (TEXT_LO..TEXT_HI).contains(&m0) {
                            log::raw_line(&format!(
                                "[GP]   obj? {:#010x} vt {:#010x} (esp+{:#x})",
                                p, vt, j * 4
                            ));
                            objs += 1;
                        }
                    }
                }
            }
        }
        j += 1;
    }
    if n < MAXHITS.load(Ordering::Relaxed) {
        rearm();
    } else {
        ARMED.store(false, Ordering::Relaxed);
        log::raw_line("[GP] reached maxhits (distinct accessors) -> disarmed");
    }
    EXCEPTION_CONTINUE_EXECUTION
}

unsafe fn rearm() {
    // Don't re-guard if a concurrent disarm has already stood us down (prevents a stray re-arm
    // from resurrecting the guard after gpoff).
    if !ARMED.load(Ordering::Relaxed) {
        return;
    }
    let page = PAGE_BASE.load(Ordering::Relaxed);
    if page == 0 {
        return;
    }
    let prot = ORIG_PROTECT.load(Ordering::Relaxed);
    let mut old = 0u32;
    VirtualProtect(page as *mut _, PAGE, prot | PAGE_GUARD, &mut old);
}

/// Arm a guard page on the page containing `addr`. Captures up to `maxhits` distinct accessor
/// EIPs, and auto-disarms after `window_ms` so a hot page only storms briefly (lets us catch a
/// one-shot event like a key-down by pressing it inside the window).
pub fn arm(addr: usize, maxhits: u32, window_ms: u64) {
    let my_epoch = EPOCH.fetch_add(1, Ordering::Relaxed) + 1;
    unsafe {
        if !VEH_INSTALLED.swap(true, Ordering::Relaxed) {
            AddVectoredExceptionHandler(1, Some(gp_veh));
        }
        let page = addr & !(PAGE - 1);
        let mut mbi: MEMORY_BASIC_INFORMATION = core::mem::zeroed();
        if VirtualQuery(
            page as *const _,
            &mut mbi,
            core::mem::size_of::<MEMORY_BASIC_INFORMATION>(),
        ) == 0
        {
            log::line("[GP] arm FAILED: VirtualQuery");
            return;
        }
        let cur = mbi.Protect;
        ORIG_PROTECT.store(cur, Ordering::Relaxed);
        PAGE_BASE.store(page, Ordering::Relaxed);
        HITS.store(0, Ordering::Relaxed);
        RAW_HITS.store(0, Ordering::Relaxed);
        SEEN_N.store(0, Ordering::Relaxed);
        TARGET_EIP.store(0, Ordering::Relaxed);
        NEED_REARM.store(false, Ordering::Relaxed);
        FIRE_CMD.store(0, Ordering::Relaxed);
        FIRE_ACTOR.store(0, Ordering::Relaxed);
        CATCH_ENTITY.store(0, Ordering::Relaxed);
        CATCH_MINCMD.store(0, Ordering::Relaxed);
        MAXHITS.store(maxhits.max(1), Ordering::Relaxed);
        ARMED.store(true, Ordering::Relaxed);
        let mut old = 0u32;
        if VirtualProtect(page as *mut _, PAGE, cur | PAGE_GUARD, &mut old) == 0 {
            ARMED.store(false, Ordering::Relaxed);
            log::line("[GP] arm FAILED: VirtualProtect");
            return;
        }
        log::line(&format!(
            "[GP] armed guard on page {:#010x} (orig protect {:#x}, maxhits {}, window {}ms)",
            page, cur, maxhits, window_ms
        ));
    }
    // Watchdog: auto-disarm after the window unless a newer arming superseded us.
    std::thread::spawn(move || {
        std::thread::sleep(std::time::Duration::from_millis(window_ms));
        if EPOCH.load(Ordering::Relaxed) == my_epoch && ARMED.load(Ordering::Relaxed) {
            log::line("[GP] capture window elapsed");
            disarm();
        }
    });
}

/// Catch a specific function ENTRY. Guards the page holding `func`; single-steps over every other
/// access; when execution reaches `func` it logs ECX (the `this`/actor) + stack args and disarms.
/// Press the skill within `window_ms`.
pub fn catch(func: usize, window_ms: u64, mincmd: u32, entity: u32) {
    let my_epoch = EPOCH.fetch_add(1, Ordering::Relaxed) + 1;
    unsafe {
        if !VEH_INSTALLED.swap(true, Ordering::Relaxed) {
            AddVectoredExceptionHandler(1, Some(gp_veh));
        }
        let page = func & !(PAGE - 1);
        let mut mbi: MEMORY_BASIC_INFORMATION = core::mem::zeroed();
        if VirtualQuery(
            page as *const _,
            &mut mbi,
            core::mem::size_of::<MEMORY_BASIC_INFORMATION>(),
        ) == 0
        {
            log::line("[GP] catch FAILED: VirtualQuery");
            return;
        }
        let cur = mbi.Protect;
        ORIG_PROTECT.store(cur, Ordering::Relaxed);
        PAGE_BASE.store(page, Ordering::Relaxed);
        HITS.store(0, Ordering::Relaxed);
        RAW_HITS.store(0, Ordering::Relaxed);
        SEEN_N.store(0, Ordering::Relaxed);
        NEED_REARM.store(false, Ordering::Relaxed);
        CATCH_MINCMD.store(mincmd, Ordering::Relaxed);
        CATCH_ENTITY.store(entity & 0x00ff_ffff, Ordering::Relaxed);
        TARGET_EIP.store(func, Ordering::Relaxed);
        ARMED.store(true, Ordering::Relaxed);
        let mut old = 0u32;
        if VirtualProtect(page as *mut _, PAGE, cur | PAGE_GUARD, &mut old) == 0 {
            ARMED.store(false, Ordering::Relaxed);
            TARGET_EIP.store(0, Ordering::Relaxed);
            log::line("[GP] catch FAILED: VirtualProtect");
            return;
        }
        log::line(&format!(
            "[GP] catching entry {:#010x} on page {:#010x} (orig protect {:#x}, window {}ms)",
            func, page, cur, window_ms
        ));
    }
    // Background re-arm poller: re-applies the guard shortly after each non-target fault (off the
    // faulting thread), and auto-disarms when the window elapses. No single-stepping.
    std::thread::spawn(move || {
        let deadline = std::time::Instant::now() + std::time::Duration::from_millis(window_ms);
        loop {
            if EPOCH.load(Ordering::Relaxed) != my_epoch || !ARMED.load(Ordering::Relaxed) {
                return; // superseded, or a hit disarmed us
            }
            if std::time::Instant::now() >= deadline {
                TARGET_EIP.store(0, Ordering::Relaxed);
                log::line("[GP] catch window elapsed (no hit)");
                disarm();
                return;
            }
            if NEED_REARM.swap(false, Ordering::Relaxed) {
                unsafe { rearm() };
            }
            std::thread::sleep(std::time::Duration::from_millis(1));
        }
    });
}

/// Fire mode: catch execCommand's entry and rewrite the in-flight call to fire `cmd` on `slot`
/// with force=1 -- executes on the game thread, no off-thread call, no code patch. One-shot.
pub fn fire(func: usize, cmd: u32, slot: u32, actor: u32, window_ms: u64) {
    FIRE_CMD.store(cmd, Ordering::Relaxed);
    FIRE_SLOT.store(slot, Ordering::Relaxed);
    FIRE_ACTOR.store(actor, Ordering::Relaxed);
    catch(func, window_ms, 0, 0);
}

/// Disarm: restore the original protection. We deliberately KEEP PAGE_BASE set so that any
/// guard fault already in flight on this page is still recognized and absorbed by the VEH
/// (zeroing it here is what crashed the game: the in-flight fault went unhandled).
/// Arm a one-shot guard on the page holding `global_addr`; the next game-thread read fires
/// any pending cast via `skill::drain_pending()` (no code hooks).
pub fn arm_cast_delivery(global_addr: usize) {
    CAST_DELIVERY.store(true, Ordering::Release);
    arm(global_addr, 1, 4000);
}

pub fn disarm() {
    unsafe {
        CAST_DELIVERY.store(false, Ordering::Relaxed);
        ARMED.store(false, Ordering::Relaxed);
        let page = PAGE_BASE.load(Ordering::Relaxed);
        if page != 0 {
            let prot = ORIG_PROTECT.load(Ordering::Relaxed);
            let mut old = 0u32;
            VirtualProtect(page as *mut _, PAGE, prot, &mut old);
        }
        log::line("[GP] disarmed");
    }
}

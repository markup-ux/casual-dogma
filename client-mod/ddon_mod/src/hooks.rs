//! Live-trace inline hooks (synchook-style: anti-debug-safe byte patch, no debug regs).
//!
//! Hooks:
//! - Input frame @ VA 0x14ea090 (thiscall, ecx = game/input manager): drains pending custom-skill
//!   casts via `skill::drain_pending()` on the game's input thread.
//! - Palette-swap update FSM at VA 0x6c3d70 (thiscall, ecx = action
//! object). From the action object we read:
//!   +0x10  step/phase byte (FSM index)
//!   +0x14  owner / player object  <- the master anchor for everything
//!   +0x0c  owner sub-object
//! The captured player pointer is published in `PLAYER` for the probe to use, so after
//! one palette swap we can snap/diff the player object to find the active-palette index
//! (task A) and the equipped skill-id slots (task B).
//!
//! Prologue @ 0x6c3d70: 83 ec 08 | 56 | 8b f1  (sub esp,8; push esi; mov esi,ecx) = 6 bytes.

use core::sync::atomic::{AtomicUsize, Ordering};

use windows_sys::Win32::System::LibraryLoader::GetModuleHandleW;
use windows_sys::Win32::System::Memory::{
    VirtualAlloc, VirtualProtect, MEM_COMMIT, MEM_RESERVE, PAGE_EXECUTE_READWRITE,
};

use crate::log;

const IMAGE_BASE_DEFAULT: usize = 0x40_0000;
const VA_INPUT_FRAME: usize = 0x14e_a090;
const PROLOGUE_INPUT: usize = 6;
const VA_SWAP_UPDATE: usize = 0x6c_3d70;
const PROLOGUE_SWAP: usize = 6;

type ThisCallFn = extern "thiscall" fn(usize) -> usize;

static TRAMP_INPUT: AtomicUsize = AtomicUsize::new(0);
static TRAMP_SWAP: AtomicUsize = AtomicUsize::new(0);
/// Latest player/owner object captured from a palette swap (0 until first swap).
pub static PLAYER: AtomicUsize = AtomicUsize::new(0);
/// Latest palette-swap action object.
pub static ACTION: AtomicUsize = AtomicUsize::new(0);
/// Number of times the swap-update ran (how we know a swap happened).
pub static HITS: AtomicUsize = AtomicUsize::new(0);

// Input-thread hook: fire queued casts before the real input frame runs (same thread/path as key handler).
extern "thiscall" fn detour_input(this: usize) -> usize {
    crate::recoverable_hp::on_input_frame();
    crate::skill::refresh_player_cache(this);
    if crate::skill::has_pending_cast() {
        crate::skill::drain_pending();
    }
    let t = TRAMP_INPUT.load(Ordering::Acquire);
    if t != 0 {
        let f: ThisCallFn = unsafe { core::mem::transmute(t) };
        f(this)
    } else {
        0
    }
}

// CRITICAL: swap hook runs on the game's main thread inside the swap FSM. It must be
// allocation-free and lock-free (no logging / format! / file I/O), or it interleaves
// our heap activity with the game's mid-swap allocations and corrupts the heap. We do
// the absolute minimum: read the owner pointer and store it; the probe logs it lazily.
extern "thiscall" fn detour_swap(this: usize) -> usize {
    if this != 0 {
        let owner = unsafe { core::ptr::read_unaligned((this + 0x14) as *const u32) } as usize;
        if owner != 0 {
            PLAYER.store(owner, Ordering::Relaxed);
        }
        ACTION.store(this, Ordering::Relaxed);
        HITS.fetch_add(1, Ordering::Relaxed);
    }
    let t = TRAMP_SWAP.load(Ordering::Acquire);
    if t != 0 {
        let f: ThisCallFn = unsafe { core::mem::transmute(t) };
        f(this)
    } else {
        0
    }
}

unsafe fn install(target: usize, prologue: usize, tramp_slot: &AtomicUsize, detour: usize) -> bool {
    let tramp = VirtualAlloc(
        core::ptr::null(),
        32,
        MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE,
    ) as usize;
    if tramp == 0 {
        return false;
    }
    core::ptr::copy_nonoverlapping(target as *const u8, tramp as *mut u8, prologue);
    let jmp_site = tramp + prologue;
    *(jmp_site as *mut u8) = 0xE9;
    let rel_back = (target + prologue) as isize - (jmp_site + 5) as isize;
    *((jmp_site + 1) as *mut i32) = rel_back as i32;
    tramp_slot.store(tramp, Ordering::Release);

    let mut old: u32 = 0;
    if VirtualProtect(target as *const _, 16, PAGE_EXECUTE_READWRITE, &mut old) == 0 {
        return false;
    }
    *(target as *mut u8) = 0xE9;
    let rel = detour as isize - (target + 5) as isize;
    *((target + 1) as *mut i32) = rel as i32;
    for i in 5..prologue {
        *((target + i) as *mut u8) = 0x90;
    }
    let mut tmp: u32 = 0;
    VirtualProtect(target as *const _, 16, old, &mut tmp);
    true
}

pub fn install_all() {
    crate::skill::init_issue_thunk();
    let base = unsafe { GetModuleHandleW(core::ptr::null()) as usize };
    let input = base + (VA_INPUT_FRAME - IMAGE_BASE_DEFAULT);
    unsafe {
        if install(
            input,
            PROLOGUE_INPUT,
            &TRAMP_INPUT,
            detour_input as usize,
        ) {
            log::line(&format!(
                "[hook] input frame hooked @ {:#010x} (cast queue on input thread)",
                input
            ));
        } else {
            log::line("[hook] input frame hook FAILED");
        }
    }
    // Swap FSM hook disabled — it crashes (~15s–2min). Player ctx comes from game+0x154 reads.
    log::line("[hook] palette-swap hook skipped (unstable; use game+0x154 for player ctx)");
}

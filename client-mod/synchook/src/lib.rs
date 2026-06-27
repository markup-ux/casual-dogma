//! synchook - DDON client param-capture DLL (32-bit), SAFE inline byte-patch hook.
//!
//! Hooks the cNamedParam recompute method at VA 0x5fd680 (thiscall; ecx = param).
//! Logs a param pointer + stat block only when that param's stats CHANGE, so at rest
//! the log is quiet and a weapon swap reveals YOUR param (the one whose stats changed).
//! No debug registers (anti-debug safe). Output: C:\Users\Public\ddon_synchook.log
//!
//! cNamedParam stat block (u16): +0x10 exp +0x12 aBP +0x14 aWP +0x16 dBP +0x18 dWP
//!   +0x1a aBM +0x1c aWM +0x1e dBM +0x20 dWM +0x22 pow

use std::collections::HashMap;
use std::fs::OpenOptions;
use std::io::Write;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::sync::Mutex;

use windows_sys::Win32::Foundation::{BOOL, HMODULE, TRUE};
use windows_sys::Win32::System::LibraryLoader::GetModuleHandleW;
use windows_sys::Win32::System::Memory::{
    VirtualAlloc, VirtualProtect, MEM_COMMIT, MEM_RESERVE, PAGE_EXECUTE_READWRITE,
};
use windows_sys::Win32::System::SystemServices::DLL_PROCESS_ATTACH;

const VA_TARGET: usize = 0x5fd680; // cNamedParam recompute (ecx = param)
const IMAGE_BASE_DEFAULT: usize = 0x400000;
const PROLOGUE_LEN: usize = 6; // push esi; mov esi,ecx; test byte[esi],0x80

type HookFn = extern "fastcall" fn(usize, usize) -> usize;

static TRAMPOLINE: AtomicUsize = AtomicUsize::new(0);

// Lock-free change filter: direct-mapped cache of (ptr, signature).
const SLOTS: usize = 4096;
static CACHE_PTR: [AtomicUsize; SLOTS] = {
    const Z: AtomicUsize = AtomicUsize::new(0);
    [Z; SLOTS]
};
static CACHE_SIG: [AtomicUsize; SLOTS] = {
    const Z: AtomicUsize = AtomicUsize::new(0);
    [Z; SLOTS]
};

// Changed (ptr, [10]u16) pushed here for the flush thread.
static CHANGES: Mutex<Vec<(usize, [u16; 10])>> = Mutex::new(Vec::new());

fn logln(s: &str) {
    if let Ok(mut f) = OpenOptions::new()
        .create(true)
        .append(true)
        .open("C:\\Users\\Public\\ddon_synchook.log")
    {
        let _ = writeln!(f, "{}", s);
    }
}

#[inline]
unsafe fn read_block(p: usize) -> [u16; 10] {
    let mut b = [0u16; 10];
    for i in 0..10 {
        b[i] = core::ptr::read_unaligned((p + 0x10 + i * 2) as *const u16);
    }
    b
}

#[inline]
fn sig_of(b: &[u16; 10]) -> usize {
    let mut h: usize = 0xcbf29ce4;
    for &v in b {
        h = (h ^ v as usize).wrapping_mul(0x1000193);
    }
    h
}

extern "fastcall" fn detour(this: usize, edx: usize) -> usize {
    // Sample/filter BEFORE calling original (param holds current/last values).
    if this != 0 {
        unsafe {
            let b = read_block(this);
            let sig = sig_of(&b);
            let idx = (this >> 4) & (SLOTS - 1);
            let pprev = CACHE_PTR[idx].load(Ordering::Relaxed);
            let sprev = CACHE_SIG[idx].load(Ordering::Relaxed);
            if pprev != this || sprev != sig {
                CACHE_PTR[idx].store(this, Ordering::Relaxed);
                CACHE_SIG[idx].store(sig, Ordering::Relaxed);
                if let Ok(mut v) = CHANGES.lock() {
                    if v.len() < 16384 {
                        v.push((this, b));
                    }
                }
            }
        }
    }
    let tramp = TRAMPOLINE.load(Ordering::Acquire);
    if tramp != 0 {
        let f: HookFn = unsafe { core::mem::transmute(tramp) };
        f(this, edx)
    } else {
        0
    }
}

unsafe fn install_hook(target: usize) -> bool {
    let tramp = VirtualAlloc(
        core::ptr::null(),
        32,
        MEM_COMMIT | MEM_RESERVE,
        PAGE_EXECUTE_READWRITE,
    ) as usize;
    if tramp == 0 {
        return false;
    }
    core::ptr::copy_nonoverlapping(target as *const u8, tramp as *mut u8, PROLOGUE_LEN);
    let jmp_site = tramp + PROLOGUE_LEN;
    *(jmp_site as *mut u8) = 0xE9;
    let rel_back = (target + PROLOGUE_LEN) as isize - (jmp_site + 5) as isize;
    *((jmp_site + 1) as *mut i32) = rel_back as i32;
    TRAMPOLINE.store(tramp, Ordering::Release);

    let mut old: u32 = 0;
    if VirtualProtect(target as *const _, 16, PAGE_EXECUTE_READWRITE, &mut old) == 0 {
        return false;
    }
    *(target as *mut u8) = 0xE9;
    let rel = (detour as usize) as isize - (target + 5) as isize;
    *((target + 1) as *mut i32) = rel as i32;
    for i in 5..PROLOGUE_LEN {
        *((target + i) as *mut u8) = 0x90;
    }
    let mut tmp: u32 = 0;
    VirtualProtect(target as *const _, 16, old, &mut tmp);
    true
}

fn flush_worker() {
    // Per-pointer last block, to log only genuine changes (and skip initial spam).
    let mut last: HashMap<usize, [u16; 10]> = HashMap::new();
    let mut warmed = false;
    let mut warm_ticks = 0;
    loop {
        std::thread::sleep(std::time::Duration::from_millis(300));
        let batch: Vec<(usize, [u16; 10])> = match CHANGES.lock() {
            Ok(mut v) => {
                let out = v.clone();
                v.clear();
                out
            }
            Err(_) => continue,
        };
        // First ~2s: just populate baseline without logging (avoids initial load spam).
        if !warmed {
            for (p, b) in &batch {
                last.insert(*p, *b);
            }
            warm_ticks += 1;
            if warm_ticks >= 7 {
                warmed = true;
                logln(&format!("[synchook] baseline ready ({} params). Now swap your weapon.", last.len()));
            }
            continue;
        }
        for (p, b) in batch {
            let prev = last.get(&p).copied();
            if prev == Some(b) {
                continue;
            }
            last.insert(p, b);
            let pa = b[1].wrapping_add(b[2]);
            let pd = b[3].wrapping_add(b[4]);
            let ma = b[5].wrapping_add(b[6]);
            let md = b[7].wrapping_add(b[8]);
            let tag = if prev.is_none() { "NEW" } else { "CHG" };
            logln(&format!(
                "{} param={:#x} PA={} PD={} MA={} MD={} | exp={} aBP={} aWP={} dBP={} dWP={} aBM={} aWM={} dBM={} dWM={} pow={}",
                tag, p, pa, pd, ma, md,
                b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7], b[8], b[9]
            ));
        }
    }
}

fn init_worker() {
    unsafe {
        let base = GetModuleHandleW(core::ptr::null()) as usize;
        let target = base + (VA_TARGET - IMAGE_BASE_DEFAULT);
        logln(&format!(
            "[synchook inline] loaded. image_base={:#x} target={:#x}",
            base, target
        ));
        if install_hook(target) {
            logln("[synchook] inline hook installed OK (warming baseline ~2s)");
        } else {
            logln("[synchook] inline hook install FAILED");
            return;
        }
    }
    flush_worker();
}

#[no_mangle]
pub extern "system" fn DllMain(_h: HMODULE, reason: u32, _r: *mut core::ffi::c_void) -> BOOL {
    if reason == DLL_PROCESS_ATTACH {
        std::thread::spawn(init_worker);
    }
    TRUE
}

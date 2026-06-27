//! In-process memory probe driven by a command file.
//!
//! Watches `C:\Users\Public\ddon_cmd.txt`; whenever it is (re)written, each line is
//! executed and results go to the normal log (which the host monitor tails). This is
//! the live RE workbench used to find the palette/skill structures, and it grows into
//! the actual skill-trigger machinery.
//!
//! All memory access goes through ReadProcessMemory/WriteProcessMemory on our OWN
//! process handle. That makes bad addresses fail gracefully (FALSE) instead of
//! faulting the game, so probing is safe even though we run in-process.
//!
//! Commands (addresses are hex, with or without `0x`):
//!   help
//!   modules                         list loaded modules (base, size, name)
//!   peek <addr> [u8|u16|u32|i32|f32|ptr]
//!   poke <addr> <u8|u16|u32|i32|f32> <value>
//!   dump <addr> <len>               hex+ascii (len capped at 1024)
//!   find-vtable <addr> [maxhits]    objects whose first dword == addr
//!   scan-actor                      find local player actor via actor vtable + max atk
//!   snap <addr> <len>               capture a region (len capped at 65536)
//!   diff                            report bytes changed since last snap (and rebase)

use core::ffi::c_void;
use core::sync::atomic::{AtomicBool, AtomicU32, Ordering};
use std::sync::Mutex;

use windows_sys::Win32::Foundation::HMODULE;
use windows_sys::Win32::System::Diagnostics::Debug::{ReadProcessMemory, WriteProcessMemory};
use windows_sys::Win32::System::LibraryLoader::{GetModuleFileNameW, GetModuleHandleW};
use windows_sys::Win32::System::Memory::{VirtualQuery, MEMORY_BASIC_INFORMATION};
use windows_sys::Win32::System::Threading::GetCurrentProcess;

use crate::log;

const CMD_PATH: &str = "C:\\Users\\Public\\ddon_cmd.txt";
// Generation guard: hot-loading a new DLL leaves the old instances running (DllMain only runs
// once per load and we can't unload them). Each injection bumps this counter; only the newest
// generation processes commands / heartbeats / guard pages, so stale instances go inert instead
// of double-executing every command and fighting over PAGE_GUARD flips.
const GEN_PATH: &str = "C:\\Users\\Public\\ddon_mod_gen.txt";
static MY_GEN: AtomicU32 = AtomicU32::new(0);

fn read_gen() -> u32 {
    std::fs::read_to_string(GEN_PATH)
        .ok()
        .and_then(|s| s.trim().parse::<u32>().ok())
        .unwrap_or(0)
}

fn register_generation() -> u32 {
    let my = read_gen().wrapping_add(1);
    let _ = std::fs::write(GEN_PATH, my.to_string());
    MY_GEN.store(my, Ordering::Relaxed);
    my
}

/// True only for the most-recently-injected DLL instance. Stale instances return false and
/// stand down (no command processing, no heartbeat, no guard pages).
pub fn is_latest() -> bool {
    read_gen() == MY_GEN.load(Ordering::Relaxed)
}

// Actor vtable from SKILL_RE_NOTES.md: VA 0x1af89d0 @ image base 0x400000 -> RVA 0x16f89d0.
const ACTOR_VTABLE_RVA: usize = 0x16f_89d0;
const ACTOR_ATK_OFF: usize = 0x64; // f32 physical attack
const ACTOR_MATK_OFF: usize = 0x68; // f32 magick attack

const MEM_COMMIT: u32 = 0x1000;
const MEM_PRIVATE: u32 = 0x20000;
const MEM_IMAGE: u32 = 0x1000000;
const PAGE_READWRITE: u32 = 0x04;
const PAGE_EXECUTE_READWRITE: u32 = 0x40;

const SCAN_MAX_ADDR: usize = 0x7FFF_0000;
const CHUNK: usize = 1 << 20;

static SNAP: Mutex<Option<(usize, Vec<u8>)>> = Mutex::new(None);
// Cheat-Engine style result set: (address, value-bits at last scan).
static FIND: Mutex<Vec<(usize, u32)>> = Mutex::new(Vec::new());
const FIND_CAP: usize = 200_000;

fn image_base() -> usize {
    unsafe { GetModuleHandleW(core::ptr::null()) as usize }
}

fn safe_read(addr: usize, buf: &mut [u8]) -> bool {
    if addr == 0 || buf.is_empty() {
        return false;
    }
    unsafe {
        let mut got = 0usize;
        ReadProcessMemory(
            GetCurrentProcess(),
            addr as *const c_void,
            buf.as_mut_ptr() as *mut c_void,
            buf.len(),
            &mut got,
        ) != 0
            && got == buf.len()
    }
}

fn safe_write(addr: usize, buf: &[u8]) -> bool {
    if addr == 0 || buf.is_empty() {
        return false;
    }
    unsafe {
        let mut put = 0usize;
        WriteProcessMemory(
            GetCurrentProcess(),
            addr as *mut c_void,
            buf.as_ptr() as *const c_void,
            buf.len(),
            &mut put,
        ) != 0
            && put == buf.len()
    }
}

pub(crate) fn safe_read_pub(addr: usize, buf: &mut [u8]) -> bool {
    safe_read(addr, buf)
}

pub(crate) fn read_u32(addr: usize) -> Option<u32> {
    let mut b = [0u8; 4];
    if safe_read(addr, &mut b) {
        Some(u32::from_le_bytes(b))
    } else {
        None
    }
}

fn read_f32(addr: usize) -> Option<f32> {
    read_u32(addr).map(f32::from_bits)
}

fn parse_uint(s: &str) -> Option<usize> {
    let t = s.trim();
    let t = t.strip_prefix("0x").or_else(|| t.strip_prefix("0X")).unwrap_or(t);
    usize::from_str_radix(t, 16).ok()
}

fn module_name_of(base: usize) -> String {
    unsafe {
        let mut buf = [0u16; 260];
        let n = GetModuleFileNameW(base as HMODULE, buf.as_mut_ptr(), buf.len() as u32) as usize;
        if n == 0 {
            return String::new();
        }
        let full = String::from_utf16_lossy(&buf[..n]);
        full.rsplit(['\\', '/']).next().unwrap_or(&full).to_string()
    }
}

/// Walk the address space, calling `f(base, size, mbi)` for each committed region.
fn for_each_region<F: FnMut(usize, usize, &MEMORY_BASIC_INFORMATION)>(mut f: F) {
    let mut addr: usize = 0;
    unsafe {
        while addr < SCAN_MAX_ADDR {
            let mut mbi: MEMORY_BASIC_INFORMATION = core::mem::zeroed();
            let r = VirtualQuery(
                addr as *const c_void,
                &mut mbi,
                core::mem::size_of::<MEMORY_BASIC_INFORMATION>(),
            );
            if r == 0 {
                break;
            }
            let base = mbi.BaseAddress as usize;
            let size = mbi.RegionSize;
            if size == 0 {
                break;
            }
            if mbi.State == MEM_COMMIT {
                f(base, size, &mbi);
            }
            match base.checked_add(size) {
                Some(next) if next > addr => addr = next,
                _ => break,
            }
        }
    }
}

fn is_object_region(mbi: &MEMORY_BASIC_INFORMATION) -> bool {
    mbi.Type == MEM_PRIVATE
        && (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_EXECUTE_READWRITE)
}

/// Executable image region (the game's .text and other code, where instructions live).
fn is_code_region(mbi: &MEMORY_BASIC_INFORMATION) -> bool {
    mbi.Type == MEM_IMAGE && (mbi.Protect & 0xF0) != 0
}

/// Scan executable image regions (1-byte step) for dwords equal to `target` -- catches the
/// constant embedded in instructions (e.g. `push 0x1bac1e0` / `mov reg,[global]`). Returns the
/// addresses of the operand bytes (subtract the opcode length to get instruction start).
fn find_xref(target: u32, max_hits: usize) -> Vec<usize> {
    let mut hits = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    let want_bytes = target.to_le_bytes();
    for_each_region(|base, size, mbi| {
        if hits.len() >= max_hits || !is_code_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && hits.len() < max_hits {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 4 <= want {
                    if buf[i] == want_bytes[0]
                        && buf[i + 1] == want_bytes[1]
                        && buf[i + 2] == want_bytes[2]
                        && buf[i + 3] == want_bytes[3]
                    {
                        hits.push(base + off + i);
                        if hits.len() >= max_hits {
                            break;
                        }
                    }
                    i += 1;
                }
            }
            // Overlap 3 bytes so a dword straddling a chunk edge isn't missed, but ALWAYS
            // advance >=1 (want-3 is 0 when want==3 -> would otherwise spin forever).
            off += if want > 3 { want - 3 } else { want };
        }
    });
    hits
}

/// Scan executable image for `call`/`jmp rel32` (E8/E9) whose computed target == `target`.
/// Returns the instruction addresses (the E8/E9 byte). These are the call sites of a function.
fn find_callers(target: usize, max_hits: usize) -> Vec<usize> {
    let mut hits = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    for_each_region(|base, size, mbi| {
        if hits.len() >= max_hits || !is_code_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && hits.len() < max_hits {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 5 <= want {
                    if buf[i] == 0xE8 || buf[i] == 0xE9 {
                        let rel = i32::from_le_bytes([buf[i + 1], buf[i + 2], buf[i + 3], buf[i + 4]]);
                        let ins = base + off + i;
                        let tgt = (ins + 5).wrapping_add(rel as usize);
                        if tgt == target {
                            hits.push(ins);
                            if hits.len() >= max_hits {
                                break;
                            }
                        }
                    }
                    i += 1;
                }
            }
            off += if want > 5 { want - 5 } else { want };
        }
    });
    hits
}

/// callers <targetaddr> [maxhits]  -- find E8/E9 call/jmp sites that target the function.
fn cmd_callers(args: &[&str]) {
    let Some(tgt) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] callers: usage: callers <targetaddr> [maxhits]");
        return;
    };
    let max = args.get(1).and_then(|s| s.parse::<usize>().ok()).unwrap_or(40);
    log::line(&format!("[probe] callers of {:#010x} (max {}) scanning code...", tgt, max));
    let hits = find_callers(tgt, max);
    log::line(&format!("[probe] callers of {:#010x}: {} site(s)", tgt, hits.len()));
    for h in &hits {
        log::line(&format!("[probe]   call/jmp @{:#010x}", h));
    }
}

/// Scan private RW regions for dwords equal to `target` (4-byte aligned); returns addresses.
pub(crate) fn find_dword(target: u32, max_hits: usize) -> Vec<usize> {
    let mut hits = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    for_each_region(|base, size, mbi| {
        if hits.len() >= max_hits || !is_object_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && hits.len() < max_hits {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 4 <= want {
                    let v = u32::from_le_bytes([buf[i], buf[i + 1], buf[i + 2], buf[i + 3]]);
                    if v == target {
                        hits.push(base + off + i);
                        if hits.len() >= max_hits {
                            break;
                        }
                    }
                    i += 4;
                }
            }
            off += want;
        }
    });
    hits
}

fn cmd_modules() {
    log::line("[probe] modules:");
    let mut last_base = usize::MAX;
    for_each_region(|_, _, mbi| {
        if mbi.Type == MEM_IMAGE {
            let alloc = mbi.AllocationBase as usize;
            if alloc != 0 && alloc != last_base {
                last_base = alloc;
                let name = module_name_of(alloc);
                if !name.is_empty() {
                    log::line(&format!("[probe]   {:#010x}  {}", alloc, name));
                }
            }
        }
    });
}

fn cmd_peek(args: &[&str]) {
    let Some(addr) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] peek: bad addr");
        return;
    };
    let ty = args.get(1).copied().unwrap_or("u32");
    match ty {
        "u8" => {
            let mut b = [0u8; 1];
            if safe_read(addr, &mut b) {
                log::line(&format!("[probe] peek {:#010x} u8 = {} ({:#04x})", addr, b[0], b[0]));
            } else {
                log::line(&format!("[probe] peek {:#010x} unreadable", addr));
            }
        }
        "u16" => {
            let mut b = [0u8; 2];
            if safe_read(addr, &mut b) {
                let v = u16::from_le_bytes(b);
                log::line(&format!("[probe] peek {:#010x} u16 = {} ({:#06x})", addr, v, v));
            } else {
                log::line(&format!("[probe] peek {:#010x} unreadable", addr));
            }
        }
        "i32" => match read_u32(addr) {
            Some(v) => log::line(&format!("[probe] peek {:#010x} i32 = {}", addr, v as i32)),
            None => log::line(&format!("[probe] peek {:#010x} unreadable", addr)),
        },
        "f32" => match read_f32(addr) {
            Some(v) => log::line(&format!("[probe] peek {:#010x} f32 = {}", addr, v)),
            None => log::line(&format!("[probe] peek {:#010x} unreadable", addr)),
        },
        "ptr" | "u32" => match read_u32(addr) {
            Some(v) => log::line(&format!("[probe] peek {:#010x} {} = {:#010x} ({})", addr, ty, v, v)),
            None => log::line(&format!("[probe] peek {:#010x} unreadable", addr)),
        },
        _ => log::line(&format!("[probe] peek: unknown type '{}'", ty)),
    }
}

fn cmd_poke(args: &[&str]) {
    let (Some(addr), Some(ty), Some(valstr)) =
        (args.first().and_then(|s| parse_uint(s)), args.get(1).copied(), args.get(2).copied())
    else {
        log::line("[probe] poke: usage: poke <addr> <type> <value>");
        return;
    };
    let ok = match ty {
        "u8" => valstr
            .parse::<u8>()
            .ok()
            .or_else(|| parse_uint(valstr).map(|v| v as u8))
            .map(|v| safe_write(addr, &[v]))
            .unwrap_or(false),
        "u16" => parse_uint(valstr)
            .map(|v| safe_write(addr, &(v as u16).to_le_bytes()))
            .unwrap_or(false),
        "u32" | "i32" => parse_uint(valstr)
            .map(|v| safe_write(addr, &(v as u32).to_le_bytes()))
            .unwrap_or(false),
        "f32" => valstr
            .parse::<f32>()
            .ok()
            .map(|v| safe_write(addr, &v.to_bits().to_le_bytes()))
            .unwrap_or(false),
        _ => {
            log::line(&format!("[probe] poke: unknown type '{}'", ty));
            return;
        }
    };
    log::line(&format!(
        "[probe] poke {:#010x} {} {} -> {}",
        addr, ty, valstr, if ok { "OK" } else { "FAILED" }
    ));
}

fn cmd_dump(args: &[&str]) {
    let Some(addr) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] dump: bad addr");
        return;
    };
    let len = args.get(1).and_then(|s| parse_uint(s)).unwrap_or(64).min(1024);
    let mut buf = vec![0u8; len];
    if !safe_read(addr, &mut buf) {
        log::line(&format!("[probe] dump {:#010x} unreadable ({} bytes)", addr, len));
        return;
    }
    log::line(&format!("[probe] dump {:#010x} ({} bytes):", addr, len));
    for (row, chunk) in buf.chunks(16).enumerate() {
        let mut hex = String::new();
        let mut asc = String::new();
        for b in chunk {
            hex.push_str(&format!("{:02x} ", b));
            asc.push(if *b >= 0x20 && *b < 0x7f { *b as char } else { '.' });
        }
        log::line(&format!("[probe]   {:#010x}: {:<48}| {}", addr + row * 16, hex, asc));
    }
}

/// xref <value> [maxhits]  -- find code (executable image) sites that embed <value> as an
/// operand. Use to locate vtable construction sites or global-pointer loads.
fn cmd_xref(args: &[&str]) {
    let Some(val) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] xref: usage: xref <hexvalue> [maxhits]");
        return;
    };
    let max = args.get(1).and_then(|s| s.parse::<usize>().ok()).unwrap_or(40);
    log::line(&format!("[probe] xref {:#010x} (max {}) scanning code...", val, max));
    let hits = find_xref(val as u32, max);
    log::line(&format!("[probe] xref {:#010x}: {} site(s)", val, hits.len()));
    for h in &hits {
        log::line(&format!("[probe]   @{:#010x} (operand; instr starts a few bytes earlier)", h));
    }
}

fn cmd_find_vtable(args: &[&str]) {
    let Some(vt) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] find-vtable: bad addr");
        return;
    };
    let max = args.get(1).and_then(|s| s.parse::<usize>().ok()).unwrap_or(64);
    log::line(&format!("[probe] find-vtable {:#010x} (max {}) scanning...", vt, max));
    let hits = find_dword(vt as u32, max);
    log::line(&format!("[probe] find-vtable {:#010x}: {} hit(s)", vt, hits.len()));
    for h in &hits {
        log::line(&format!("[probe]   obj {:#010x}", h));
    }
}

/// Print the player object captured by the palette-swap hook (if any).
fn cmd_player() {
    use core::sync::atomic::Ordering::Relaxed;
    let p = crate::hooks::PLAYER.load(Relaxed);
    let action = crate::hooks::ACTION.load(Relaxed);
    let hits = crate::hooks::HITS.load(Relaxed);
    log::line(&format!("[probe] swap hits = {}", hits));
    if p == 0 {
        log::line("[probe] player: not captured yet -- press the palette-swap button once");
    } else {
        log::line(&format!(
            "[probe] player = {:#010x}  action = {:#010x}",
            p, action
        ));
    }
}

/// Detect the player's HP/Stamina with no prior knowledge by scanning for the param-block
/// signature confirmed by live RE:
///   +0x00 HP cur (u32)   +0x08 HP recoverable (u32)   +0x10 HP max (u32)
///   +0x18 Stamina cur (f32)   +0x1c Stamina max (f32)
/// with the in-between words (+0x04/+0x0c/+0x14) zero and HP monotonic (cur<=rec<=max).
/// Usage: hp [maxhits]
fn cmd_hp(args: &[&str]) {
    let max = args.first().and_then(|s| s.parse::<usize>().ok()).unwrap_or(64);
    log::line("[probe] hp: signature-scanning for HP/Stamina param block...");
    let mut rows: Vec<(usize, u32, u32, u32, f32, f32)> = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    let rd_u32 = |b: &[u8], o: usize| u32::from_le_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);
    for_each_region(|base, size, mbi| {
        if rows.len() >= max || !is_object_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && rows.len() < max {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 0x20 <= want && rows.len() < max {
                    // Padding words must be zero -- cheapest discriminator first.
                    if rd_u32(buf, i + 4) == 0
                        && rd_u32(buf, i + 0xc) == 0
                        && rd_u32(buf, i + 0x14) == 0
                    {
                        let cur = rd_u32(buf, i);
                        let rec = rd_u32(buf, i + 8);
                        let mx = rd_u32(buf, i + 0x10);
                        let stc = f32::from_bits(rd_u32(buf, i + 0x18));
                        let stm = f32::from_bits(rd_u32(buf, i + 0x1c));
                        let hp_ok = (1..=200_000).contains(&cur) && cur <= rec && rec <= mx && mx <= 200_000;
                        let st_ok = stc.is_finite()
                            && stm.is_finite()
                            && (1.0..=200_000.0).contains(&stc)
                            && stc <= stm
                            && stm <= 200_000.0;
                        if hp_ok && st_ok {
                            rows.push((base + off + i, cur, rec, mx, stc, stm));
                        }
                    }
                    i += 4;
                }
            }
            off += want;
        }
    });
    if rows.is_empty() {
        log::line("[probe] hp: no param block found (are you in-world?)");
        return;
    }
    // Drop "constant clusters": false positives where many candidates share an identical
    // HP max (e.g. 48128 render structs). A real character's HP max is near-unique. Then the
    // player is the largest-HP survivor whose stamina looks like a real pool (cur<=max, round).
    let dup_max: std::collections::HashMap<u32, usize> =
        rows.iter().fold(std::collections::HashMap::new(), |mut m, r| {
            *m.entry(r.3).or_insert(0) += 1;
            m
        });
    // Score: not part of a constant cluster (HP max appears <3x) + prefer the live combat
    // mirror (recoverable below max) over a stale resting copy (rec==max, full stamina).
    let score = |r: &(usize, u32, u32, u32, f32, f32)| -> (i32, i32, i32, i32, u32) {
        let plausible = if r.3 > 0 && r.3 < 60_000 { 1 } else { 0 };
        let not_cluster = if dup_max.get(&r.3).copied().unwrap_or(0) < 3 { 1 } else { 0 };
        let rec_loss = if r.2 < r.3 { 1 } else { 0 };
        let st_full = if (r.4 - r.5).abs() < 0.5 { 1 } else { 0 };
        (plausible, not_cluster, rec_loss, st_full, r.3)
    };
    let player = rows.iter().max_by_key(|r| score(r)).map(|r| r.0);
    log::line(&format!("[probe] hp: {} candidate(s):", rows.len()));
    for (a, cur, rec, mx, stc, stm) in &rows {
        let tag = if Some(*a) == player { "  <- player?" } else { "" };
        log::line(&format!(
            "[probe]   @{:#010x}  HP {}/{} (rec {})  ST {:.0}/{:.0}{}",
            a, cur, mx, rec, stc, stm, tag
        ));
    }
    if let Some(p) = player {
        log::line(&format!(
            "[probe] hp: player param = {:#010x}  (HP cur @ {:#010x}, ST cur @ {:#010x})",
            p,
            p,
            p + 0x18
        ));
    }
}

/// entity <id> [maxhits]  -- enumerate all component objects of one entity.
/// Every component of a character shares an entity id at +0x10 (with +0x14==1) and starts with
/// an in-image vtable at +0x00. Scanning for that signature lists the whole entity (param/HP
/// component, the command/skill "player" root, model, etc.) so we can pick the root by vtable.
fn cmd_entity(args: &[&str]) {
    let Some(id) = args.first().and_then(|s| parse_uint(s)).map(|v| v as u32) else {
        log::line("[probe] entity: usage: entity <id> [maxhits]   (id = +0x10 value, e.g. from a known component)");
        return;
    };
    let max = args.get(1).and_then(|s| s.parse::<usize>().ok()).unwrap_or(64);
    log::line(&format!("[probe] entity: scanning for components with +0x10=={:#x}...", id));
    let mut rows: Vec<(usize, u32)> = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    let rd_u32 = |b: &[u8], o: usize| u32::from_le_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);
    for_each_region(|base, size, mbi| {
        if rows.len() >= max || !is_object_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && rows.len() < max {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 0x18 <= want && rows.len() < max {
                    if rd_u32(buf, i + 0x10) == id && rd_u32(buf, i + 0x14) == 1 {
                        let vt = rd_u32(buf, i);
                        if (0x0040_0000..0x0200_0000).contains(&vt) && vt & 3 == 0 {
                            rows.push((base + off + i, vt));
                        }
                    }
                    i += 4;
                }
            }
            off += if want > 0x18 { want - 0x18 } else { want };
        }
    });
    log::line(&format!("[probe] entity {:#x}: {} component(s):", id, rows.len()));
    for (a, vt) in &rows {
        log::line(&format!("[probe]   obj @{:#010x}  vtable {:#010x}", a, vt));
    }
}

/// findcmd <id24>  -- scan for the entity's command actor: an object whose +0x10 low-24-bits ==
/// id24 AND which holds a valid heap pointer at +0x1b2c (the command manager). The char-component
/// scan misses it because the cExActor has +0x14 != 1 and a tagged +0x10 high byte.
fn cmd_findcmd(args: &[&str]) {
    let Some(id) = args.first().and_then(|s| parse_uint(s)).map(|v| (v as u32) & 0x00ff_ffff) else {
        log::line("[probe] findcmd: usage: findcmd <id24>   (e.g. findcmd 0xf4c92)");
        return;
    };
    let max = args.get(1).and_then(|s| s.parse::<usize>().ok()).unwrap_or(32);
    log::line(&format!("[probe] findcmd: scanning for cmd-actor with (+0x10 & 0xffffff)=={:#x} and valid +0x1b2c...", id));
    let mut hits = 0usize;
    let mut chunk = vec![0u8; CHUNK];
    let rd_u32 = |b: &[u8], o: usize| u32::from_le_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);
    let is_heap_ptr = |v: u32| (0x0080_0000..0x4000_0000).contains(&v) && v & 3 == 0;
    for_each_region(|base, size, mbi| {
        if hits >= max || !is_object_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && hits < max {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 0x18 <= want && hits < max {
                    let vt = rd_u32(buf, i);
                    if (0x0040_0000..0x0200_0000).contains(&vt)
                        && vt & 3 == 0
                        && rd_u32(buf, i + 0x10) & 0x00ff_ffff == id
                    {
                        let obj = base + off + i;
                        let mut mb = [0u8; 4];
                        let mut fb = [0u8; 4];
                        let mgr = if safe_read(obj + 0x1b2c, &mut mb) {
                            u32::from_le_bytes(mb)
                        } else {
                            0
                        };
                        if is_heap_ptr(mgr) {
                            let flags = if safe_read(obj + 0x1bf0, &mut fb) {
                                u32::from_le_bytes(fb)
                            } else {
                                0
                            };
                            log::line(&format!(
                                "[probe]   cmd-actor @{:#010x}  vt {:#010x}  +0x10={:#x}  mgr(+0x1b2c)={:#010x}  flags(+0x1bf0)={:#x}",
                                obj, vt, rd_u32(buf, i + 0x10), mgr, flags
                            ));
                            hits += 1;
                        }
                    }
                    i += 4;
                }
            }
            off += if want > 0x18 { want - 0x18 } else { want };
        }
    });
    log::line(&format!("[probe] findcmd {:#x}: {} cmd-actor(s)", id, hits));
}

/// findexactor <id24>  -- scan for cExActor (vtable 0x01d6e8b0) whose entity id low-24 == id24.
/// This is the object execCommand runs on (distinct from findcmd hits which may be sibling types).
fn cmd_findexactor(args: &[&str]) {
    let Some(id) = args.first().and_then(|s| parse_uint(s)).map(|v| (v as u32) & 0x00ff_ffff) else {
        log::line("[probe] findexactor: usage: findexactor <id24>");
        return;
    };
    const VT: u32 = 0x01d6_e8b0;
    let max = args.get(1).and_then(|s| s.parse::<usize>().ok()).unwrap_or(16);
    log::line(&format!(
        "[probe] findexactor: scanning vt {:#010x} (+0x10 & 0xffffff)=={:#x}...",
        VT, id
    ));
    let mut hits = 0usize;
    let mut chunk = vec![0u8; CHUNK];
    let rd_u32 = |b: &[u8], o: usize| u32::from_le_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);
    let is_heap_ptr = |v: u32| (0x0080_0000..0x4000_0000).contains(&v) && v & 3 == 0;
    for_each_region(|base, size, mbi| {
        if hits >= max || !is_object_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && hits < max {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 0x18 <= want && hits < max {
                    if rd_u32(buf, i) == VT && rd_u32(buf, i + 0x10) & 0x00ff_ffff == id {
                        let obj = base + off + i;
                        let mut mb = [0u8; 4];
                        let mgr = if safe_read(obj + 0x1b2c, &mut mb) {
                            u32::from_le_bytes(mb)
                        } else {
                            0
                        };
                        log::line(&format!(
                            "[probe]   ex-actor @{:#010x}  +0x10={:#x}  mgr(+0x1b2c)={:#010x}",
                            obj, rd_u32(buf, i + 0x10), mgr
                        ));
                        hits += 1;
                    }
                    i += 4;
                }
            }
            off += if want > 0x18 { want - 0x18 } else { want };
        }
    });
    log::line(&format!("[probe] findexactor {:#x}: {} ex-actor(s)", id, hits));
}

/// cast <slot1-8>  -- queue a palette slot cast (executed on the input thread via hook).
fn cmd_cast(args: &[&str]) {
    let Some(slot) = args.first().and_then(|s| s.parse::<u8>().ok()) else {
        log::line("[probe] cast: usage: cast <slot1-8>");
        return;
    };
    if crate::skill::queue_slot(slot).is_ok() {
        log::line(&format!("[probe] cast slot{} queued (input-thread hook)", slot));
    } else {
        log::line("[probe] cast: failed (no player ctx / bad slot entry)");
    }
}

/// weap  -- dump weapon-drawn / skill-input gate flags (sheath vs draw live test).
fn cmd_weap(_args: &[&str]) {
    crate::skill::log_weapon_state();
}

/// keycfg  -- dump keyconfig entry pointers (slot1..8 @ +0x264..).
fn cmd_keycfg(_args: &[&str]) {
    crate::skill::log_keyconfig();
}

/// queuecmd <actor> <idx> <cmd_id>  -- queue a skill via SetCommand request table (pure write).
/// Writes flag=1, d330=0, d334=cmd_id, d338=0 into slot <idx> of actor's command manager.
fn cmd_queuecmd(args: &[&str]) {
    let Some(actor) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] queuecmd: usage: queuecmd <actor> <idx> <cmd_id>");
        return;
    };
    let idx = args.get(1).and_then(|s| parse_uint(s)).unwrap_or(0);
    let cmd_id = args.get(2).and_then(|s| parse_uint(s)).unwrap_or(0);
    let a = format!("{:#x}", actor);
    let i = format!("{:#x}", idx);
    let c = format!("{:#x}", cmd_id);
    cmd_setcmd(&[&a, &i, "1", "0", &c, "0"]);
}

/// call <func> [ecx] [arg0 arg1 ...]  -- invoke a game function via a self-allocated thunk
/// (no code patching). `ecx` (0 = none) goes in ECX for thiscall; stack args are pushed
/// right-to-left; callee assumed to self-clean (`ret N`, stdcall/thiscall). Returns EAX.
/// Floats are passed as raw IEEE-754 bits, e.g. 0xbf800000 = -1.0, 0x3f800000 = 1.0.
fn cmd_call(args: &[&str]) {
    let Some(func) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] call: usage: call <func> [ecx] [arg0 arg1 ...]");
        return;
    };
    let ecx = args.get(1).and_then(|s| parse_uint(s)).unwrap_or(0);
    let stack: Vec<u32> = args
        .get(2..)
        .unwrap_or(&[])
        .iter()
        .filter_map(|s| parse_uint(s))
        .map(|v| v as u32)
        .collect();
    log::line(&format!(
        "[probe] call {:#010x} ecx={:#010x} args={:#x?}",
        func, ecx, stack
    ));
    let r = unsafe { crate::call::raw_call(func as u32, ecx as u32, &stack, 0) };
    log::line(&format!("[probe] call {:#010x} -> eax={:#010x} ({})", func, r, r));
}

/// resolveplayer  -- call the local-player getter (node hardcoded @0x00c15360) to obtain the
/// live local player object, then dump its command slots at +0xec0 (stride 0x140, u16 id @+4).
fn cmd_resolveplayer() {
    let getter: u32 = 0x00c1_5360;
    log::line(&format!("[probe] resolveplayer: calling getter {:#010x}()", getter));
    let p = unsafe { crate::call::raw_call(getter, 0, &[], 0) };
    log::line(&format!("[probe] resolveplayer: player = {:#010x}", p));
    if !(0x0080_0000..0x7000_0000).contains(&p) {
        log::line("[probe] resolveplayer: not a plausible heap pointer (call may have failed)");
        return;
    }
    if let Some(vt) = read_u32(p as usize) {
        log::line(&format!("[probe]   player vtable = {:#010x}", vt));
    }
    for i in 0..8u32 {
        let slot = p as usize + 0xec0 + (i as usize) * 0x140;
        match read_u32(slot + 4) {
            Some(v) => log::line(&format!(
                "[probe]   slot[{}] @{:#010x} id={} ({:#06x})",
                i,
                slot,
                v & 0xffff,
                v & 0xffff
            )),
            None => log::line(&format!("[probe]   slot[{}] @{:#010x} unreadable", i, slot)),
        }
    }
}

/// dumpimage [path]  -- write the fully-unpacked main module image (code + data) from LIVE
/// memory to disk for offline RE in Ghidra/IDA. The packed on-disk DDO.exe is useless; this
/// captures the decrypted in-memory image. Section headers are rewritten to memory layout
/// (PointerToRawData=VirtualAddress, SizeOfRawData=VirtualSize) so a static tool loads it with
/// correct VAs (image base 0x400000) -- our anchor addresses line up exactly.
fn cmd_dumpimage(args: &[&str]) {
    let path = args.first().copied().unwrap_or("C:\\Users\\Public\\ddon_image.bin");
    let base = image_base();
    let rd_u32 = |addr: usize| -> Option<u32> {
        let mut b = [0u8; 4];
        if safe_read(addr, &mut b) {
            Some(u32::from_le_bytes(b))
        } else {
            None
        }
    };
    let rd_u16 = |addr: usize| -> Option<u16> {
        let mut b = [0u8; 2];
        if safe_read(addr, &mut b) {
            Some(u16::from_le_bytes(b))
        } else {
            None
        }
    };
    let Some(e_lfanew) = rd_u32(base + 0x3c) else {
        log::line("[probe] dumpimage: cannot read DOS header");
        return;
    };
    let nt = base + e_lfanew as usize;
    if rd_u32(nt) != Some(0x0000_4550) {
        log::line("[probe] dumpimage: PE signature not found");
        return;
    }
    let num_sections = rd_u16(nt + 0x6).unwrap_or(0) as usize;
    let size_opt = rd_u16(nt + 0x14).unwrap_or(0) as usize;
    let size_of_image = rd_u32(nt + 0x18 + 0x38).unwrap_or(0) as usize;
    if size_of_image == 0 || size_of_image > 0x4000_0000 {
        log::line(&format!("[probe] dumpimage: bad SizeOfImage {:#x}", size_of_image));
        return;
    }
    log::line(&format!(
        "[probe] dumpimage: base={:#010x} size={:#x} sections={} -> {}",
        base, size_of_image, num_sections, path
    ));
    let mut buf = vec![0u8; size_of_image];
    let mut read_ok = 0usize;
    let mut off = 0usize;
    while off < size_of_image {
        let n = core::cmp::min(0x1000, size_of_image - off);
        if safe_read(base + off, &mut buf[off..off + n]) {
            read_ok += n;
        }
        off += n;
    }
    // Rewrite section headers to memory layout so the file maps 1:1 to VAs.
    let sec_tbl = e_lfanew as usize + 0x18 + size_opt;
    for i in 0..num_sections {
        let sh = sec_tbl + i * 0x28;
        if sh + 0x28 > buf.len() {
            break;
        }
        let vsize = u32::from_le_bytes([buf[sh + 8], buf[sh + 9], buf[sh + 10], buf[sh + 11]]);
        let vaddr = u32::from_le_bytes([buf[sh + 12], buf[sh + 13], buf[sh + 14], buf[sh + 15]]);
        buf[sh + 0x10..sh + 0x14].copy_from_slice(&vsize.to_le_bytes()); // SizeOfRawData
        buf[sh + 0x14..sh + 0x18].copy_from_slice(&vaddr.to_le_bytes()); // PointerToRawData
    }
    match std::fs::write(path, &buf) {
        Ok(_) => log::line(&format!(
            "[probe] dumpimage: wrote {:#x} bytes ({:#x} readable) to {}",
            size_of_image, read_ok, path
        )),
        Err(e) => log::line(&format!("[probe] dumpimage: write failed: {}", e)),
    }
}

/// findactor [maxhits]  -- scan heap for the cExActor (execCommand `this`): a code-range vtable
/// whose methods at +0x98 and +0x100 are valid code, with 8 command slots at +0xec0 (stride
/// 0x140, u16 id at slot+4). Prints candidates + their slot ids (the live palette command ids).
fn cmd_findactor(args: &[&str]) {
    let max = args.first().and_then(|s| s.parse::<usize>().ok()).unwrap_or(16);
    let base = image_base();
    // Classify the two relevant sections by name so we only accept REAL vtables: a vtable lives
    // in .rdata and every method slot points into .text. (Marker dwords like 0x020007d0 live in
    // .data and fail this.)
    let (mut text_lo, mut text_hi, mut rd_lo, mut rd_hi) = (0usize, 0usize, 0usize, 0usize);
    {
        let e = read_u32(base + 0x3c).unwrap_or(0) as usize;
        let nsec = {
            let mut b = [0u8; 2];
            safe_read(base + e + 6, &mut b);
            u16::from_le_bytes(b) as usize
        };
        let szopt = {
            let mut b = [0u8; 2];
            safe_read(base + e + 0x14, &mut b);
            u16::from_le_bytes(b) as usize
        };
        let sect = base + e + 0x18 + szopt;
        for i in 0..nsec {
            let sh = sect + i * 0x28;
            let mut nm = [0u8; 8];
            safe_read(sh, &mut nm);
            let vsz = read_u32(sh + 8).unwrap_or(0) as usize;
            let va = base + read_u32(sh + 0xc).unwrap_or(0) as usize;
            if &nm[..6] == b".text\0" && text_lo == 0 {
                text_lo = va;
                text_hi = va + vsz;
            } else if &nm[..7] == b".rdata\0" && rd_lo == 0 {
                rd_lo = va;
                rd_hi = va + vsz;
            }
        }
    }
    let in_text = |v: usize| v >= text_lo && v < text_hi;
    let in_rdata = |v: usize| v >= rd_lo && v < rd_hi;
    let rd_u16 = |a: usize| -> u16 {
        let mut b = [0u8; 2];
        if safe_read(a, &mut b) { u16::from_le_bytes(b) } else { 0 }
    };
    log::line(&format!(
        "[probe] findactor: scanning (.text {:#x}..{:#x} .rdata {:#x}..{:#x})...",
        text_lo, text_hi, rd_lo, rd_hi
    ));
    let mut found = 0usize;
    let mut chunk = vec![0u8; CHUNK];
    for_each_region(|rbase, size, mbi| {
        if found >= max || !is_object_region(mbi) {
            return;
        }
        // Need room for the whole actor (up to ~+0x1c00) from a candidate start.
        let mut off = 0usize;
        while off + 4 <= size && found < max {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(rbase + off, buf) {
                let mut i = 0usize;
                while i + 4 <= want && found < max {
                    let vt = u32::from_le_bytes([buf[i], buf[i + 1], buf[i + 2], buf[i + 3]]) as usize;
                    if vt & 3 == 0 && in_rdata(vt) {
                        let a = rbase + off + i;
                        // vtable methods used by execCommand must be real code in .text.
                        let m0 = read_u32(vt).unwrap_or(0) as usize;
                        let m98 = read_u32(vt + 0x98).unwrap_or(0) as usize;
                        let m100 = read_u32(vt + 0x100).unwrap_or(0) as usize;
                        if in_text(m0) && in_text(m98) && in_text(m100) {
                            // Check 8 command slots at +0xec0, stride 0x140, id @ slot+4.
                            let mut plausible = 0;
                            let mut ids = [0u16; 8];
                            for s in 0..8 {
                                let id = rd_u16(a + 0xec0 + s * 0x140 + 4);
                                ids[s] = id;
                                if id != 0 && id < 0x2000 {
                                    plausible += 1;
                                }
                            }
                            if plausible >= 4 {
                                found += 1;
                                let eid = read_u32(a + 0x10).unwrap_or(0);
                                log::line(&format!(
                                    "[probe]   actor? @{:#010x} vt {:#010x} eid {:#x} slots[{}]: {:?}",
                                    a, vt, eid, plausible, ids
                                ));
                            }
                        }
                    }
                    i += 4;
                }
            }
            off += if want > 4 { want - 4 } else { want };
        }
    });
    log::line(&format!("[probe] findactor: {} candidate(s)", found));
}

/// bp <addr> [w|rw] [len=4] [maxhits=1]   hardware data breakpoint to find accessing code.
fn cmd_bp(args: &[&str]) {
    let Some(addr) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] bp: usage: bp <addr> [w|rw] [len=1|2|4] [maxhits] [tid]");
        log::line("[probe]   tid omitted/0 => main game thread; use 'threads' to list");
        return;
    };
    let rw = match args.get(1).copied().unwrap_or("w") {
        "rw" | "r" => 3u8,
        _ => 1u8,
    };
    let len = args.get(2).and_then(|s| s.parse::<u8>().ok()).unwrap_or(4);
    let maxhits = args.get(3).and_then(|s| s.parse::<u32>().ok()).unwrap_or(1);
    let tid = args.get(4).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    crate::hwbp::arm(addr, rw, len, maxhits, tid);
}

/// gp <addr> [maxhits]  -- guard-page software breakpoint (no DR regs, no code patch).
/// Arms PAGE_GUARD on the page holding <addr>; the next game access logs the accessing
/// instruction + all registers (see [GP] lines), then re-arms up to maxhits times.
fn cmd_gp(args: &[&str]) {
    let Some(addr) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] gp: usage: gp <addr> [maxhits] [window_ms]");
        return;
    };
    let maxhits = args.get(1).and_then(|s| s.parse::<u32>().ok()).unwrap_or(8);
    let window_ms = args.get(2).and_then(|s| s.parse::<u64>().ok()).unwrap_or(4000);
    crate::guardpage::arm(addr, maxhits, window_ms);
}

/// catch <func> [window_ms]  -- one-shot: capture ECX (this/actor) + stack args at the entry of
/// <func> the next time it runs (e.g. cast a skill with catch on execCommand 0xc83f90).
fn cmd_catch(args: &[&str]) {
    let Some(func) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] catch: usage: catch <func> [window_ms]");
        return;
    };
    let window = args.get(1).and_then(|s| s.parse::<u64>().ok()).unwrap_or(20000);
    let mincmd = args.get(2).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    let entity = args.get(3).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    crate::guardpage::catch(func, window, mincmd, entity);
}

/// fire <cmd> [slot] [window_ms] [actor]  -- hijack the next execCommand on the game thread
/// and rewrite it to (actor, slot, cmd, force=1). If <actor> is given, ECX is forced to that
/// address (use the cmd-actor from `findcmd`). No off-thread call, no code patch.
fn cmd_fire(args: &[&str]) {
    let Some(cmd) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] fire: usage: fire <cmd> [slot] [window_ms] [actor]");
        return;
    };
    let slot = args.get(1).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    let window = args.get(2).and_then(|s| s.parse::<u64>().ok()).unwrap_or(8000);
    let actor = args.get(3).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    log::line(&format!(
        "[probe] fire: cmd={:#x} slot={} window={}ms actor={:#010x}",
        cmd, slot, window, actor
    ));
    crate::guardpage::fire(0x00c8_3f90, cmd as u32, slot, actor, window);
}

/// setcmd <actor> <idx> <flag> <d330> <d334> <d338>  -- write a SetCommand request entry
/// directly into the actor's command manager (pure memory write, safe off-thread).
fn cmd_setcmd(args: &[&str]) {
    let Some(actor) = args.first().and_then(|s| parse_uint(s)) else {
        log::line("[probe] setcmd: usage: setcmd <actor> <idx> <flag> <d330> <d334> <d338>");
        return;
    };
    let idx = args.get(1).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    let flag = args.get(2).and_then(|s| parse_uint(s)).unwrap_or(1) as u8;
    let d330 = args.get(3).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    let d334 = args.get(4).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    let d338 = args.get(5).and_then(|s| parse_uint(s)).unwrap_or(0) as u32;
    if idx >= 10 {
        log::line("[probe] setcmd: idx must be < 10");
        return;
    }
    let Some(mgr) = read_u32(actor + 0x1b2c) else {
        log::line(&format!("[probe] setcmd: unreadable mgr @ actor+0x1b2c ({:#010x})", actor));
        return;
    };
    let mgr = mgr as usize;
    let actor = actor as usize;
    if !(0x0080_0000..0x7000_0000).contains(&(mgr as u32)) {
        log::line(&format!("[probe] setcmd: mgr {:#010x} not a heap ptr", mgr));
        return;
    }
    let off = (idx * 0x18) as usize;
    let ok = safe_write(mgr + 0x32c + off, &[flag])
        && safe_write(mgr + 0x330 + off, &d330.to_le_bytes())
        && safe_write(mgr + 0x334 + off, &d334.to_le_bytes())
        && safe_write(mgr + 0x338 + off, &d338.to_le_bytes());
    log::line(&format!(
        "[probe] setcmd: actor={:#010x} mgr={:#010x} idx={} flag={} d330={:#x} d334={:#x} d338={:#x} ({})",
        actor, mgr, idx, flag, d330, d334, d338, if ok { "ok" } else { "WRITE FAIL" }
    ));
}

fn cmd_scan_actor() {
    let vt = image_base() + ACTOR_VTABLE_RVA;
    log::line(&format!("[probe] scan-actor: actor vtable {:#010x} scanning...", vt));
    let hits = find_dword(vt as u32, 256);
    let mut scored: Vec<(usize, f32, f32)> = hits
        .iter()
        .map(|&h| (h, read_f32(h + ACTOR_ATK_OFF).unwrap_or(0.0), read_f32(h + ACTOR_MATK_OFF).unwrap_or(0.0)))
        .collect();
    scored.sort_by(|a, b| b.1.partial_cmp(&a.1).unwrap_or(core::cmp::Ordering::Equal));
    log::line(&format!("[probe] scan-actor: {} actor instance(s)", scored.len()));
    for (h, atk, matk) in scored.iter().take(8) {
        log::line(&format!("[probe]   actor {:#010x}  atk={} matk={}", h, atk, matk));
    }
    if let Some((h, atk, matk)) = scored.first() {
        log::line(&format!(
            "[probe] LIKELY PLAYER actor={:#010x} atk={} matk={}",
            h, atk, matk
        ));
    }
}

/// Scan all private RW memory for dword values matching `pred`; cap the result set.
fn scan_all<F: Fn(u32) -> bool>(pred: F) -> Vec<(usize, u32)> {
    let mut hits = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    for_each_region(|base, size, mbi| {
        if hits.len() >= FIND_CAP || !is_object_region(mbi) {
            return;
        }
        let mut off = 0usize;
        while off < size && hits.len() < FIND_CAP {
            let want = core::cmp::min(CHUNK, size - off);
            let buf = &mut chunk[..want];
            if safe_read(base + off, buf) {
                let mut i = 0usize;
                while i + 4 <= want {
                    let v = u32::from_le_bytes([buf[i], buf[i + 1], buf[i + 2], buf[i + 3]]);
                    if pred(v) {
                        hits.push((base + off + i, v));
                        if hits.len() >= FIND_CAP {
                            break;
                        }
                    }
                    i += 4;
                }
            }
            off += want;
        }
    });
    hits
}

/// Build a predicate (and a label) for a typed value argument.
fn make_pred(ty: &str, valstr: &str, tol: Option<&str>) -> Option<(Box<dyn Fn(u32) -> bool>, String)> {
    match ty {
        "u32" | "i32" => {
            let target = if ty == "i32" {
                valstr.parse::<i32>().ok().map(|v| v as u32).or_else(|| parse_uint(valstr).map(|v| v as u32))?
            } else {
                valstr.parse::<u32>().ok().or_else(|| parse_uint(valstr).map(|v| v as u32))?
            };
            Some((Box::new(move |v| v == target), format!("{} {}", ty, target)))
        }
        "u16" => {
            let target = valstr.parse::<u16>().ok().or_else(|| parse_uint(valstr).map(|v| v as u16))?;
            Some((Box::new(move |v| (v & 0xFFFF) as u16 == target), format!("u16 {}", target)))
        }
        "f32" => {
            let target = valstr.parse::<f32>().ok()?;
            let tol = tol.and_then(|t| t.parse::<f32>().ok()).unwrap_or(0.5);
            Some((
                Box::new(move |v| {
                    let f = f32::from_bits(v);
                    f.is_finite() && (f - target).abs() <= tol
                }),
                format!("f32 {} (+-{})", target, tol),
            ))
        }
        _ => None,
    }
}

fn report_find(label: &str) {
    if let Ok(g) = FIND.lock() {
        log::line(&format!("[probe] {} -> {} match(es)", label, g.len()));
        for (a, v) in g.iter().take(24) {
            log::line(&format!("[probe]   {:#010x} = {:#010x} ({})", a, v, *v as i32));
        }
        if g.len() > 24 {
            log::line(&format!("[probe]   ... and {} more", g.len() - 24));
        }
    }
}

fn cmd_find(args: &[&str]) {
    let (Some(ty), Some(val)) = (args.first().copied(), args.get(1).copied()) else {
        log::line("[probe] find: usage: find <u32|i32|u16|f32> <value> [f32-tol]");
        return;
    };
    let Some((pred, label)) = make_pred(ty, val, args.get(2).copied()) else {
        log::line(&format!("[probe] find: bad type/value '{} {}'", ty, val));
        return;
    };
    log::line(&format!("[probe] find {} scanning...", label));
    let hits = scan_all(pred);
    if let Ok(mut g) = FIND.lock() {
        *g = hits;
    }
    report_find(&format!("find {}", label));
}

/// Re-scan the existing result set, keeping addresses that now match the new value.
fn cmd_next(args: &[&str]) {
    let (Some(ty), Some(val)) = (args.first().copied(), args.get(1).copied()) else {
        log::line("[probe] next: usage: next <u32|i32|u16|f32> <value> [f32-tol]");
        return;
    };
    let Some((pred, label)) = make_pred(ty, val, args.get(2).copied()) else {
        log::line(&format!("[probe] next: bad type/value '{} {}'", ty, val));
        return;
    };
    if let Ok(mut g) = FIND.lock() {
        g.retain(|(a, _)| read_u32(*a).map(|v| pred(v)).unwrap_or(false));
        for e in g.iter_mut() {
            if let Some(v) = read_u32(e.0) {
                e.1 = v;
            }
        }
    }
    report_find(&format!("next {}", label));
}

/// Refine the result set by whether each address changed/kept its value since last scan.
fn cmd_filter_change(keep_changed: bool) {
    if let Ok(mut g) = FIND.lock() {
        g.retain(|(a, old)| {
            match read_u32(*a) {
                Some(v) => (v != *old) == keep_changed,
                None => false,
            }
        });
        for e in g.iter_mut() {
            if let Some(v) = read_u32(e.0) {
                e.1 = v;
            }
        }
    }
    report_find(if keep_changed { "changed" } else { "unchanged" });
}

fn cmd_snap(args: &[&str]) {
    let (Some(addr), Some(len)) =
        (args.first().and_then(|s| parse_uint(s)), args.get(1).and_then(|s| parse_uint(s)))
    else {
        log::line("[probe] snap: usage: snap <addr> <len>");
        return;
    };
    let len = len.min(65536);
    let mut buf = vec![0u8; len];
    if !safe_read(addr, &mut buf) {
        log::line(&format!("[probe] snap {:#010x} unreadable", addr));
        return;
    }
    if let Ok(mut g) = SNAP.lock() {
        *g = Some((addr, buf));
        log::line(&format!("[probe] snap {:#010x} {} bytes captured", addr, len));
    }
}

fn cmd_diff() {
    let mut g = match SNAP.lock() {
        Ok(g) => g,
        Err(_) => return,
    };
    let Some((addr, ref old)) = *g else {
        log::line("[probe] diff: no snapshot (run 'snap <addr> <len>' first)");
        return;
    };
    let mut cur = vec![0u8; old.len()];
    if !safe_read(addr, &mut cur) {
        log::line(&format!("[probe] diff {:#010x} unreadable", addr));
        return;
    }
    let mut changes = 0;
    for i in 0..old.len() {
        if old[i] != cur[i] {
            if changes < 64 {
                log::line(&format!(
                    "[probe]   +{:#06x} ({:#010x}): {:#04x} -> {:#04x}",
                    i, addr + i, old[i], cur[i]
                ));
            }
            changes += 1;
        }
    }
    log::line(&format!("[probe] diff {:#010x}: {} byte(s) changed", addr, changes));
    *g = Some((addr, cur)); // rebase so successive diffs are incremental
}

fn help() {
    for l in [
        "[probe] commands:",
        "[probe]   help | modules",
        "[probe]   peek <addr> [u8|u16|u32|i32|f32|ptr]",
        "[probe]   poke <addr> <u8|u16|u32|i32|f32> <value>",
        "[probe]   dump <addr> <len>",
        "[probe]   find-vtable <addr> [maxhits]",
        "[probe]   xref <value> [maxhits]   (scan code for an operand constant)",
        "[probe]   callers <addr> [maxhits] (scan code for call/jmp to a function)",
        "[probe]   scan-actor | player",
        "[probe]   hp [maxhits]   (scan gauge objects, read HP/Stamina, find player)",
        "[probe]   threads        (list threads; main = earliest)",
        "[probe]   find <u32|i32|u16|f32> <value> [f32-tol]",
        "[probe]   next <u32|i32|u16|f32> <value> [f32-tol] | changed | unchanged",
        "[probe]   snap <addr> <len> | diff",
        "[probe]   bp <addr> [w|rw] [len] [maxhits] [tid] | bpoff   (hardware data breakpoint)",
        "[probe]   gp <addr> [maxhits] | gpoff   (guard-page software breakpoint, anti-tamper safe)",
        "[probe]   entity <id> [maxhits]   (list all component objects sharing entity id @+0x10)",
        "[probe]   keycfg            (dump keyconfig entry ptrs for slot1..8)",
        "[probe]   weap              (weapon-drawn / skill-input gate flags)",
        "[probe]   cast <slot1-8>    (queue custom-skill cast via input-thread hook)",
        "[probe]   dumpimage [path]   (dump unpacked code+data image for Ghidra/IDA)",
    ] {
        log::line(l);
    }
}

fn run_line(line: &str) {
    let line = line.trim();
    if line.is_empty() || line.starts_with('#') {
        return;
    }
    log::line(&format!("[cmd] {}", line));
    let parts: Vec<&str> = line.split_whitespace().collect();
    let args = &parts[1..];
    match parts[0] {
        "help" => help(),
        "modules" => cmd_modules(),
        "peek" => cmd_peek(args),
        "poke" => cmd_poke(args),
        "dump" => cmd_dump(args),
        "find-vtable" => cmd_find_vtable(args),
        "scan-actor" => cmd_scan_actor(),
        "player" => cmd_player(),
        "find" => cmd_find(args),
        "next" => cmd_next(args),
        "changed" => cmd_filter_change(true),
        "unchanged" => cmd_filter_change(false),
        "snap" => cmd_snap(args),
        "diff" => cmd_diff(),
        "hp" => cmd_hp(args),
        "xref" => cmd_xref(args),
        "callers" => cmd_callers(args),
        "bp" => cmd_bp(args),
        "bpoff" => crate::hwbp::disarm(),
        "gp" => cmd_gp(args),
        "catch" => cmd_catch(args),
        "fire" => cmd_fire(args),
        "setcmd" => cmd_setcmd(args),
        "gpoff" => crate::guardpage::disarm(),
        "entity" => cmd_entity(args),
        "findcmd" => cmd_findcmd(args),
        "findexactor" => cmd_findexactor(args),
        "queuecmd" => cmd_queuecmd(args),
        "cast" => cmd_cast(args),
        "keycfg" => cmd_keycfg(args),
        "weap" => cmd_weap(args),
        "call" => cmd_call(args),
        "resolveplayer" => cmd_resolveplayer(),
        "dumpimage" => cmd_dumpimage(args),
        "findactor" => cmd_findactor(args),
        "threads" => crate::hwbp::list_threads(),
        other => log::line(&format!("[probe] unknown command '{}' (try 'help')", other)),
    }
}

fn probe_loop() {
    log::line(&format!("[probe] command channel ready: {}", CMD_PATH));
    help();
    // Seed last_mtime with the file's CURRENT state so we never execute stale commands
    // left over from a previous session at startup (a stale 'bpoff'/'bp' running during
    // DLL init crashed the game). We only act on writes that happen after we're loaded.
    let mut last_mtime: Option<std::time::SystemTime> =
        std::fs::metadata(CMD_PATH).ok().and_then(|m| m.modified().ok());
    loop {
        std::thread::sleep(std::time::Duration::from_millis(300));
        let meta = match std::fs::metadata(CMD_PATH) {
            Ok(m) => m,
            Err(_) => continue,
        };
        let mtime = meta.modified().ok();
        if mtime == last_mtime {
            continue;
        }
        last_mtime = mtime;
        // A newer injection has superseded us -> stand down (don't double-run commands).
        if !is_latest() {
            static STALE_DISARMED: AtomicBool = AtomicBool::new(false);
            if !STALE_DISARMED.swap(true, Ordering::Relaxed) {
                crate::guardpage::disarm();
            }
            continue;
        }
        if let Ok(text) = std::fs::read_to_string(CMD_PATH) {
            for line in text.lines() {
                run_line(line);
            }
        }
    }
}

pub fn spawn() {
    let g = register_generation();
    std::thread::spawn(move || {
        log::line(&format!(
            "[probe] generation {} active (older injections now inert)",
            g
        ));
        probe_loop();
    });
}

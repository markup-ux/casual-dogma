//! Pin recoverable HP (gray / white bar) while below max job level.
//!
//! Combat keeps several HP param mirrors (same max, different cur/rec). Pinning only the
//! "resting" mirror (full stamina / rec==max) misses the live combat copy. We pin **every**
//! plausible param block at the player's max HP, block the char+0x7f0 store @ DDO+0xd2d624,
//! and rewrite every input frame.

use core::ffi::c_void;
use core::sync::atomic::{AtomicBool, AtomicU8, AtomicU32, AtomicUsize, Ordering};

use windows_sys::Win32::System::Diagnostics::Debug::{ReadProcessMemory, WriteProcessMemory};
use windows_sys::Win32::System::LibraryLoader::GetModuleHandleW;
use windows_sys::Win32::System::Memory::{VirtualQuery, MEMORY_BASIC_INFORMATION};
use windows_sys::Win32::System::Threading::GetCurrentProcess;

use crate::log;
use crate::probe;

const SIGNAL_PATH: &str = "D:\\DDON\\client-mod\\sync\\recoverable_hp_state.json";
const CHAR_VTABLE_RVA: usize = 0x01d3_d3a0;
const OFF_HP_CUR: usize = 0x7e8;
const OFF_HP_REC: usize = 0x7f0;
const OFF_HP_MAX: usize = 0x7f8;
const MAX_PIN_TARGETS: usize = 8;

const SCAN_MAX_ADDR: usize = 0x7FFF_0000;
const CHUNK: usize = 1 << 20;
const MEM_COMMIT_SCAN: u32 = 0x1000;
const MEM_PRIVATE: u32 = 0x20000;
const PAGE_READWRITE: u32 = 0x04;
const PAGE_EXECUTE_READWRITE_SCAN: u32 = 0x40;

static PIN_FLAG: AtomicU8 = AtomicU8::new(0);
static PIN_COUNT: AtomicUsize = AtomicUsize::new(0);
static PIN_LOGGED: AtomicBool = AtomicBool::new(false);
static REPIN_COUNT: AtomicU32 = AtomicU32::new(0);
static PIN_TARGETS: [AtomicUsize; MAX_PIN_TARGETS] = [const { AtomicUsize::new(0) }; MAX_PIN_TARGETS];

fn image_base() -> usize {
    unsafe { GetModuleHandleW(core::ptr::null()) as usize }
}

fn pin_requested() -> bool {
    let Ok(text) = std::fs::read_to_string(SIGNAL_PATH) else {
        return false;
    };
    let lower = text.to_lowercase();
    lower.contains("\"pinrecoverablehp\": true") || lower.contains("\"pinrecoverablehp\":true")
}

fn read_u32(addr: usize) -> Option<u32> {
    let mut b = [0u8; 4];
    unsafe {
        let mut got = 0usize;
        if ReadProcessMemory(
            GetCurrentProcess(),
            addr as *const c_void,
            b.as_mut_ptr() as *mut c_void,
            4,
            &mut got,
        ) == 0
            || got != 4
        {
            return None;
        }
    }
    Some(u32::from_le_bytes(b))
}

fn write_u32(addr: usize, val: u32) -> bool {
    let bytes = val.to_le_bytes();
    unsafe {
        let mut put = 0usize;
        WriteProcessMemory(
            GetCurrentProcess(),
            addr as *mut c_void,
            bytes.as_ptr() as *const c_void,
            4,
            &mut put,
        ) != 0
            && put == 4
    }
}

fn expected_char_vtable() -> u32 {
    image_base().wrapping_add(CHAR_VTABLE_RVA) as u32
}

fn is_object_region(mbi: &MEMORY_BASIC_INFORMATION) -> bool {
    mbi.State == MEM_COMMIT_SCAN
        && mbi.Type == MEM_PRIVATE
        && (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_EXECUTE_READWRITE_SCAN)
}

fn scan_hp_param_rows() -> Vec<(usize, u32, u32, u32, f32, f32)> {
    let mut rows: Vec<(usize, u32, u32, u32, f32, f32)> = Vec::new();
    let mut chunk = vec![0u8; CHUNK];
    let rd_u32 = |b: &[u8], o: usize| u32::from_le_bytes([b[o], b[o + 1], b[o + 2], b[o + 3]]);

    let mut addr: usize = 0;
    while addr < SCAN_MAX_ADDR && rows.len() < 64 {
        let mut mbi = MEMORY_BASIC_INFORMATION {
            BaseAddress: core::ptr::null_mut(),
            AllocationBase: core::ptr::null_mut(),
            AllocationProtect: 0,
            RegionSize: 0,
            State: 0,
            Protect: 0,
            Type: 0,
        };
        unsafe {
            if VirtualQuery(addr as *const c_void, &mut mbi, core::mem::size_of::<MEMORY_BASIC_INFORMATION>())
                == 0
            {
                break;
            }
        }
        let base = mbi.BaseAddress as usize;
        let size = mbi.RegionSize;
        if is_object_region(&mbi) {
            let mut off = 0usize;
            while off < size && rows.len() < 64 {
                let want = core::cmp::min(CHUNK, size - off);
                let buf = &mut chunk[..want];
                if probe::safe_read_pub(base + off, buf) {
                    let mut i = 0usize;
                    while i + 0x20 <= want && rows.len() < 64 {
                        if rd_u32(buf, i + 4) == 0
                            && rd_u32(buf, i + 0xc) == 0
                            && rd_u32(buf, i + 0x14) == 0
                        {
                            let cur = rd_u32(buf, i);
                            let rec = rd_u32(buf, i + 8);
                            let mx = rd_u32(buf, i + 0x10);
                            let stc = f32::from_bits(rd_u32(buf, i + 0x18));
                            let stm = f32::from_bits(rd_u32(buf, i + 0x1c));
                            let hp_ok =
                                (1..=60_000).contains(&cur) && cur <= rec && rec <= mx && mx < 60_000;
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
        }
        addr = base.saturating_add(size);
    }
    rows
}

/// Same param scan as probe `hp`, but return every non-cluster block at the player's max HP.
fn discover_pin_params() -> Vec<usize> {
    let rows = scan_hp_param_rows();
    if rows.is_empty() {
        return Vec::new();
    }

    let dup_max: std::collections::HashMap<u32, usize> =
        rows.iter().fold(std::collections::HashMap::new(), |mut m, r| {
            *m.entry(r.3).or_insert(0) += 1;
            m
        });

    let player_max = rows
        .iter()
        .filter(|r| dup_max.get(&r.3).copied().unwrap_or(0) < 3 && r.3 < 60_000)
        .map(|r| r.3)
        .max()
        .unwrap_or(0);
    if player_max == 0 {
        return Vec::new();
    }

    let mut out: Vec<usize> = rows
        .iter()
        .filter(|r| r.3 == player_max && dup_max.get(&r.3).copied().unwrap_or(0) < 3)
        .map(|r| r.0)
        .collect();
    out.sort_unstable();
    out.dedup();

    // Prefer mirrors that currently show recoverable loss (combat copy) first in the cache.
    out.sort_by_key(|p| {
        let rec = read_u32(*p + 8).unwrap_or(0);
        let mx = read_u32(*p + 0x10).unwrap_or(0);
        u32::MAX - (mx.saturating_sub(rec))
    });

    out.truncate(MAX_PIN_TARGETS);
    out
}

fn valid_param(param: usize) -> bool {
    let cur = read_u32(param).unwrap_or(0);
    let rec = read_u32(param + 8).unwrap_or(0);
    let mx = read_u32(param + 0x10).unwrap_or(0);
    cur > 0 && (1..60_000).contains(&mx) && cur <= rec && rec <= mx
}

fn pin_param_block(param: usize) -> bool {
    let cur = read_u32(param).unwrap_or(0);
    let mx = read_u32(param + 0x10).unwrap_or(0);
    if cur == 0 || mx == 0 {
        return false;
    }
    let rec = read_u32(param + 8).unwrap_or(0);
    if rec >= mx {
        return false;
    }

    let mut ok = write_u32(param + 8, mx);

    let ch = param.wrapping_sub(OFF_HP_CUR);
    if read_u32(ch).unwrap_or(0) == expected_char_vtable() {
        ok |= write_u32(ch + OFF_HP_REC, mx);
    }

    if ok {
        REPIN_COUNT.fetch_add(1, Ordering::Relaxed);
        if !PIN_LOGGED.swap(true, Ordering::Relaxed) {
            log::line(&format!(
                "[recoverable_hp] pin param @{:#010x} rec {} -> max {} (cur {})",
                param, rec, mx, cur
            ));
        }
    }
    ok
}

fn pin_all_cached() {
    let n = PIN_COUNT.load(Ordering::Relaxed);
    for i in 0..n.min(MAX_PIN_TARGETS) {
        let param = PIN_TARGETS[i].load(Ordering::Relaxed);
        if param != 0 && valid_param(param) {
            let _ = pin_param_block(param);
        }
    }
}

fn store_pin_targets(params: &[usize]) {
    PIN_COUNT.store(params.len().min(MAX_PIN_TARGETS), Ordering::Relaxed);
    for i in 0..MAX_PIN_TARGETS {
        let v = params.get(i).copied().unwrap_or(0);
        PIN_TARGETS[i].store(v, Ordering::Relaxed);
    }
}

fn refresh_signal_and_cache() {
    let want = pin_requested();
    let was = PIN_FLAG.swap(if want { 1 } else { 0 }, Ordering::Relaxed) != 0;
    if !want {
        if was {
            store_pin_targets(&[]);
            PIN_LOGGED.store(false, Ordering::Relaxed);
            log::line("[recoverable_hp] pin cleared (signal off)");
        }
        return;
    }

    let mut need_rescan = PIN_COUNT.load(Ordering::Relaxed) == 0;
    if !need_rescan {
        let n = PIN_COUNT.load(Ordering::Relaxed);
        need_rescan = (0..n.min(MAX_PIN_TARGETS)).all(|i| {
            let p = PIN_TARGETS[i].load(Ordering::Relaxed);
            p == 0 || !valid_param(p)
        });
    }

    if need_rescan {
        let params = discover_pin_params();
        if !params.is_empty() {
            store_pin_targets(&params);
            PIN_LOGGED.store(false, Ordering::Relaxed);
            let mut parts = String::new();
            for (i, p) in params.iter().enumerate() {
                let cur = read_u32(*p).unwrap_or(0);
                let rec = read_u32(*p + 8).unwrap_or(0);
                let mx = read_u32(*p + 0x10).unwrap_or(0);
                if i > 0 {
                    parts.push_str(", ");
                }
                parts.push_str(&format!("{:#010x} {}/{}/{}", p, cur, rec, mx));
            }
            log::line(&format!(
                "[recoverable_hp] cached {} param mirror(s): {}",
                params.len(),
                parts
            ));
        }
    }

    pin_all_cached();
}

/// Called every input frame from the game thread — must stay allocation-free.
pub fn on_input_frame() {
    if PIN_FLAG.load(Ordering::Relaxed) == 0 {
        return;
    }
    pin_all_cached();
}

pub fn install_hooks() {
    PIN_FLAG.store(if pin_requested() { 1 } else { 0 }, Ordering::Relaxed);
    // Intentionally no code patch here — inline hooks on the recoverable store have crashed DDO.
    log::line("[recoverable_hp] using frame pin only (no code hook)");
}

pub fn spawn() {
    std::thread::spawn(|| {
        log::line(&format!(
            "[recoverable_hp] watcher started (multi-mirror pin, signal: {})",
            SIGNAL_PATH
        ));
        let mut last_repin = 0u32;
        refresh_signal_and_cache();
        loop {
            std::thread::sleep(std::time::Duration::from_millis(100));
            if !probe::is_latest() {
                continue;
            }
            refresh_signal_and_cache();

            let repins = REPIN_COUNT.load(Ordering::Relaxed);
            if repins != last_repin && repins > 0 && repins % 20 == 0 {
                log::line(&format!("[recoverable_hp] repins={repins}"));
            }
            last_repin = repins;
        }
    });
}

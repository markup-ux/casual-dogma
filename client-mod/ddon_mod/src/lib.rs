//! ddon_mod - DDON in-process client mod.
//!
//! Current stage: diagnostics foundation. On injection it arms crash diagnostics,
//! logs a session header, loads the key/controller bindings, and runs a heartbeat so
//! the host monitor can tell "alive" from "hung" from "crashed". Skill-binding hooks
//! are layered on top of this in later stages.

mod bind;
mod call;
mod config;
mod diag;
mod guardpage;
mod hooks;
mod hwbp;
mod log;
mod probe;
mod recoverable_hp;
mod skill;

use core::ffi::c_void;

use windows_sys::Win32::Foundation::{BOOL, HMODULE, TRUE};
use windows_sys::Win32::System::LibraryLoader::GetModuleHandleW;
use windows_sys::Win32::System::SystemServices::DLL_PROCESS_ATTACH;
use windows_sys::Win32::System::Threading::GetCurrentProcessId;

const VERSION: &str = env!("CARGO_PKG_VERSION");

fn session_header() {
    unsafe {
        let image_base = GetModuleHandleW(core::ptr::null()) as usize;
        let pid = GetCurrentProcessId();
        log::line("============================================================");
        log::line(&format!(
            "[ddon_mod] v{} loaded | pid={} | DDO.exe image_base={:#010x}",
            VERSION, pid, image_base
        ));
        log::line(&format!("[ddon_mod] log: {}", log::LOG_PATH));
    }
}

fn init() {
    // Diagnostics must be armed before anything risky runs.
    diag::arm();
    session_header();

    // Input-thread hook drains queued casts; swap hook captures player for probe.
    hooks::install_all();
    recoverable_hp::install_hooks();

    let bindings = config::load();
    log::line(&format!(
        "[ddon_mod] bindings loaded: {} entries (file: {})",
        bindings.map.len(),
        config::CONFIG_PATH
    ));

    // Live RE workbench: file-driven memory probe (peek/poke/scan/diff).
    probe::spawn();

    // Keep recoverable HP pinned while the server signal is active.
    recoverable_hp::spawn();

    // Bind loop: GetAsyncKeyState -> queue_slot -> input-thread hook drains casts.
    bind::spawn();

    // Heartbeat: lets the monitor distinguish a hang (heartbeat stops, process alive)
    // from a clean run. Kept infrequent to stay out of the way.
    let mut tick: u64 = 0;
    loop {
        std::thread::sleep(std::time::Duration::from_secs(15));
        // Stale (superseded) injections stand down quietly so logs stay single-voiced.
        if !probe::is_latest() {
            continue;
        }
        tick = tick.wrapping_add(1);
        log::line(&format!("[hb] tick={} alive", tick));
    }
}

#[no_mangle]
pub extern "system" fn DllMain(_h: HMODULE, reason: u32, _r: *mut c_void) -> BOOL {
    if reason == DLL_PROCESS_ATTACH {
        std::thread::spawn(init);
    }
    TRUE
}

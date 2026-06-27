//! Minimal, dependency-free file logger. Output: C:\Users\Public\ddon_mod.log
//!
//! This runs inside the game process, so it is kept panic-safe and cheap. Two entry
//! points exist:
//!   * `line`     - normal, mutex-serialized logging.
//!   * `raw_line` - used from crash handlers; it does NOT take the global lock, because
//!                  the crash may have happened while the lock was held (avoids deadlock).

use std::fs::OpenOptions;
use std::io::Write;
use std::sync::Mutex;

use windows_sys::Win32::Foundation::SYSTEMTIME;
use windows_sys::Win32::System::SystemInformation::GetLocalTime;

pub static LOG_PATH: &str = "C:\\Users\\Public\\ddon_mod.log";
static LOCK: Mutex<()> = Mutex::new(());

/// Local wall-clock timestamp `YYYY-MM-DD HH:MM:SS.mmm`.
pub fn ts() -> String {
    unsafe {
        let mut st: SYSTEMTIME = core::mem::zeroed();
        GetLocalTime(&mut st);
        format!(
            "{:04}-{:02}-{:02} {:02}:{:02}:{:02}.{:03}",
            st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds
        )
    }
}

fn write_raw(full: &str) {
    if let Ok(mut f) = OpenOptions::new().create(true).append(true).open(LOG_PATH) {
        let _ = writeln!(f, "{}", full);
        let _ = f.flush();
    }
}

/// Normal logging path (serialized).
pub fn line(s: &str) {
    let _g = LOCK.lock();
    write_raw(&format!("{} {}", ts(), s));
}

/// Crash-safe logging path: never blocks on the global lock.
pub fn raw_line(s: &str) {
    write_raw(&format!("{} {}", ts(), s));
}

#[macro_export]
macro_rules! logln {
    ($($arg:tt)*) => {
        $crate::log::line(&format!($($arg)*))
    };
}

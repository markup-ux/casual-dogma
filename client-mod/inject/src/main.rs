//! Minimal 32-bit DLL injector for DDO.exe (WOW64).
//! Usage: inject.exe <full\path\to\synchook.dll>

use std::ffi::CString;

use windows_sys::Win32::Foundation::{CloseHandle, FALSE, HANDLE, INVALID_HANDLE_VALUE};
use windows_sys::Win32::System::Diagnostics::ToolHelp::{
    CreateToolhelp32Snapshot, Process32First, Process32Next, PROCESSENTRY32, TH32CS_SNAPPROCESS,
};
use windows_sys::Win32::System::LibraryLoader::{GetModuleHandleA, GetProcAddress};
use windows_sys::Win32::System::Memory::{
    VirtualAllocEx, MEM_COMMIT, MEM_RESERVE, PAGE_READWRITE,
};
use windows_sys::Win32::System::Threading::{
    OpenProcess, PROCESS_CREATE_THREAD, PROCESS_QUERY_INFORMATION, PROCESS_VM_OPERATION,
    PROCESS_VM_READ, PROCESS_VM_WRITE,
};

const INFINITE: u32 = 0xFFFF_FFFF;

extern "system" {
    fn WriteProcessMemory(
        h: HANDLE,
        addr: *mut core::ffi::c_void,
        buf: *const core::ffi::c_void,
        size: usize,
        written: *mut usize,
    ) -> i32;
    fn CreateRemoteThread(
        h: HANDLE,
        sa: *const core::ffi::c_void,
        stack: usize,
        start: Option<extern "system" fn(*mut core::ffi::c_void) -> u32>,
        param: *mut core::ffi::c_void,
        flags: u32,
        tid: *mut u32,
    ) -> HANDLE;
    fn WaitForSingleObject(h: HANDLE, ms: u32) -> u32;
}

fn find_pid(name: &str) -> Option<u32> {
    unsafe {
        let snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if snap == INVALID_HANDLE_VALUE {
            return None;
        }
        let mut pe: PROCESSENTRY32 = core::mem::zeroed();
        pe.dwSize = core::mem::size_of::<PROCESSENTRY32>() as u32;
        let mut ok = Process32First(snap, &mut pe);
        while ok != FALSE {
            let exe = pe
                .szExeFile
                .iter()
                .take_while(|&&c| c != 0)
                .map(|&c| c as u8 as char)
                .collect::<String>();
            if exe.eq_ignore_ascii_case(name) {
                CloseHandle(snap);
                return Some(pe.th32ProcessID);
            }
            ok = Process32Next(snap, &mut pe);
        }
        CloseHandle(snap);
        None
    }
}

fn main() {
    let dll = std::env::args().nth(1).unwrap_or_else(|| {
        r"D:\DDON\client-mod\synchook\target\i686-pc-windows-msvc\release\synchook.dll".to_string()
    });
    println!("DLL: {dll}");
    let pid = match find_pid("DDO.exe") {
        Some(p) => p,
        None => {
            eprintln!("DDO.exe not running");
            std::process::exit(1);
        }
    };
    println!("PID: {pid}");
    unsafe {
        let h = OpenProcess(
            PROCESS_CREATE_THREAD
                | PROCESS_QUERY_INFORMATION
                | PROCESS_VM_OPERATION
                | PROCESS_VM_WRITE
                | PROCESS_VM_READ,
            FALSE,
            pid,
        );
        if h.is_null() {
            eprintln!("OpenProcess failed (run elevated)");
            std::process::exit(1);
        }
        let path = CString::new(dll).unwrap();
        let bytes = path.as_bytes_with_nul();
        let remote = VirtualAllocEx(
            h,
            core::ptr::null(),
            bytes.len(),
            MEM_COMMIT | MEM_RESERVE,
            PAGE_READWRITE,
        );
        if remote.is_null() {
            eprintln!("VirtualAllocEx failed");
            std::process::exit(1);
        }
        let mut written = 0usize;
        WriteProcessMemory(
            h,
            remote,
            bytes.as_ptr() as *const _,
            bytes.len(),
            &mut written,
        );
        let k32 = GetModuleHandleA(b"kernel32.dll\0".as_ptr());
        let load = GetProcAddress(k32, b"LoadLibraryA\0".as_ptr());
        let start: extern "system" fn(*mut core::ffi::c_void) -> u32 =
            core::mem::transmute(load);
        let th = CreateRemoteThread(
            h,
            core::ptr::null(),
            0,
            Some(start),
            remote,
            0,
            core::ptr::null_mut(),
        );
        if th.is_null() {
            eprintln!("CreateRemoteThread failed");
            std::process::exit(1);
        }
        WaitForSingleObject(th, INFINITE);
        println!("Injected. Check C:\\Users\\Public\\ddon_synchook.log");
        CloseHandle(th);
        CloseHandle(h);
    }
}

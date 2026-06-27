# Client-mod diagnostics & monitoring

Foundation for developing the skill-binding mod safely: the DLL self-reports crashes,
and a host-side monitor watches the whole session. Build this once, then layer the
actual skill hooks on top with full crash visibility.

## Pieces

| Piece | What it does |
|---|---|
| `ddon_mod` (DLL) | Injected into DDO.exe. Arms crash diagnostics, logs a session header, loads bindings, heartbeats. |
| `inject` (exe) | 32-bit DLL injector (`LoadLibrary` via `CreateRemoteThread`). |
| `build.ps1` | Builds `ddon_mod.dll` + `inject.exe` (release, `i686-pc-windows-msvc`). |
| `inject.ps1` | Builds if needed, self-elevates, injects the DLL into a running DDO.exe. |
| `monitor.ps1` | Live console: process lifetime/crashes, log tails, minidumps, OS error events. |

## In-process diagnostics (baked into the DLL)

Armed at the very start of `init()` in `ddon_mod/src/lib.rs`, before anything risky:

1. **Rust panic hook** - any panic in our code -> `[PANIC] at file:line : msg`.
2. **Vectored handler (first-chance)** - logs faults whose instruction pointer is
   inside `ddon_mod.dll` (i.e. our hook bugs), even if the game would swallow them:
   `[CRASH][first-chance in ddon_mod] ...`.
3. **Unhandled filter (last-chance)** - any genuine crash -> full report (code,
   faulting address resolved to `module+offset`, x86 registers) **plus a minidump**,
   then the process exits:
   `[CRASH] ... minidump written: C:\Users\Public\ddon_mod_crash_<pid>_<ts>.dmp`.

Crash-path logging uses an unlocked writer so it can't deadlock on the log mutex.

### Log files
- `C:\Users\Public\ddon_mod.log`     - this mod (timestamped).
- `C:\Users\Public\ddon_synchook.log` - the older param-capture hook (if used).
- `C:\Users\Public\ddon_mod_crash_*.dmp` - minidumps (open in WinDbg/VS).

## Typical workflow

```powershell
cd D:\DDON\client-mod

# 1. Start the monitor (leave running in its own window/job).
.\monitor.ps1

# 2. Launch DDON normally, then inject (elevated; inject.ps1 self-elevates):
.\inject.ps1
```

The monitor prints, with stable greppable prefixes:

```
[MON] ...            lifecycle (started / exited clean / watching)
[MON][CRASH] ...     DDO.exe exited with a non-zero/exception code (decoded)
[MON][DUMP] ...      a new minidump appeared
[MON][EVT] ...       Application event-log error mentioning DDO
[MOD]  / [SYNC] ...  live tail of the mod logs
[CRASH] / [PANIC]    emitted by the DLL itself (shown via [MOD])
```

### Useful switches
- `.\monitor.ps1 -FromStart` - replay existing log history, not just new lines.
- `.\monitor.ps1 -PollMs 500` - faster polling.
- `.\inject.ps1 -Dll <path>` - inject a different DLL (e.g. `synchook.dll`).
- `.\build.ps1 -Mod` / `-Inject` - build just one crate.

## Diagnosing a crash

1. `[MON][CRASH] ... exit=0x... (NAME)` tells you it died and how.
2. Look at the last `[MOD]` lines and any `[CRASH]`/`[PANIC]` for the in-mod report,
   including `eip -> module+offset` (was it our code or game code?).
3. Open the `.dmp` in WinDbg/Visual Studio for stacks, memory, and threads.

## Notes
- DDO.exe is 32-bit, so all crates build for `i686-pc-windows-msvc`.
- Injection requires elevation; `inject.ps1` handles that.
- If `cargo` fails on a TLS revocation error, the scripts already set
  `CARGO_HTTP_CHECK_REVOKE=false`.

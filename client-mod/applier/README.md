# DDON Zone Level Sync — Applier

Keeps over-leveled characters fair in low-level zones by **reducing combat attack power** while you're
in a synced zone, then restoring it when you leave. Your **real level and EXP bar stay intact** on the
HUD — nothing about your character, gear, or progression is changed on the server.

It works entirely from **outside** the game with plain memory reads/writes (no DLL injection, no
debugger, no code patching), which is the only approach the client doesn't fight.

## For players (install)

1. Make sure the game is installed and you can log in normally.
2. Double-click **`setup.cmd`**.
   - Enter the **server address** (host/IP) and your **character name** exactly as shown in-game.
   - Approve the Windows **UAC prompt** (needed because the game runs as administrator).
3. That's it. A background helper (`apply_sync.exe`) now auto-starts at logon and activates only while
   you're in a level-synced zone. It does nothing the rest of the time.

To remove it later, run **`uninstall_applier_task.ps1`** (right-click → Run with PowerShell), or from an
admin PowerShell:

```
powershell -ExecutionPolicy Bypass -File uninstall_applier_task.ps1
```

### What gets installed
- A Scheduled Task `DDONLevelSyncApplier` that runs the applier at logon with the privileges it needs
  (so there's no extra prompt every time you launch the game).
- A log next to the exe: `applier.log` (handy if you ever need to check what it did).

## How it works (technical)

- The **server** decides, per zone, whether your level is above the zone's recommended level and
  publishes per-character attack **scale factors** (`PhysFactor`, `MagFactor`). Factors account for
  both job level and equipped gear tier (IR / item level) when `LevelSyncGearAwareScaling` is enabled.
  Tunable via `LevelSyncAttackFactorExponent` / `LevelSyncMinAttackFactor` in the server settings.
  - Network: `GET http://<server>:52099/rpc/levelsync?name=First%20Last`
  - Local file (same PC): `D:\DDON\client-mod\sync\sync_state.json`
- **Recoverable HP (gray bar):** when the server has `DisableRecoverableHpLossBelowMaxLevel` enabled and
  your job level is below `JobLevelMax`, the applier also pins your recoverable HP to max in memory so
  Priests can fully heal you during leveling. Signal file:
  `D:\DDON\client-mod\sync\recoverable_hp_state.json` (or `PinRecoverableHp` on the `/rpc/levelsync`
  endpoint in network mode).
- The **applier** finds your combat-stat object by its C++ vtable (`DDO.exe + 0x16f89d0`), reads the
  attack fields (`PhysAtk @ +0x64`, `MagAtk @ +0x68`), and writes scaled values. While synced it also
  finds the Status menu Parameters block(s) in memory (matching your true total atk pair) and scales
  Phys./Magick Attack plus knockdown/chance/exhaust/stun display totals. It tracks true/scaled
  values per object so it never double-scales, re-applies after the game recomputes (equip/zone), and
  restores on leaving. Your own pawns are scaled too (they're the other non-zero instances).

## Modes / options (`apply_sync.exe` or `apply_sync.py`)

| Option | Meaning |
| --- | --- |
| `--server <url>` + `--char "<name>"` | Network mode: poll the server endpoint for this character. |
| `--signal <path>` | Local-file mode: read the server's `sync_state.json` (default). |
| `--hp-signal <path>` | Recoverable-HP pin signal (default `recoverable_hp_state.json`). |
| `--interval <sec>` | Poll interval (default 0.75). |
| `--once` | Run a single pass and exit (testing). |
| `--verbose` | Log each actor it scales. |

## For developers

- Source: `apply_sync.py`. Rebuild the standalone exe with:

```
python -m PyInstaller --onefile --noconsole --name apply_sync --distpath dist --workpath build --specpath build apply_sync.py
```

- On a dev box with the game + server on one machine, `setup.cmd` with both prompts left blank uses
  local-file mode and needs no character name.
- The installer prefers `apply_sync.exe` (next to the script or in `dist\`) and falls back to running
  `apply_sync.py` via `pythonw` if no exe is present.

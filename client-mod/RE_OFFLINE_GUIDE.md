# DDON offline RE guide — unpacked image + anchors

The packed on-disk `DDO.exe` is useless for static analysis. Use the **live-unpacked dump**:

- **File:** `D:\DDON\client-mod\ddon_image.bin` (~66 MB, image base `0x400000`)
- Produced by the probe: `dumpimage <path>` (reads the decrypted in-memory image; section
  headers rewritten to memory layout so **file offset == VA − 0x400000**).
- Regenerate any session (addresses of CODE/.data are stable across runs): inject, then
  `dumpimage D:\DDON\client-mod\ddon_image.bin`.

## Load in Ghidra (recommended)
1. File ▸ Import File ▸ select `ddon_image.bin`.
2. Format: **Portable Executable (PE)** should auto-detect (ImageBase `0x400000`). Language
   `x86:LE:32:default`, compiler `windows`.
   - If PE parsing misbehaves, re-import as **Raw Binary**, language `x86:LE:32:default`,
     and set **Base Address = 0x00400000**. (VAs still line up.)
3. Run Auto-Analysis (defaults + "Aggressive Instruction Finder" off first pass).
4. Go to an address: `G` ▸ paste a VA below.

IDA: open as PE, image base `0x400000`; or load binary as 32-bit x86 at `0x400000`.

## Confirmed anchors (VAs, current build)

### Local player character / stats
- **Char/status class vtable = `0x01d3d3a0`** (instances on heap). Layout: `+0x10` entity id,
  `+0x14`=1, `+0x18/+0x1c` sibling component ptrs, **HP cur `+0x7e8`**, recover `+0x7f0`,
  HP max `+0x7f8`, **ST cur `+0x800`**, ST max `+0x804`; gauge reader reads `+0x7b8`.
- Gauge/HUD reader: `0xd6e1a0` (`push esi; mov esi,ecx; mov eax,[esi+0x7b8]`).
- HUD gauge worker: ~`0x145ef00`; uses globals below. Caller `0xf0e4a8` (`mov eax,[0x22044a0]`),
  nearby `call 0xd69130(lea [ecx+0x7d0], dti 0x1d3d528, 0x5b73c263)`.

### Input → command dispatch (the fire path lives here)
- **Input dispatch update loop = `0x15bfb40`** (a vtable method; called indirectly).
  Body: `call [0x2233824]`; `mov esi,[esp+0xc]` (controller arg); `push [esi+0x2700];
  call 0x13b5a00` (getSingleton-by-dti) → `edi`; loop driven by global `[0x2205d14]`;
  `mov eax,[edi]; call [eax+0x18]` (0x15bfbeb) deep-updates entities incl. our char.
- getSingleton(dti): `0x13b5670` and `0x13b5a00`.
- **8-entry × 0x20 slot table search** at `0x145ef80`:
  `loop { cmp [ecx],edx; je hit; inc eax; add ecx,0x20; cmp eax,8; jb }; shl eax,5`.
  Almost certainly the command/skill SLOT table — find what loads `ecx` (table base) and who
  calls it; `edx` is the lookup id (likely a CustomSkill command id).
- Action driver `0xc83f90` (player = `[action+0x14]`); `SetCommand` `0x9b59a0`.
- Swap-action: ctor `0x6d1510`, swap FSM `0x6c3d70`, swap DTI const `0x218d214`.

### Stable .data global singletons (session-stable)
- `[0x2204dc8]` = **INPUT / keyboard manager** (verified: holds `1234567890 QWERTYUIOP ...`).
- `[0x2204c44]` = state/HUD manager. `[0x22044a0]`, `[0x220417c]`, `[0x21e732c]` = HUD/gauge mgrs.
- `[0x2205d14]` = update-loop control (const value `0x0018d7a8`).
- `[0x22334d0]`,`[0x2233824]` = imported fn ptrs (system DLLs).
- `cPaletteParams` (menu/equip UI, NOT the live fire palette) vtable `0x1c19140`.

## Goals for offline analysis
1. **Command-player class**: from `0x15bfb40` / action driver `0xc83f90`, recover the player
   struct — the command-manager offset, the active-palette index field, and the palette/skill-id
   storage (cross-ref the 8-slot table at `0x145ef80`).
2. **Fire function**: how a CustomSkill input id flows to "execute skill id N on player". Identify
   the function + its calling convention/args so the mod can call it directly.
3. **CustomSkill command ids**: the `edx` keys used against the 8-slot table (what input maps to).

Feed findings back here; the mod will input-poll bound keys and invoke the fire function (or
write slot + index) on the live command-player.

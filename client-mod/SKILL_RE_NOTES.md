# DDON custom-skill / palette RE notes

Goal: bind **any** key/controller button to **any** learned custom skill from **either**
palette, fire it directly — no palette switching, no "4 active" gate, no per-key game limits.

Image base `0x400000`. Unpacked dump: `tools/re/dump/ddo_mem.bin` (file_off == VA - base).
All VAs below are for the dump used so far (one specific build/session); validate live.

## 2026-06-20 GHIDRA: CustomSkill command ids + input dispatch chain

Static trace output: ghidra_inputtrace.txt, ghidra_inputtrace2.txt (partial), offline disasm.

### CustomSkill keyconfig command ids (CONFIRMED @ 0xab5140)
Registration binds name string -> **slot offset** in the keyconfig command table (base `edi`).
`FUN_004236b0` stores the binding; `FUN_00ab5140` (area ~0xab5140) registers:

| Keyconfig name   | String @     | Command id (edi+off) |
|------------------|--------------|----------------------|
| (prior entry)    | 0x1c68840    | +4                   |
| CustomSkill1     | 0x1c68848    | **+8**               |
| (paired name)    | 0x1c68858    | +0x10                |
| CustomSkill2     | 0x1c6885c    | **+0xa (10)**        |
| (paired name)    | 0x1c6886c    | +0x12                |
| CustomSkill3     | 0x1c68870    | **+0xc (12)**        |
| (paired name)    | 0x1c68880    | +0x14                |
| CustomSkill4     | 0x1c68884    | **+0xe (14)**        |
| (paired name)    | 0x1c68894    | +0x16                |
| NormalSkill      | 0x1c68898    | **+0x18 (24)**       |

The paired entries at +0x10/+0x12/+0x14/+0x16 are likely sub-palette key names (same pattern:
action name + display string per slot). **Custom binding mod should use ids 8/10/12/14 for main
palette keys** (+ sub-palette ids at +0x10..+0x16 TBC).

### Input dispatch chain (game thread)
- Stable global: **`DAT_02205d14`** (controller singleton ptr, in .data).
- `FUN_015c00b0` creates input thread `FUN_015bfb40`.
- Input thread loop calls **`FUN_014ea090`** per active frame -> **`FUN_014ed4c0`**.
- `FUN_014ed4c0` loads controller from `0x2205d14`, calls **`FUN_015c1530`** with `[edi+0xb0]`,
  checks in-world (`+0x25f1==1`), jump-dispatches on `[edi+0x45daa8]` (0..4).

### Skill-input gate (NOT visual weapon-out, NOT combat mode)
Custom skills work idle with the weapon model visible; `game+0x38` is **not** "weapon on screen".

Live confirmed: weapon visibly out, `+0x38==0`, `controller+0x26f7==0` (gen 35, HP 760/760).

| Field | Offset | Role |
|-------|--------|------|
| `game+0x38` | byte | **Skill-key dispatch armed** @ `0x14ed508`. Cleared @ `0x14e8d54`. Can be 0 while weapon model is out. |
| `game+0x4599f8` | u32 | Prerequisite; must be non-zero for `+0x38` to set. Set to `1` @ `0x14ead29` when draw check `0x14e3920` passes. |
| `game+0x3c` | byte | Checked in input frame @ `0x14ea0eb` (transient sub-flag). |
| `game+0x36` | byte | Cleared each input frame @ `0x14ea0ff`. |
| `controller+0xba` | byte | Must be `1` for `+0x38` setter @ `0x15c07d0`. |
| `controller+0x24` | u32 | Must be `>= 0xa` for `+0x38` setter. |
| `controller+0x26f7` | byte | Stance byte toggled by draw/sheath **action** @ `0x15be45d`; also 0 while weapon model visible. |
| `controller+0x1c8` | ptr | Weapon ref compared by draw check @ `0x14e3920`. |

Setter chain: entity update @ `0x15be429` -> `0x14e7590` (if controller check + `+0x4599f8!=0` -> `+0x38=1`).

Mod: `skill_input_ready()` = `game+0x38==1` (probe only; cast drain no longer gated on it). Probe: `weap` / `keycfg`.

### Skill fire path (NOT the request queue)
- Command ids above feed the **command-context** system, NOT `SetCommand(mgr)` directly.
- Factory **`FUN_00507240`**: alloc 0x28 ctx, `*ctx = 0x1b189a8` (handler vtable), zero fields,
  `ctx+0x1c = 1`. Caller sets **`ctx+0x14 = player actor`** before virtual call.
- Vtable method 6 = **`FUN_005008a0`**: `execCommand(actor,0,0x4a0,1,...)` + SetCommand side effect.
  Neighbor **`FUN_00500910`** uses **`FUN_009b7a20`** (alternate command path with facing vector).
- Internal action cmd **0x4a0** is NOT the keyconfig id 8; mapping from id 8 -> ctx type -> vtable
  method still needs one more hop (likely in `FUN_015c1530` or command resolver table).

### Live session confirmed (2026-06-20)
- Player cmd-actor: vt `0x01d060c8`, entity id `0xf4c92`, mgr at `+0x1b2c`.
- `queuecmd` / request-table writes: **no effect** (casts don't touch mgr+0x32c).
- Stamina auto-detect works for verifying casts without `gp` (guard on hot pages caused 8-12s
  freeze; fixed in mod: no re-arm on duplicate EIP + DATA_STORM_CAP abort).

### Mod hook plan (next implementation)
1. Resolve live `controller*` from `peek 0x2205d14 ptr` each session.
2. **DONE (2026-06-20):** traced cast chain to `IssuePlayerCommand` @ `0x1541cc0`.
   - Input thread `0x14ea090` -> key handler `0x14e9800` -> `1541cc0(ecx=player, cmdEntry)`.
   - Player via getter `0xc15360`; game root global `0x2204c44`; keyconfig @ `[game+0x449a90]`.
   - Per-slot entry pointers @ keyconfig+`0x264`..+`0x280` (4-byte stride, verify with `keycfg`).
   - Mod hooks `0x14ea090` to drain queued casts on the input thread; bind loop queues slots.
3. Live verify: `keycfg` then `cast 1` (watch stamina). Bind keys 1-8 via `ddon_bindings.ini`.
4. Do NOT use `gp` on actor/char/HUD pages; use stamina `peek f32` for cast verification only.

## 2026-06-20 GHIDRA: full fire mechanism decoded

Static analysis of the analyzed image (re-tools/ghidra-proj, /ddon_image.bin) nailed the command
system. Outputs saved to client-mod/ghidra_firetrace.txt, ghidra_dispatch.txt,
ghidra_actorglobal.txt; disasm via tools/offline_disasm.py.

### The fire API (CONFIRMED from decompilation)
- `execCommand` @ `0xc83f90`, __thiscall(ecx=actor):
  `execCommand(actor, int slot, uint cmd, char force, u32 p5, float dirX, u32 p7, float dirZ)`.
  Fires `cmd` IMMEDIATELY (does not read the request table). If dirX<0 it derives facing.
  Typical call: `execCommand(actor, 0, <cmd>, 1, 0, -1.0f(0xbf800000), 0, 1.0f(0x3f800000))`.
- `GetCommandManager` @ `0x43b2b0`: returns `mgr = *(u32*)(actor + 0x1b2c)`.
- `SetCommand` @ `0x9b59a0`, __thiscall(ecx=mgr):
  `SetCommand(mgr, uint idx<10, u8 flag, u32 d330, u32 d334, u32 d338)` writes a request entry:
  `mgr[idx*0x18 + 0x32c]=flag(byte); +0x330=d330; +0x334=d334; +0x338=d338`. Plain bounded
  writes -> safe to replicate by memory write. (This is the queue the per-frame pump consumes.)
- Skill/action `cmd` ids are hardcoded per handler. Seen: 0x4a0, 0x478, 0x479, 0x401, 0x115,
  0x64(idle), 0xffff(cancel). Range ~0x100..0x4ff.

### Command-context object model (why the live hunt was hard)
- Commands run as VIRTUAL methods of a transient "command-context" object:
  - factory `0x507240` (and siblings every +0x60) allocates a 0x28-byte ctx, sets `[ctx]=vtable`,
    `[ctx+0x14]=actor` (caller fills it), `[ctx+0x1c]=1`, `[ctx+0x20]=param`, etc.
  - The 250-entry table at `0x1b189a8..0x1b18d90` is CONCATENATED VTABLES, 18 methods per
    command-context subclass (each subclass vtable = a 0x48 block; e.g. 0x1b189a8, 0x1b189f0, ...).
  - Handler `0x5008a0` = vtable method #6 of the first block; body does
    `execCommand([ctx+0x14], 0, 0x4a0, 1, 0, -1.0, 0, 1.0)` then GetCommandManager + SetCommand.
- In ALL 385 execCommand call sites the actor is `[ESI+0x14]` (or [reg+0xc]) of a ctx object.
  There is NO global actor pointer at the call sites (scanned: ghidra_actorglobal.txt, 0 globals).

### REMAINING BLOCKER + plan: resolve the LIVE local-player actor (cExActor)
- The cExActor is the object execCommand runs on; it has `[+0x1b2c]=mgr`. NPC cExActors caught live
  had vtable `0x01d6e8b0` but [+0x1b2c]=garbage (NPCs may not own a cmd mgr); the player's should.
- Player char (cCharacter vt 0x01d3d3a0) is reliably found via `hp`; it shares entity id (+0x10)
  with the actor. Next-session options to get the actor:
  1. Live scan for object with vtable `0x01d6e8b0` AND (+0x10 & 0xffffff)==char entity id (add a
     probe cmd `findvt <vt> <id24>`), then verify [+0x1b2c] is a heap ptr (mgr).
  2. Find the cCharacter->cExActor link field (decompile a cCharacter method that fetches actor),
     or a global "local player" getter (search xrefs to vt 0x01d3d3a0 / a known getter).
- THEN fire: call `execCommand(actor, 0, cmd, 1, 0, 0xbf800000, 0, 0x3f800000)` via raw_call.
  Off-thread may be OK (execCommand is actor-local, no allocator lock like the getter that crashed);
  if it crashes, run it on the game thread via the guard-page trampoline. Bind keys -> cmd ids.

## 2026-06-20 session: ruled out execCommand-as-player-path; tooling hardened

Confirmed-live local player this session: char/cCharacter `0x0d58dc60`, vt `0x01d3d3a0`,
entity id (+0x10) `0x000f4c92`, HP cur (+0x7e8) = 760 (matches in-game). Reliable anchor recipe
(`hp` -> param block; char = param-0x7e8) still good.

Key NEGATIVE results (save time next run):
- `execCommand` (0xc83f90) is NOT the local-player skill path. Catching its entry filtered to the
  player entity (0xf4c92) over 30s of moving+casting NEVER matched -> the player's casts don't call
  execCommand with the skill. The per-frame idle (cmd 0x64) calls we caught earlier were OTHER
  actors (e.g. NPC entity 0xf4c4a). execCommand's page also has hot non-target code (storms if you
  re-arm waiting) -> only good for a single first-hit capture, not selective waiting.
- `findcmd 0xf4c92` (objects sharing entity-id-low with valid +0x1b2c heap ptr) found 5 candidates;
  3 real managers. Candidate 1 (`cExActor 0x0cc2bf40`, vt `0x01d22250`, mgr `0x24499bc0`) holds the
  ASCII skill-resource string `"0403_0000"` at mgr+0x32c -- but a 20s DATA-guard on that page during
  active casting saw ZERO accesses => it's a DEAD/static object, not the live command path. The
  other candidates' tables likewise not confirmed live.
- SetCommand code page (0x9b5000) is HOT: guarding it as code froze the game (every-instruction
  fault storm). DO NOT guard hot code pages.

Tooling added/confirmed this session:
- `catch <func> [window] [mincmd] [entity24]` one-shot entry capture with optional cmd/entity
  filters; background re-arm poller (no single-step) + hot-page auto-abort (CATCH_STORM_CAP, only
  counts true non-target eips). `fire <cmd> [slot] [window]` rewrites an in-flight call's args.
- `findcmd <id24>` scans for the entity's command actor(s).
- DATA guard (`gp <addr> [maxhits] [window_ms]`) VERIFIED working: caught HUD reader DDO+0x96e1a3
  reading char+0x418 every frame. Safe on heap pages; use this, not code-page guards.

NEXT (untried, most promising): find what the player's cast actually WRITES.
- Option A: memory-diff the char + its linked sibling/actor objects across idle->cast (need a
  snapshot/diff probe cmd).
- Option B: DATA-guard the char object's state pages for WRITES during a cast (filter log to
  "write"), trace writer EIP -> skill exec code.
- Option C: Ghidra static trace of the input path: CustomSkill1..4 keyconfig command -> handler ->
  fire, independent of live guessing.

## BREAKTHROUGH 2026-06-18: caught execCommand entry + game-thread fire path

Guard-page "catch entry" mode landed `execCommand` (`0x00c83f90`):
- `ecx` (this/actor) = `0x0d5d80b0` (session-specific). Confirmed **stable**: vtable
  `[actor]=0x01d6e8b0` (valid `.rdata`) unchanged across 30s+. So the player actor object persists.
- Arg layout (stdcall, args @ esp+4), captured on the per-frame idle call:
  `arg0=slot(0)  arg1=cmdId(0x64 idle)  arg2=force(0)  arg3=1  arg4=dirX f32(10.0)  arg5=0  arg6=dirZ f32(1.0)`
  (arg7 0xf is past the 7-arg / ret 0x1c boundary). Real casts use force=1, cmd in skill range.

Fire path decided = **game-thread arg-rewrite**, NOT off-thread call (off-thread getter crashed the
allocator lock earlier). execCommand runs EVERY FRAME for the idle command. `fire <cmd>` arms a
one-shot catch on execCommand entry and, on the next (idle) call, rewrites the in-flight args to
`(slot, cmd, force=1)` then lets it proceed -> the game's own thread executes our skill. No patch,
no off-thread call, anti-tamper-safe.

Single-step "skip non-target fault" was REMOVED: a global trap-flag raced across the game's many
threads and left an unhandled `0x80000004` single-step -> crash (16:29). Replaced with a background
re-arm poller: a non-target fault just flags `NEED_REARM`; a helper thread re-applies the guard
~1ms later off the faulting thread. No TF, no cross-thread race.

### Next-session test recipe (actor is session-specific, re-capture each launch)
1. Relaunch DDO, inject. Get in-world.
2. `catch 0xc83f90 20000 0x400` then cast a REAL skill -> logs that skill's cmd id (+ live actor).
   (mincmd filter 0x400 skips the 0x64 idle; safe now that single-step is gone.)
3. `fire <thatCmd>` with NO input -> skill should fire on its own = proof of game-thread fire.
4. Then map cmd ids for each skill the user wants to bind; wire to keypoll for the real feature.

## Architecture (confirmed)

DDON gates custom skills entirely client-side:
- The key-config exposes only **4** custom-skill input commands: `CustomSkill1..4`
  (keyconfig action-name strings at `0x1c68848/.._5c/.._70/.._84`, registered by the
  table builder around `0x00ab5184`). These map to the **active palette's** 4 slots.
- Sub palette is reached via a **palette-swap action**, class `cHumanActCustomSkillChange`.
  This is why the AHK approach must long-press B and eats swap lag.

So "all 8 from both palettes / unlimited" needs one of:
1. flip the active-palette state in memory + trigger a slot command (instant, no visible swap), or
2. hot-swap arbitrary skill ids into the 4 active slots then trigger (gives "as many as learned"), or
3. call the skill-cast/command path directly with a chosen skill/slot.

## Reusable assets (from the level-sync client digging)

- **Local player actor locator**: scan committed `MEM_PRIVATE` regions for the actor
  vtable `VA 0x1af89d0` (RVA `0x16f89d0`); pick the instance with the highest
  attack. Actor combat fields: phys atk `+0x64`, magick atk `+0x68`.
  Scripts: `find_owner.py`, `find_level_ptr.py`.
- **Live disassembler** `disasm.py` (capstone); **offline** `disasm_off.py` (dump).
- **Static hunters** (offline, no game): `find_skill_static.py` (strings+xrefs),
  `find_classreg.py` (name -> DTI singleton + vtable + size), `disasm_off.py`.
- **Snapshot/diff** `finddelta.py` (Cheat-Engine-style next-scan).

## Key classes (name -> DTI singleton, vtable)

| class | dti | vtable |
|---|---|---|
| cHumanActCustomSkillChange (palette swap action) | 0x218d214 | 0x1bac1e0 |
| cPaletteParams | 0x21972f8 | 0x1c19140 |
| cCustomSkillInfo | (str 0x1c17960) | - |
| cAcquirement::cCustomSkillData | 0x217d4ec | 0x1ae84e0 |
| rAcquirement::rCustomSkillData | 0x21a80b8 | 0x1c48994 |
| uGUIHudCustomSkillPallet | 0x21c8230 | 0x1d1fe68 |

## Palette-swap action internals (vtable 0x1bac1e0)

Action object layout (this = esi):
- `+0x0c` -> some owner sub-object (writes flags at +0x1bf0/+0x1bf4)
- `+0x10` -> step/phase byte (FSM index; update switches on it, 6 cases)
- `+0x14` -> **owner / player object** (target of all command calls)
- `+0x28` -> bool in-progress flag

Methods:
- `vtbl[1]=0x6d1510` ctor/alloc; inits action fields (+0x14..+0x26).
- `vtbl[8]=0x6b3b00` enter: calls `0x43b2b0(player, ... ,0x10)`/`(...,0x11)` then `0x9b59a0`.
- `vtbl[9]=0x6c3d70` **update** state machine: `movzx eax,[esi+0x10]; cmp eax,5; ja ..;
  jmp [eax*4 + 0x6c3f7c]`. Drives the actual swap across phases. Calls `0x43b2b0(player,...,0x85)`.

### Candidate "trigger command/action by id" function
`0x43b2b0` — cdecl, ~5 args, returns an object; result used as `this` for `0x9b59a0`
(execute) / `0x9b55f0`. Args seen: ids `0x10, 0x11, 0x85, 0x401`, plus small bool/flags.
**Hypothesis:** `obj = GetCommand(player, id, ...); Execute(obj)`. If true, the CustomSkill1..4
command ids fed here = direct slot trigger. NEEDS LIVE CONFIRMATION.

## LIVE-CONFIRMED (current installed build, 2026-06-18) — addresses valid as-is

Image loads at preferred base `0x400000` (verified: crash `DDO.exe+off` math, and code dumps).
**The current build matches these notes exactly** — `0x6c3d70` disassembles byte-for-byte as the
swap-update FSM described below. So all VAs in this file are live-valid for the current build.

### Methodology that WORKS (and what crashes)
- **Read-only is 100% stable**: probe `dump`/`find`/`find-vtable`/`hp`/`threads` ran for minutes,
  zero crashes. Disassemble live (unpacked) bytes host-side: `tools/live_disasm.py <va> <len>`
  (parses probe `dump` lines out of `C:\Users\Public\ddon_mod.log`, Capstone x86-32).
- **Hardware data breakpoints (`bp`/`bpoff`) CRASH** the game deterministically at
  `DDO.exe+0x10f19a5` (anti-tamper sabotage on DR writes). Do NOT use hardware BPs.
- **Palette-swap hook (`0x6c3d70`) CRASHES** — runs every frame during swap FSM; causes heap/type
  confusion. Do NOT hook the swap FSM.
- **Input-frame hook (`0x14ea090`) is the cast delivery path** — hook it to drain queued
  `IssuePlayerCommand` calls on the game's input thread; pair with `GetAsyncKeyState` bind loop.
  Must stay allocation-free inside the detour (no logging/format!/locks).
- **Guard-page VEH** is for RE discovery and fallback cast delivery, not the primary ship path.
- **Python RE tools** (`disasm.py`, `offline_disasm.py`, Ghidra exports) are fine for static/live
  analysis; the shipped mod is Rust in-process.
- Stateful game funcs (entity getter `0xc15360`, etc.) must NOT be **called off-thread** — resolve
  `game+0x154` with reads only, fire casts on the input thread.

### Player anchoring (no user input needed)
- `hp` command: signature-scans for the param block `+0x00 HP cur(u32) +0x08 rec +0x10 max
  +0x18 ST cur(f32) +0x1c ST max(f32)` (between-words zero, cur<=rec<=max). Picks player by HP
  max after discarding constant-value clusters (e.g. 48128 render structs). Player block low
  address bits are stable across sessions (~`...d7e8`).
- Actor: `scan-actor` (vtable `0x1af89d0`) finds ~106 instances; the atk-offset heuristic is
  UNRELIABLE (picks garbage floats). Player actor is in the `0x0a59xxxx` party cluster (e.g.
  `0x0a590a08`, signature `...0a08`). Cross-verify via the HP block, not the atk heuristic.

### Command/trigger path (CONFIRMED live)
- `0x43b2b0` = `player->GetCommandManager()`: `push ecx; lea eax,[ecx+0x1b2c]; push 4; push
  &local; call <atomic-load>; mov eax,[esp+0xc]; ret`. Returns the mgr ptr held at
  `player+0x1b2c`. (Old notes called this "GetCommand"; really it just fetches the mgr.)
- `0x9b59a0` = `mgr->SetCommand(idx, .., flag, val, id, ..)` thiscall, `ret 0x14`: range-checks
  `idx<=9`, writes a descriptor into `mgr[idx*0x18 + 0x32c..0x338]` (id stored at +0x334).
  Queues the action; the mgr runs it. Caller pushes `(idx, 1, 0, 1, id)` then this consumes them.
- `0x6b3b00` swap-enter (vtbl[8]): `ecx=[esi+0x14]=player`; builds vec3(-1,0,1) on stack and
  `call 0xc83f90` with `(0,0x401,1,0)`; then twice does `mgr=GetCommandManager; SetCommand(..,id)`
  with ids `0x10` then `0x11`.
- `0xc83f90` = thiscall(player, .., id, .., vec) — uses `id` to index `[player+0xec0 + id*0x140]`
  (a per-command/per-action table), calls `0x4aec60`, compares an id field, may call `0x95e6f0`.
  This is a direct "drive action/command by id with a direction" entry. id `0x401` seen for swap.
- `0x6c3d70` swap-update FSM (vtbl[9]): EXACT match to notes (`movzx eax,[esi+0x10];cmp 5;ja;
  jmp [eax*4+0x6c3f7c]`); writes `[player+0x1bf0]=1,[+0x1bf4]=0`.
- HP/Stamina are mirrored into the param block by a worker routine at `0x105f2a0` (gauge copy).

### Keyconfig (CustomSkill1..4)
- Builder `0xab5184` registers action-name strings via `0x4236b0`: `0x1c68848` (CustomSkill1),
  `0x1c68858` (CustomSkill2), ... (push len 5). Need: the numeric command ids these map to.

## Still to find (read-only; fastest via static disasm + value scan)

- **A**: active-palette-index field offset (toggled during swap). Read-only: `snap`/`diff` a
  region of the player/palette object across a manual B press (no breakpoints needed), or trace
  the `0x6c3d70` jump-table cases statically to see which field they flip.
- **B**: the 8 equipped custom-skill-id fields (main 4 + sub 4) in the player/palette object
  (`cPaletteParams` dti `0x21972f8`, vtable `0x1c19140`) — enables hot-swapping arbitrary skills
  into a slot => "as many as learned". Find via find-vtable on `0x1c19140`, then dump layout.
- **C**: the numeric `CustomSkill1..4` command ids (statically trace what `0xab5184` stores for
  each registered name) -> feed to `SetCommand`/`0xc83f90` to fire a slot.
- **D**: the input->skill handler for CustomSkill1..4 (call it directly with a chosen slot after
  writing the desired skill id), as an alternative to the command-queue path.

## WALL HIT (2026-06-18 session 2): player object only reachable via volatile work-nodes

- The player/gauge objects are referenced ONLY from churning per-frame buffers in the
  `0x67xxxxxx` / `0x6axxxxxx` regions (engine "work/update node" lists). Example: a node at
  `0x6ae0fa90` is stable in its list bookkeeping (self-pointers) but its object-pointer fields
  (e.g. `+0x34` -> gauge) are rewritten every frame, so two dumps 1.5s apart already differ.
  => Snapshot `find`/`dump` cannot reliably walk UP from gauge -> player.
- `xref` of the swap-action DTI `0x218d214` returns only DTI plumbing: registration
  (`0x1a1cd73/83`), getSingleton-by-DTI thunks (`0x6a87d0`: `push dti; call 0x13b5670;
  mov edx,[eax]; call [edx+0x34]`), DTI accessors (`mov eax,<dti>;ret` table near `0x6ade50`),
  and the ctor `0x6d1510`. The gameplay creation site (which loads `player`) goes through a
  generic factory, so it is NOT xref-able from the class constant.
- `0x13b5670` = getSingleton(dti) (DTI -> singleton instance). Singletons ARE the stable anchor.

### Next viable anchor = a GLOBAL singleton holding the local player (stable .data ptr)
Options to find it (read-only):
1. Add a **call-site scanner** (find `e8 rel32` whose target == a known player-method like
   `0xc83f90`); disassemble callers to see how `player` (ecx) is loaded -> likely `mov ecx,[GLOBAL]`
   or `call getSingleton(charMgrDti); mov ecx,[eax+off]`. The GLOBAL is our stable anchor.
2. Identify the character/player-manager DTI, call `0x13b5670(dti)` to get its singleton, read
   the local-player field. (Needs confirming the dti + offset.)

### Open feasibility risk
Even with a stable `player`, CALLING deep game funcs from our thread is unproven vs anti-tamper.
Gate this early once we have a valid `player`.

## BREAKTHROUGH (2026-06-18 session 3): guard-page capture BEATS anti-tamper

- **`gp <addr> [maxhits]` / `gpoff`** = guard-page software data breakpoint (`guardpage.rs`).
  Flips the target page's protection to add `PAGE_GUARD`; the next time the GAME's own code
  touches it, a `STATUS_GUARD_PAGE_VIOLATION` fires into our VEH, which logs the accessing
  instruction + all registers + stack return addresses, then continues. **No DR registers, no
  patched bytes -> anti-tamper does NOT trip. Game stayed alive through repeated captures.**
  This is THE capture tool to use for RE (NOT hardware `bp` — those trip anti-tamper).
- Handler only touches a fault whose data address is inside the single armed page; everything
  else (e.g. real stack-growth guard pages) is passed through (`CONTINUE_SEARCH`). OS clears the
  guard on the triggering access, so it auto re-arms up to `maxhits`.

### Stable anchor recipe (read-only, every session)
- Run `hp` -> player HP/Stamina **param block** (e.g. `0x0ec7e668` = HP cur this session).
- **Character/status object = `param_block - 0x7e8`** (HP cur lives at `char+0x7e8`).
  Verified: guarding the param page caught reader `DDO+0x96e1a3` with `ecx=esi=char`, reading
  `[esi+0x7b8]`. Char **vtable = `0x01d3d3a0`** (in-image .rdata) -> use to validate.
- NOTE: this char/status object is NOT the command "player": its `+0x1b2c`/`+0x1bf0` are 0.
  It has parent pointers: `+0x18 -> objA`, `+0x1c -> objB`, `+0xec0 -> objC` (objC+0xec4=9.0f,
  +0xec8=150.0f look like gauge/UI params). The command-player is UP this ownership chain.

### Next: walk up to the command-player
Guard the char page (`gp char 8`) and read `[GP] stack[...] ... (ret)` lines -> disasm those
callers (`live_disasm.py`) to see how each parent loads the next (`mov ecx,[parent+off]`),
recursing up until we reach the object with cmd-mgr `+0x1b2c` / palette params, or a `mov
reg,[GLOBAL]` stable anchor.

## ENTITY MODEL (2026-06-18 session 3) — component graph + palette located

### Object header signature (all entity components share this)
- `+0x00` = vtable (in-image .rdata, `0x004xxxxx..0x01ffxxxx`)
- `+0x10` = **entity id** (e.g. `0xf4c92`), identical across every component of one character
- `+0x14` = `1`
- `+0x18`, `+0x1c` = sibling/related component pointers (cross-link the entity's components)
- `gp` stack walk of the char inner-loop (`0x148f7d2`, reads char+0x28) was noisy: my E8-only
  "ret" tag misses indirect-call returns and tags pushed function pointers as `ptr`. Need to also
  recognise FF /2 (call r/m) returns to make the stack walk reliable. The component-graph route
  below was faster.

### `entity <id>` probe cmd = enumerate one character's components
`entity 0xf4c92` -> 9 components this session (addresses volatile per session; vtables stable):
`0x01d4d940, 0x01d3d3a0 (char/HP), 0x01d40818, 0x01d1c3d0, 0x01d13928, 0x01d1ca70,
0x01cf5b40, 0x01d3efe0, 0x01d4d940`. Re-find any session: `hp` -> param block; char =
param-0x7e8; read char +0x10 = entity id; `entity <id>`. One of these is the command/skill root
(holds palette + cmd-mgr) — STILL TO CONFIRM which vtable.

### Palette params located: vtable `0x1c19140`
`find-vtable 0x1c19140` -> 4 instances: a stable-region pair `0x..ffac4`/`0x..ffbb4` (0xf0 apart
= main+sub?) and a churn-region pair. BUT a double-read 1ms apart showed the bytes CHANGING ->
these 4 look like menu/preview copies, not the live active palette. Field `+0x50` held `01 01 01`.
=> Don't trust snapshot palette dumps. GROUND-TRUTH the ACTIVE palette + fire path by `gp`-guarding
during a real palette swap / skill press (guard a candidate component or a palette page, then act).

### Tools added this session
- `gp <addr> [maxhits]` / `gpoff` (guardpage.rs) — anti-tamper-safe data breakpoint (regs+stack).
- `entity <id> [maxhits]` (probe.rs) — list all component objects sharing an entity id.
- **generation guard** (probe.rs `is_latest`): hot-reloads now auto-silence older injections
  (file `C:\Users\Public\ddon_mod_gen.txt`). Only need to RESTART the game to clear pre-guard
  instances; after that, re-inject freely.

## STABLE GLOBALS found (2026-06-18 session 3) — the real anchors

Disasm of the per-frame update loop **`0x15bfb40`** (reached on the char's stack) is driven by
`.data` globals (session-stable, survive across runs since they're in the image):
- `mov eax,[0x2205d14]` -> big manager singleton (fields `+0xbc/+0x25f1/+0x2708/+0x2745`,
  vcall `[eax+0x4c]`); it's the LOOP CONTROLLER (`0x15bfbf2: mov eax,[0x2205d14];
  cmp [eax+0x2708],0; je ...`). NOTE live value was `0x0018d7a8` (low) — re-check; may be a
  sub-field load not a heap ptr.
- `[0x2204dc8]` -> heap mgr (`0x22968060`), checks `[ecx+0x134]&0x30`, `[ecx+0x160]&0x2000`.
- `[0x2204c44]` -> heap mgr (`0x20e88060`), `call 0x14ea090`.
- `[0x22334d0]`,`[0x2233824]` -> system DLL fn ptrs (call thunks).
- Loop body: `call 0x13b5a00([esi+0x2700])` (singleton-by-dti) -> edi (entity subsystem);
  `mov eax,[edi]; call [eax+0x18]` (0x15bfbeb) is the call whose deep work touches our char
  (return `0x15bfbf2`). So path = `[global mgr] -> entity subsystem -> ... -> char`.
=> These globals are the stable roots. Next: navigate `0x2204c44`/`0x2204dc8` mgr -> local player
   -> command/palette. (Or capture the skill-fire path directly, below.)

### cPaletteParams vtable `0x1c19140` is NOT the live skill palette
Guarding it (page only) got ZERO hits in-world AND zero on skill use -> it's the menu/equip-screen
palette UI, not the active command palette. Don't chase it for firing.

### Direct route to the command-player (in progress): stamina-write capture
Using a skill consumes stamina -> a DISTINCT writer of char param block (HP cur @ char+0x7e8,
ST cur @ char+0x800) whose call stack IS the fire path. Plan: `gp <param_block> 12`, let the
per-frame UI readers fill a few slots (deduped), then use a skill; the new write EIP's stack
reveals command-mgr -> command-player. (Param page is read every frame, so guard fires immediately;
dedup + RAW_CAP keep it bounded.)

## SESSION 3 cont. — hardened guard, param=display copy, more globals, 8-slot lead

- **Guard hardened & stable**: time-boxed window (`gp <addr> [maxhits] [window_ms]`, watchdog
  auto-disarm) + ALL handler reads via ReadProcessMemory (fault-proof). Survives full window on a
  hot page, disarms clean. Two earlier crashes were OUR bugs (disarm race; unsafe stack reads).
- **Param block is a DISPLAY COPY**: guarding it across real skill use produced NO write — only
  the gauge reader `0xd6e1a3` (reads char+0x7b8; char=param-0x7e8, vtable `0x01d3d3a0`). So skill
  stamina-consumption writes a DIFFERENT (master) location, not this HUD block. Don't expect the
  fire path from guarding the HP/ST display block.
- **Gauge worker chain** (HUD draw, loads local player): reader `0xd6e1a3` <- `0xf0e4a8`
  (`mov eax,[0x22044a0]`; nearby `call 0xd69130(lea [ecx+0x7d0], dti 0x1d3d528, 0x5b73c263)`)
  <- worker around VA `0x145ef00`: `mov ecx,[0x220417c]; mov eax,[ecx]; call [eax+0x3c]` and
  `mov eax,[0x21e732c]`.
- **NEW stable globals** (.data, session-stable): `0x22044a0`, `0x220417c`, `0x21e732c`
  (add to `0x2205d14`(const 0x18d7a8), `0x2204dc8`=INPUT/keyboard mgr, `0x2204c44`=state mgr).
- **8-slot table lead**: at VA `0x145ef80`: `loop { cmp [ecx],edx; je hit; inc eax; add ecx,0x20;
  cmp eax,8; jb }` then `shl eax,5`. An 8-entry × 0x20-byte array keyed by an id (edx) -> almost
  certainly the command/skill SLOT table. Find what loads `ecx` here (the table base on the
  command-player). NOTE: disasm of this region was partially misaligned -> re-dump aligned.

## OFFLINE RE WIN (2026-06-18 session 3) — fire function fully decoded

Dump `D:\DDON\client-mod\ddon_image.bin` + `tools/offline_disasm.py <va> [count|--until-ret]`.

### `0xc83f90` = SKILL/COMMAND FIRE  (thiscall, `ecx=this=command-player`, `ret 0x1c` = 7 dwords)
```
edi = this (command-player)
ebp = arg1 = SLOT INDEX
xmm/float args = [esp+..] direction/charge vector (arg4 + later [esp+0x24/28/34/38])
slot = edi + 0xec0 + slotIndex*0x140         ; command-slot array, stride 0x140
cur_id = 0x4aec60(slot)                       ; returns u16 = slot's command id (reads slot+4 via 0x3666850)
... call 0xc7f770(...) ; call [edi+0x98]/[edi+0x100] ; touches edi+0x19dc/0x19e8/0x1ac8/0x1ad0
```
=> To fire ANY skill on any key: write the desired command id into a slot (slot+4) and call
   `0xc83f90(player, .., slotIndex, .., dir)`, OR (cleaner) find the lower-level "execute command
   id" under `call [edi+0x98]` so we don't clobber a palette slot. Direct callers all reach player
   via an ACTION (`[action+0x14]`), so the command-player == the object holding cmd slots at +0xec0.

### Command-player struct (confirmed offsets)
- `+0x00` vtable (methods at `+0x98`, `+0x100`)
- `+0xec0` command-slot array, stride `0x140`; `slot+0x04` = command id (u16)
- `+0x19dc`,`+0x19e8` float/aim sub-structs; `+0x1ac8`,`+0x1ad0` sub-objects (have `+0x2c` flag)
- (old notes' `+0x1b2c` cmd-mgr / `+0x1bf0` flags are on THIS object too)

### IMPORTANT refinements
- `0xc83f90` has **1286 callers** (scanner) => it's a GENERIC `cExActor::execCommand(slotIdx,
  cmdId, force, dir...)` used by player AND AI, not the palette-only fire. Good: we can invoke it
  on the local player with a chosen command id. The `cmp cur_id,arg1; jne` is NOT a hard gate
  (execution continues either way) -> arg1 likely "expected/previous id" for a sub-branch only.
- **RTTI is stripped** (vtable-4 not a valid COL). Class names need MT Framework DTI (cDTI name
  strings), not MSVC RTTI. `tools/rtti.py` returns None by design.
- Offline tools added: `tools/offline_disasm.py`, `tools/rtti.py`, caller-scan one-liner.

### LIVE-ACTOR HUNT — current wall (2026-06-18)
- `dumpimage` captures only the IMAGE (0x400000..0x45a3c00), NOT the heap, so the live local
  player can't be resolved purely from the dump. No `.data` global holds the player directly.
- `findactor` probe cmd added (scan heap for code-range vtable + 8 plausible cmd slots @+0xec0).
  Result: INCONCLUSIVE — the `+0xec0` 8-slot/u16-id signature is not distinctive (random heap
  matches it), and `0x020007d0` is a real shared base struct in .rdata so vtable test alone is
  weak. No candidate carried the player entity id `0xf4c92`.
- Controller object (`0x15c00b0`, the per-frame input task owner) holds `[this+0x2700]` = local
  player entity handle, and registry `0x21e6de0` maps entityId->object. But `0x15c00b0` has NO
  static xref (vtable built at runtime), so the controller can't be reached from the image alone.
- NEXT viable techniques (need either): (a) capture the actor pointer at execCommand time — but
  that's a CODE bp, anti-tamper-blocked; (b) find the local-player global ACCESSOR in Ghidra
  (decompiler) and read it live; (c) find the component->owner pointer (each entity component
  likely stores the owning unit/actor ptr at a fixed offset — dump a component header and look
  for a shared pointer across components = the actor).

### LIVE-PLAYER: runtime-only approaches EXHAUSTED (2026-06-18)
Tried, all dead-ended (player is behind layers of managers/handles):
- Guard char base page during skill use + object-stack walk: only catches the **HUD/gauge** path
  (objs on stack = gauge mgr 0x048d6570 vt 0x01ca3a08, 0x20d38060 vt 0x01ca50c0). char is a
  DISPLAY/param component read by HUD; the actor doesn't expose itself via char's pages.
- Registry 0x21e6de0 (`[base+(id&0xff)*4]`, walk +0x14, key +0x1c): it's a HASHED-HANDLE table
  (keys 0x55c2c792.. low-byte 0x92 only coincidental), nodes in 0x0218xxxx mgr region, entries
  share +0x10=0x0218c054 owner. NOT the entity registry for our 0x0e8xxxxx player. Dead end.
- findactor structural scan: not distinctive (see above). No candidate had eid 0xf4c92.
CONCLUSION: pinning the live local player WITHOUT a decompiler is exhausted. The accessor that
all runtime paths obscure (getLocalPlayer / controller global) needs Ghidra on ddon_image.bin.

## GHIDRA DECOMP BREAKTHROUGH (2026-06-18) — firing fully cracked

Tools: Ghidra 12.1.2 @ D:\DDON\re-tools (portable JDK21). `start-ghidra.bat` launches GUI.
Headless: `analyzeHeadless <proj> ddon -process ddon_image.bin -noanalysis -postScript X.java
-scriptPath D:\DDON\re-tools\scripts`. Project already analyzed+saved. Scripts: DecompDump.java,
XrefDump.java, ActorTrace.java. Output: ghidra_decomp.txt / ghidra_xref.txt / ghidra_actor.txt.

### FIRE: `execCommand` = 0xc83f90 (__thiscall)
Signature (from disasm; Ghidra dropped ecx): `execCommand(ecx=actor, int slot, uint cmdId,
char force, undef p4, float dirX, undef p6, float dirZ)`, `ret 0x1c`.
- slot struct = `actor + 0xec0 + slot*0x140`; `slot+4` = u16 current cmd id (getter 0x4aec60).
- Body: `if ((curId & 0xffff) != cmdId || force != 0) { FUN_c7f770(...); actor->vtable[0x98](...);
  ... }`  => **force=1 fires ANY cmdId regardless of slot contents.**
- Real call sites (e.g. 0x5008a0): `mov ecx,[esi+0x14]` then
  `push 0(slot); push 0x4a0(cmd); push 1(force); push 0; floats -1.0,0,1.0; call 0xc83f90`.
  => **actor = [dispatcher + 0x14]** (dispatcher = `this` of skill handlers 0x4d2080/0x4fe9c0/
  0x5008a0...). Example skill cmd ids seen: 0x478, 0x479, 0x4a0 (weapon/core skills, ~0x400-0x4ff).

### LOCAL PLAYER: deterministic from a STABLE GLOBAL
- **controller = `[0x02205d14]`** (set in allocator 0x15be050: `DAT_02205d14 = param_1`).
- Input thread 0x15bfb40(controller) [spawned via CreateThread in 0x15c00b0, handle@ctrl+0x2704]:
  ```
  node   = registry( controller[0x9c0] )      ; controller+0x2700 = local player HANDLE
  player = node->vtable[1]()                   ; vtable offset +4 = index 1
  loop:  FUN_014ea090(); player->vtable[0x18]()   ; per-frame update
  ```
- registry 0x13b5a00(id): `p=[0x21e6de0 + (id&0xff)*4]; while p: if [p+0x1c]==id: return p; p=[p+0x14]`.

### LIVE RESOLUTION RECIPE (run when game is up)
1. controller = [0x2205d14]
2. handle = [controller + 0x2700]
3. node = registry walk above (bucket 0x21e6de0 + (handle&0xff)*4, next +0x14, key +0x1c)
4. read node->vtable, disasm node->vtable[1] (off +4) to learn what `player` it returns
5. actor = player (verify: actor+0xec0 slots have plausible u16 cmd ids at +4)
6. read actor+0xec0 slots -> the equipped palette skill cmd ids (the bind menu source)

### REMAINING UNKNOWNS (test live)
- Confirm node->vtable[1]() result == actor (has +0xec0 slots), or how to step to actor.
- Map equipped/learned skills -> cmdId (read palette slots; maybe a skill table).
- Does a DIRECT call to execCommand from our thread trip anti-tamper? (patching/HWBP tripped it;
  a plain call should be OK.) Test: execCommand(actor, 0, <known cmd>, 1, 0, -1.0,0, 1.0).
- Does slot index matter with force=1?

## LIVE TEST 2026-06-18: function-call thunk works, but OFF-THREAD CALLS CRASH

- Built `call.rs` raw_call(): allocates RWX page, emits `push args / mov ecx / mov eax,func /
  call eax / ret` thunk. No code patching => not blocked by anti-tamper. Probe cmds: `call`,
  `resolveplayer`.
- Live-resolved the chain by READS (all stable, in-module .bss):
  controller=[0x2205d14]=0x0018d7a8; handle=[ctrl+0x2700]=0x7872dd2b;
  registry bucket 0x21e6de0+(0x2b*4) -> walked 15 hops -> **local-player node = 0x021c10ac**
  (node+0x1c=handle, node+0x18=0xa0ee, node vtable=0x01ca39e8, node->vtable[1]=getter 0x00c15360).
- getter 0x00c15360 (node hardcoded): `0x13b5670(node)` -> M=[0x21e7328]; then
  `M->vtable[8](0x283b8,0x10,handle)`; tailcall 0xc106d0 -> returns local player.
- **`resolveplayer` (called getter on OUR probe thread) => INSTANT CRASH.**
  AV write at DDO.exe+0x114d0bb (eip 0x0154d0bb) AND 0x019a139a, **multiple game threads faulting
  simultaneously** => off-thread call corrupted shared entity-manager state mid-use. Game process
  died. So: the thunk executes fine, but stateful game funcs MUST run on the game's own thread.

### REVISED PLAN (thread-context is the real constraint)
1. RESOLVE PLAYER WITHOUT CALLING: replicate getter read-only. Next session read M=[0x21e7328],
   Mvt=[M], vtable[8]=[Mvt+0x20]; disasm vtable[8] from dump; if it's a hash/array lookup,
   reproduce with reads to get the actor pointer (no call, no crash).
2. FIRE ON THE GAME THREAD (not our thread). Options, in order of safety:
   a. VEH guard-page "hook": arm PAGE_GUARD on a DATA global the per-frame update touches; our
      VEH handler runs ON the faulting game thread -> call execCommand there. (Guard-page already
      proven anti-tamper-safe. Cost: hot-page faults; toggle/re-arm carefully.)
   b. vtable hook: swap player->vtable[0x18] (per-frame update, called by input thread 0x15bfb40)
      to our fn (save original); runs on game thread every frame. .rdata vtable write needs
      VirtualProtect; may or may not be checksummed.
   c. Try a SINGLE off-thread execCommand anyway: it is actor-local (only touches one actor's
      command slots), unlike the global entity-manager getter, so it MIGHT tolerate off-thread.
      Lower confidence after this crash; test last, expect possible crash.

## SESSION 2026-06-18 pt2: SAFE FIRING = WRITE THE REQUEST TABLE (no call)

- Re-resolved chain (all deterministic this char): controller [0x2205d14]=0x0018d7a8,
  handle=0x7872dd2b, player node=0x021c10ac.
- getter 0x00c15360 = pooled "get-or-create" (alloc size 0x283b8) keyed by handle via
  M=[0x21e7328]=0x021e7438, M->vtable[8]->vtable[7]=allocator 0x013c3e10 (EnterCriticalSection).
  => calling it off our thread races the allocator lock -> CRASH. NEVER call it off-thread.
- **SAFE FIRING PATH (memory write, game thread executes):** skill handlers (e.g. 0x5008a0) do:
  `actor=[dispatcher+0x14]; comp = FUN_0043b2b0(actor) = refcounted ptr @ [actor+0x1b2c];
   SetCommand(comp,...)` which just WRITES a 10-entry request table @ comp+0x32c (stride 0x18:
   byte@+0x32c, dwords@+0x330/+0x334/+0x338). The game's own update thread consumes the request
   and runs execCommand. => We can fire by WRITING that table, no game-function call, no crash.
- Cast-time capture (guard on execCommand page): dominant obj 0x046246b0 (vt 0x01ca3a08, player
  vtable cluster) as esi/edi; player HP/ST param block @ 0x0dfdb718 (display copy).
- Guard-page re-arms fine for DATA faults but NOT for EXEC faults (1 hit then stops) -> can't yet
  catch execCommand entry (ecx=actor) cleanly. Need single-step re-arm OR a one-shot catch.

### NEXT (reliable actor capture, then safe fire)
1. Add one-shot "catch func entry" capture: guard execCommand page; in VEH, when eip==0xc83f90
   log ecx(actor)+stack args and DISARM; for non-target faults, single-step (set EFLAGS.TF, re-arm
   on the SINGLE_STEP exception) so we don't loop and don't miss the target. => actor pointer.
2. From actor: comp=[actor+0x1b2c]; map request table @ comp+0x32c; capture a real skill's request
   fields during a natural cast (guard comp page -- DATA, re-arm works) to learn the field meanings.
3. Fire by WRITING the request table for a chosen skill on a key; validate in-world. Stamina/cost
   handled by the game since it runs the real execCommand.

### TODO to ship the feature
1. Find the live command-player address: it's one of the `entity <id>` components (the one whose
   `+0xec0`+slots look like command ids, vtable has `+0x98/+0x100`). Then `find-vtable <its vtable>`
   for a stable re-find recipe.
2. Read the 8 skills' command ids from the slots (both palettes) -> that's the bind menu.
3. Determine the direction/float args (try player facing or 0).
4. Mod: input-poll bound keys -> set slot id + call `0xc83f90` on the live player (no patching).

### GUARD-PAGE CRASH FIX (our bug, not anti-tamper)
`gpoff` while a hot guard was firing crashed at code `0x80000001` (the guard fault itself) because
disarm zeroed PAGE_BASE and the VEH then DECLINED an in-flight fault on our page -> unhandled.
Fixed: VEH ALWAYS absorbs (CONTINUE_EXECUTION) any guard fault whose addr is in our page, even
when not armed; disarm keeps PAGE_BASE; `rearm` re-checks ARMED; added `RAW_CAP`=4000 auto-disarm
so a hot loop can't storm. Guarding a HOT (every-frame) page is now safe but heavy — prefer event
pages; for hot pages the capture auto-stops after the cap.

## Implementation plan (read-only RE -> direct calls)
1. Locate `cPaletteParams` (vtable `0x1c19140`) on the player; map the 8 skill-id slots + active
   palette index (items A,B).
2. Confirm the fire path: either `mgr->SetCommand(slot,..,CustomSkillId)` (`0x9b59a0`) or
   `0xc83f90(player,..,id,..,vec)`. Get the CustomSkill command ids (item C).
3. In `ddon_mod`, input-poll thread: on a bound key, (optionally) write the chosen skill id into
   the active slot + set palette index, then CALL the fire function on the player (no patching).
4. Validate one hardcoded skill on one key in-world before generalizing to full rebinding.

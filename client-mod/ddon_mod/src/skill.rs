//! Custom-skill fire path (input-thread safe).
//!
//! Static RE traced the live cast chain to `IssuePlayerCommand` @ `0x1541cc0`.
//! Resolve player + keyconfig entry on the **caller** thread; only the final
//! game call runs on the input thread (no alloc/logging there).

use core::sync::atomic::{AtomicU32, AtomicUsize, Ordering};

use windows_sys::Win32::System::LibraryLoader::GetModuleHandleW;

use crate::log;

const IMAGE_BASE_DEFAULT: usize = 0x40_0000;

const VA_ISSUE_PLAYER_CMD: usize = 0x154_1cc0;
const VA_PREP_PLAYER: usize = 0x153_f280;
const VA_SETUP_PLAYER_CMD: usize = 0x154_2020;
const VA_FIRE_PALETTE: usize = 0x14e_d770;
const VA_GAME_ROOT_GLOBAL: usize = 0x220_4c44;
const VA_CONTROLLER_GLOBAL: usize = 0x220_5d14;
const OFF_IN_WORLD: usize = 0x25f1;
/// Toggled on draw/sheath @ 0x15be45d / 0x15be469 (esi==1 -> draw).
const OFF_CTRL_WEAPON_DRAWN: usize = 0x26f7;
const OFF_CTRL_WEAPON_OK: usize = 0xba;
const OFF_CTRL_STATE: usize = 0x24;
const OFF_CTRL_SUBSTATE: usize = 0x28;
const OFF_CTRL_WEAPON_REF: usize = 0x1c8;
const OFF_KEYCONFIG: usize = 0x449a90;
/// Set each frame when weapon is drawn and skill keys may dispatch (@ 0x14ed508).
const OFF_WEAPON_INPUT: usize = 0x38;
/// Cleared at start of input frame; checked before dispatch (@ 0x14ea0eb).
const OFF_WEAPON_SUB: usize = 0x3c;
const OFF_WEAPON_FLAG: usize = 0x36;
/// Non-zero when drawn (literal 1 @ 0x14ead29); required for +0x38 (@ 0x14e75a2).
const OFF_SKILL_MGR_ARM: usize = 0x4599f8;
/// IssuePlayerCommand `ecx` is NOT the cExActor — input handler sets `edi = game + 0x154`.
const OFF_PLAYER_CMD_CTX: usize = 0x154;

const SLOT_ENTRY_OFFS: [usize; 8] = [0x264, 0x268, 0x26c, 0x270, 0x274, 0x278, 0x27c, 0x280];

static PENDING_GAME: AtomicU32 = AtomicU32::new(0);
static PENDING_PLAYER: AtomicU32 = AtomicU32::new(0);
static PENDING_ENTRY: AtomicU32 = AtomicU32::new(0);
static PENDING_ARG_208: AtomicU32 = AtomicU32::new(0);
static PENDING_ARG_198: AtomicU32 = AtomicU32::new(0);

fn va_to_live(va: usize) -> usize {
    unsafe {
        let base = GetModuleHandleW(core::ptr::null()) as usize;
        base + (va - IMAGE_BASE_DEFAULT)
    }
}

fn rd_u32(addr: usize) -> Option<u32> {
    crate::probe::read_u32(addr)
}

fn rd_u8(addr: usize) -> Option<u8> {
    let mut b = [0u8; 1];
    if crate::probe::safe_read_pub(addr, &mut b) {
        Some(b[0])
    } else {
        None
    }
}

fn is_heap(p: usize) -> bool {
    (0x0080_0000..0x7000_0000).contains(&p)
}

pub fn in_world_flag() -> Option<u8> {
    let ctrl = rd_u32(va_to_live(VA_CONTROLLER_GLOBAL))? as usize;
    rd_u8(ctrl + OFF_IN_WORLD)
}

pub fn in_world() -> bool {
    // Input dispatch @ 0x14ed4fb compares against 1, but the input-thread loop only
    // checks != 0. Accept either so casts aren't blocked when the flag is set but not 1.
    in_world_flag().is_some_and(|b| b != 0)
}

pub fn controller() -> Option<usize> {
    let p = rd_u32(va_to_live(VA_CONTROLLER_GLOBAL))? as usize;
    (p != 0).then_some(p)
}

/// True when custom-skill key dispatch is armed (@ 0x14ed508). NOT the same as having the
/// weapon model visible — in town / idle the weapon can be out while this stays 0.
pub fn skill_input_ready() -> bool {
    let Some(game) = game_root() else {
        return false;
    };
    rd_u8(game + OFF_WEAPON_INPUT) == Some(1)
}

/// Prerequisite dword for skill_input_ready (+0x38 setter @ 0x14e75a2).
pub fn skill_input_armed() -> bool {
    let Some(game) = game_root() else {
        return false;
    };
    rd_u32(game + OFF_SKILL_MGR_ARM).is_some_and(|v| v != 0)
}

pub fn game_root() -> Option<usize> {
    let p = rd_u32(va_to_live(VA_GAME_ROOT_GLOBAL))? as usize;
    is_heap(p).then_some(p)
}

/// Command-context pointer passed to IssuePlayerCommand (memory-only; safe off-thread).
pub fn player_command_ctx() -> Option<usize> {
    // Prefer the value refreshed each input frame by the hook (same thread as real casts).
    let cached = CACHED_PLAYER_CTX.load(Ordering::Acquire) as usize;
    if cached != 0 && is_heap(cached) {
        return Some(cached);
    }
    let game = game_root()?;
    let ctx = game + OFF_PLAYER_CMD_CTX;
    is_heap(ctx).then_some(ctx)
}

static CACHED_PLAYER_CTX: AtomicU32 = AtomicU32::new(0);

/// Permanent input-thread fire stub (init thread only).
static FIRE_THUNK: AtomicUsize = AtomicUsize::new(0);
static FIRE_GAME_SLOT: AtomicU32 = AtomicU32::new(0);
static FIRE_PLAYER_SLOT: AtomicU32 = AtomicU32::new(0);
static FIRE_ENTRY_SLOT: AtomicU32 = AtomicU32::new(0);
static FIRE_ARG_208_SLOT: AtomicU32 = AtomicU32::new(0);
static FIRE_ARG_198_SLOT: AtomicU32 = AtomicU32::new(0);

/// Args for SetupPlayerCommand @ 0x1542020: push order matches 0x14e9a58 (208-chain, then 198-chain).
fn keyconfig_setup_args(kc: usize) -> (u32, u32) {
    let mut from_208 = 0u32;
    let obj_a = rd_u32(kc + 0x208).unwrap_or(0) as usize;
    if obj_a != 0 {
        let p = rd_u32(obj_a + 0x48).unwrap_or(0) as usize;
        if p != 0 {
            from_208 = rd_u32(p).unwrap_or(0);
        }
    }
    let mut from_198 = 0u32;
    let obj_b = rd_u32(kc + 0x198).unwrap_or(0) as usize;
    if obj_b != 0 {
        let p = rd_u32(obj_b + 0x44).unwrap_or(0) as usize;
        if p != 0 {
            from_198 = rd_u32(p).unwrap_or(0);
        }
    }
    (from_208, from_198)
}

fn emit_mov_ecx_from(code: &mut Vec<u8>, slot: u32) {
    code.extend_from_slice(&[0x8B, 0x0D]);
    code.extend_from_slice(&slot.to_le_bytes());
}

fn emit_call_abs(code: &mut Vec<u8>, func: u32) {
    code.push(0xB8);
    code.extend_from_slice(&func.to_le_bytes());
    code.extend_from_slice(&[0xFF, 0xD0]);
}

fn emit_push_slot(code: &mut Vec<u8>, slot: u32) {
    code.push(0xFF);
    code.extend_from_slice(&[0x35]);
    code.extend_from_slice(&slot.to_le_bytes());
}

/// Build stub: palette prep + SetupPlayerCommand + IssuePlayerCommand (matches 0x14e9960..0x14e9a77).
pub fn init_issue_thunk() {
    use windows_sys::Win32::System::Memory::{
        VirtualAlloc, MEM_COMMIT, MEM_RESERVE, PAGE_EXECUTE_READWRITE,
    };
    let game_slot = &FIRE_GAME_SLOT as *const AtomicU32 as u32;
    let player_slot = &FIRE_PLAYER_SLOT as *const AtomicU32 as u32;
    let entry_slot = &FIRE_ENTRY_SLOT as *const AtomicU32 as u32;
    let arg_208_slot = &FIRE_ARG_208_SLOT as *const AtomicU32 as u32;
    let arg_198_slot = &FIRE_ARG_198_SLOT as *const AtomicU32 as u32;
    let f_fire = va_to_live(VA_FIRE_PALETTE) as u32;
    let f_prep = va_to_live(VA_PREP_PLAYER) as u32;
    let f_setup = va_to_live(VA_SETUP_PLAYER_CMD) as u32;
    let f_issue = va_to_live(VA_ISSUE_PLAYER_CMD) as u32;

    let mut code = Vec::with_capacity(128);
    // FirePalette(game, player): push player; mov ecx, game; call
    emit_push_slot(&mut code, player_slot);
    emit_mov_ecx_from(&mut code, game_slot);
    emit_call_abs(&mut code, f_fire);
    // PrepPlayer(player)
    emit_mov_ecx_from(&mut code, player_slot);
    emit_call_abs(&mut code, f_prep);
    // SetupPlayerCommand(player, ..., 208-chain, 198-chain) @ 0x14e9a58
    code.extend_from_slice(&[0x6A, 0x00, 0x6A, 0x00]);
    emit_push_slot(&mut code, arg_208_slot);
    emit_push_slot(&mut code, arg_198_slot);
    emit_mov_ecx_from(&mut code, player_slot);
    emit_call_abs(&mut code, f_setup);
    // IssuePlayerCommand(player, entry, 0, 0)
    code.extend_from_slice(&[0x6A, 0x00, 0x6A, 0x00]);
    emit_push_slot(&mut code, entry_slot);
    emit_mov_ecx_from(&mut code, player_slot);
    emit_call_abs(&mut code, f_issue);
    code.push(0xC3);

    unsafe {
        let mem = VirtualAlloc(
            core::ptr::null(),
            code.len(),
            MEM_COMMIT | MEM_RESERVE,
            PAGE_EXECUTE_READWRITE,
        ) as usize;
        if mem != 0 {
            core::ptr::copy_nonoverlapping(code.as_ptr(), mem as *mut u8, code.len());
            FIRE_THUNK.store(mem, Ordering::Release);
        }
    }
}

/// Called from the input-thread hook each frame (allocation-free).
pub fn refresh_player_cache(game_this: usize) {
    if game_this != 0 {
        CACHED_PLAYER_CTX.store((game_this + OFF_PLAYER_CMD_CTX) as u32, Ordering::Release);
    }
}

pub fn keyconfig() -> Option<usize> {
    let game = game_root()?;
    let kc = rd_u32(game + OFF_KEYCONFIG)? as usize;
    is_heap(kc).then_some(kc)
}

pub fn cmd_entry_for_slot(slot: u8) -> Option<usize> {
    if !(1..=8).contains(&slot) {
        return None;
    }
    let kc = keyconfig()?;
    let ent = rd_u32(kc + SLOT_ENTRY_OFFS[(slot - 1) as usize])? as usize;
    (ent != 0 && is_heap(ent)).then_some(ent)
}

/// Input-thread only: full game fire chain, no alloc, no logging.
pub unsafe fn fire_pending_cast() -> u32 {
    let t = FIRE_THUNK.load(Ordering::Acquire);
    if t == 0 {
        return 0;
    }
    let f: extern "C" fn() -> u32 = core::mem::transmute(t);
    f()
}

fn ready_to_cast() -> bool {
    game_root().is_some() && player_command_ctx().is_some()
}

/// Resolve pointers on this thread, then queue the input-thread fire.
pub fn queue_slot(slot: u8) -> Result<(), &'static str> {
    if !(1..=8).contains(&slot) {
        return Err("slot must be 1..8");
    }
    if !ready_to_cast() {
        return Err("not ready (no game root / player ctx)");
    }
    let game = game_root().ok_or("game root null")?;
    let player = player_command_ctx().ok_or("player ctx null")?;
    let kc = keyconfig().ok_or("keyconfig null")?;
    let entry = cmd_entry_for_slot(slot).ok_or("keyconfig entry null")?;
    let (arg_208, arg_198) = keyconfig_setup_args(kc);
    log::line(&format!(
        "[skill] queue slot{} game={:#010x} player={:#010x} entry={:#010x} setup=({:#x},{:#x})",
        slot, game, player, entry, arg_208, arg_198
    ));
    PENDING_GAME.store(game as u32, Ordering::Release);
    PENDING_ARG_208.store(arg_208, Ordering::Release);
    PENDING_ARG_198.store(arg_198, Ordering::Release);
    PENDING_ENTRY.store(entry as u32, Ordering::Release);
    PENDING_PLAYER.store(player as u32, Ordering::Release);
    // Input-thread hook @ 0x14ea090 drains this each frame; no guard-page needed.
    Ok(())
}

/// Called from input-thread hook after each frame. Must stay allocation-free.
pub fn has_pending_cast() -> bool {
    PENDING_PLAYER.load(Ordering::Acquire) != 0
}

pub fn drain_pending() {
    let game = PENDING_GAME.swap(0, Ordering::AcqRel) as usize;
    let player = PENDING_PLAYER.swap(0, Ordering::AcqRel) as usize;
    let entry = PENDING_ENTRY.swap(0, Ordering::AcqRel) as usize;
    let arg_208 = PENDING_ARG_208.swap(0, Ordering::AcqRel);
    let arg_198 = PENDING_ARG_198.swap(0, Ordering::AcqRel);
    if player == 0 || entry == 0 || game == 0 {
        return;
    }
    FIRE_GAME_SLOT.store(game as u32, Ordering::Relaxed);
    FIRE_PLAYER_SLOT.store(player as u32, Ordering::Relaxed);
    FIRE_ENTRY_SLOT.store(entry as u32, Ordering::Relaxed);
    FIRE_ARG_208_SLOT.store(arg_208, Ordering::Relaxed);
    FIRE_ARG_198_SLOT.store(arg_198, Ordering::Relaxed);
    let _ = unsafe { fire_pending_cast() };
}

pub fn log_keyconfig() {
    let Some(game) = game_root() else {
        log::line("[skill] keycfg: game_root unreadable");
        return;
    };
    let Some(kc) = keyconfig() else {
        log::line(&format!("[skill] keycfg: game={:#010x} keyconfig null", game));
        return;
    };
    let pl = player_command_ctx().unwrap_or(0);
    let cached = CACHED_PLAYER_CTX.load(Ordering::Relaxed);
    log::line(&format!(
        "[skill] keycfg: game={:#010x} keyconfig={:#010x} player_ctx={:#010x} cached={:#010x}",
        game, kc, pl, cached
    ));
    log_weapon_state();
    for (i, off) in SLOT_ENTRY_OFFS.iter().enumerate() {
        let v = rd_u32(kc + off).unwrap_or(0);
        log::line(&format!(
            "[skill]   slot{} (+{:#x}) -> {:#010x}",
            i + 1,
            off,
            v
        ));
    }
}

/// One-line weapon / skill-input snapshot for live sheath-draw tests.
pub fn log_weapon_state() {
    let Some(game) = game_root() else {
        log::line("[skill] weap: game_root unreadable");
        return;
    };
    let pl = player_command_ctx().unwrap_or(0);
    let cached = CACHED_PLAYER_CTX.load(Ordering::Relaxed);
    let wi = rd_u8(game + OFF_WEAPON_INPUT).unwrap_or(0);
    let ws = rd_u8(game + OFF_WEAPON_SUB).unwrap_or(0);
    let wf = rd_u8(game + OFF_WEAPON_FLAG).unwrap_or(0);
    let arm = rd_u32(game + OFF_SKILL_MGR_ARM).unwrap_or(0);
    let iw = in_world_flag().unwrap_or(0);
    let (cba, cst, csub, cref, cwd) = controller().map_or((0, 0, 0, 0, 0), |c| {
        (
            rd_u8(c + OFF_CTRL_WEAPON_OK).unwrap_or(0),
            rd_u32(c + OFF_CTRL_STATE).unwrap_or(0),
            rd_u32(c + OFF_CTRL_SUBSTATE).unwrap_or(0),
            rd_u32(c + OFF_CTRL_WEAPON_REF).unwrap_or(0),
            rd_u8(c + OFF_CTRL_WEAPON_DRAWN).unwrap_or(0),
        )
    });
    log::line(&format!(
        "[skill] weap: skill_input(+0x38)={} (raw={}) armed(+0x4599f8)={:#x} sub(+0x3c)={} flag(+0x36)={} \
         ctrl_stance(+0x26f7)={} in_world(+0x25f1)={:#04x} ctrl ba={} state={:#x} sub={:#x} weapon_ref={:#010x} \
         player_ctx={:#010x} cached={:#010x}  (26f7/38 != weapon visible)",
        skill_input_ready(),
        wi,
        arm,
        ws,
        wf,
        cwd,
        iw,
        cba,
        cst,
        csub,
        cref,
        pl,
        cached
    ));
}

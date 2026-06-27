#Requires AutoHotkey v2.0
#SingleInstance Force

; DDO.exe runs as administrator. Windows blocks input sent from a
; non-elevated app to an elevated one, so this script MUST run elevated
; too or none of its keypresses reach the game. Auto-elevate on launch:
if (!A_IsAdmin) {
    try {
        if A_IsCompiled
            Run('*RunAs "' A_ScriptFullPath '" /restart')
        else
            Run('*RunAs "' A_AhkPath '" /restart "' A_ScriptFullPath '"')
    }
    ExitApp()
}

; ============================================================
;  DDON Skill Palette Hotkeys
; ------------------------------------------------------------
;  Adds number keys to fire all 8 custom-skill palette slots,
;  WITHOUT replacing your existing Shift/Ctrl + mouse controls.
;
;     1-4  /  Numpad 1-4   ->  MAIN palette slots 1-4
;     5-8  /  Numpad 5-8   ->  SUB  palette slots 1-4
;
;  IMPORTANT - how this has to work:
;  In DDON, switching palettes is a SLOW move (hold B ~2-3s, then a
;  ~3-4s animation). It is NOT something that can happen per-keypress.
;  So the script remembers which palette you are on and only performs
;  the swap when you change groups:
;    - On MAIN, keys 1-4 fire instantly. First time you press 5-8 it
;      runs the full ~6s swap to SUB, then 5-8 fire instantly.
;    - Pressing 1-4 again runs the ~6s swap back to MAIN.
;  Inputs pressed during a swap are ignored (you're locked in the
;  swap animation anyway).
;
;  The script assumes you START on the MAIN palette. If the game ever
;  resets your palette (job change, zone, login) press the RESYNC
;  hotkey below so the script and game agree again.
;
;  RESYNC to main  :  Ctrl + Alt + Home
;  Emergency / pause:  Ctrl + Alt + End
; ============================================================


; ====================== CONFIG ==============================
; Game process. Hotkeys are active only while this window is focused.
GameExe := "DDO.exe"

; How each MAIN palette slot is triggered in-game (DDON defaults):
;     Slot 1 = Shift + Left Click     Slot 2 = Shift + Right Click
;     Slot 3 = Ctrl  + Left Click     Slot 4 = Ctrl  + Right Click
; mod = "" if a slot uses no modifier; btn = LButton / RButton / a key.
MainSkill := Map(
    1, { mod: "LShift",   btn: "LButton" },
    2, { mod: "LShift",   btn: "RButton" },
    3, { mod: "LControl", btn: "LButton" },
    4, { mod: "LControl", btn: "RButton" }
)

; The in-game "Custom Skill Palette Switch (Long Press)" key (default B).
PaletteKey := "b"

; Palette-swap timing (milliseconds) - tuned to DDON's slow swap.
;   PaletteHoldMs   = how long to HOLD B to start the swap (the "long press").
;   SwapAnimationMs = how long the swap animation takes before the other
;                     palette is usable. Wait this long after releasing B.
; If swaps don't trigger, raise PaletteHoldMs. If sub skills fire before the
; animation finishes (and hit the wrong palette), raise SwapAnimationMs.
PaletteHoldMs   := 2600
SwapAnimationMs := 4200

; Small delay after pressing a modifier, before the click.
ModDelay := 25
; ==================== END CONFIG ============================


; --- runtime state ---
CurrentPalette := "main"   ; assume you start on main
Swapping       := false    ; true while a swap is in progress

; key name -> [ slot, palette ]
TopRow := Map(
    "1", [1, "main"], "2", [2, "main"], "3", [3, "main"], "4", [4, "main"],
    "5", [1, "sub"],  "6", [2, "sub"],  "7", [3, "sub"],  "8", [4, "sub"]
)
NumpadOn := Map(
    "Numpad1", [1, "main"], "Numpad2", [2, "main"], "Numpad3", [3, "main"], "Numpad4", [4, "main"],
    "Numpad5", [1, "sub"],  "Numpad6", [2, "sub"],  "Numpad7", [3, "sub"],  "Numpad8", [4, "sub"]
)
NumpadOff := Map(
    "NumpadEnd",   [1, "main"], "NumpadDown",  [2, "main"], "NumpadPgdn", [3, "main"], "NumpadLeft", [4, "main"],
    "NumpadClear", [1, "sub"],  "NumpadRight", [2, "sub"],  "NumpadHome", [3, "sub"],  "NumpadUp",   [4, "sub"]
)

HotIfWinActive("ahk_exe " GameExe)
RegisterAll(TopRow)
RegisterAll(NumpadOn)
RegisterAll(NumpadOff)
HotIf()

TrayTip("On MAIN palette. 1-4 instant; 5-8 will swap to SUB (~6s) the first time."
      , "DDON Skill Palette Hotkeys active")

RegisterAll(m) {
    for key, data in m
        Hotkey(key, Fire.Bind(data[1], data[2], key))
}

Fire(slot, palette, keyName, *) {
    global MainSkill, CurrentPalette, Swapping, ModDelay

    ; Ignore presses while a swap is animating
    if (Swapping)
        return

    if (CurrentPalette != palette)
        EnsurePalette(palette)   ; ~6s swap, then fall through and fire

    info := MainSkill[slot]
    if (info.mod != "")
        Send("{" info.mod " Down}")
    Sleep(ModDelay)
    Send("{" info.btn " Down}")
    KeyWait(StripPrefix(keyName))   ; hold the skill while the number key is held
    Send("{" info.btn " Up}")
    if (info.mod != "")
        Send("{" info.mod " Up}")
}

EnsurePalette(target) {
    global CurrentPalette, Swapping, PaletteKey, PaletteHoldMs, SwapAnimationMs
    Swapping := true
    ToolTip("Swapping to " StrUpper(target) " palette...")
    Send("{" PaletteKey " Down}")
    Sleep(PaletteHoldMs)
    Send("{" PaletteKey " Up}")
    Sleep(SwapAnimationMs)
    CurrentPalette := target
    Swapping := false
    ToolTip()
    TrayTip("Now on " StrUpper(target) " palette.", "DDON Skill Palette Hotkeys")
}

StripPrefix(k) {
    return RegExReplace(k, "^[~$*]+", "")
}

; --- Resync: tell the script you are back on the MAIN palette (no input sent) ---
^!Home:: {
    global CurrentPalette, Swapping
    Swapping := false
    CurrentPalette := "main"
    ToolTip()
    TrayTip("Resynced: assuming MAIN palette.", "DDON Skill Palette Hotkeys")
}

; --- Emergency: release everything, reset to main, pause/resume ---
^!End:: {
    global CurrentPalette, Swapping, PaletteKey
    for k in ["LShift", "LControl", "LButton", "RButton", PaletteKey]
        Send("{" k " Up}")
    Swapping := false
    CurrentPalette := "main"
    ToolTip()
    Suspend(-1)
    TrayTip(A_IsSuspended ? "Suspended - hotkeys off" : "Resumed - hotkeys on (MAIN)"
          , "DDON Skill Palette Hotkeys")
}

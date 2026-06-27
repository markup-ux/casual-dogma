# DDON Skill Palette Hotkeys

Adds number keys **1–8** (top row **and** numpad) to fire all eight custom-skill
palette slots in Dragon's Dogma Online — **in addition to** your existing
Shift/Ctrl + mouse controls. Nothing is replaced or rebound in-game.

| Key (top row or numpad) | Fires |
|---|---|
| `1` `2` `3` `4` | Main palette slots 1–4 |
| `5` `6` `7` `8` | Sub palette slots 1–4 |

For keys **5–8** the script automatically swaps to the sub palette, fires the
skill, and swaps back — so you never switch palettes by hand. Hold a number key
to hold a charged skill.

Hotkeys are active **only while the game window (`DDO.exe`) is focused**, so the
number keys still type normally everywhere else.

## Why this approach

The palette toggle, key bindings, and the "4 skills active at a time" limit live
inside `DDO.exe`, not in the server. Rather than patching the executable (risky,
can crash or trip anti-cheat), this is a lightweight external input layer. It
drives the game's *existing* skill inputs faster than a human could, which is why
all 8 slots become reachable from the number keys without you touching palettes.

## Setup

1. ~~Install AutoHotkey v2~~ — **DONE.** AutoHotkey v2.0.26 is already installed
   (per-user) and the script passed AHK's syntax validation. If you ever need it
   again: https://www.autohotkey.com/
2. **Palette switch — nothing to bind.** DDON's "Custom Skill Palette Switch
   (Long Press)" action is already bound to the **B** key by default, so the
   script is set to `PaletteKey := "b"` and `PaletteMode := "longpress"` to match.
   - It's a *long press* action, so the script briefly holds B (~350 ms) to swap.
   - If you rebound that action, set `PaletteKey` to your key. The game rejects
     some keys (e.g. backslash) for these actions, which is why we use B.
3. **(Only if you changed defaults)** If you ever remapped your *skill* inputs
   away from the defaults (Shift/Ctrl + Left/Right click), update the `MainSkill`
   map at the top of the script to match.
4. **Run it.** Double-click `SkillPaletteHotkeys.ahk`. A green "H" icon appears in
   the system tray = running. Launch the game (before or after, order doesn't
   matter) and use `1`–`8`.

> Heads-up on keys 5–8: because the palette switch is a *long press*, sub-palette
> skills carry noticeable lag (long-press B → fire → long-press B back, ~0.8 s of
> overhead). That's a game limitation, not the script. Main-palette skills (1–4)
> have no such delay. Tune `LongPressMs` if the swap doesn't register (raise) or
> feels too sluggish (lower).

## Controls / tray

- **Ctrl + Alt + End** — emergency release of all held keys and pause/resume the
  script (toggle). Use this if an input ever feels "stuck."
- Right-click the tray icon to **Pause**, **Edit**, **Reload**, or **Exit**.

## Tuning (top of the script)

| Setting | What it does |
|---|---|
| `GameExe` | Process the hotkeys are scoped to. Default `DDO.exe`. |
| `MainSkill` | Maps each slot 1–4 to its in-game modifier + button. |
| `PaletteKey` | The key that swaps Main ⇄ Sub. Default `b` (DDON's default bind). |
| `PaletteMode` | `"longpress"` (DDON default), `"toggle"` (tap), or `"hold"`. |
| `LongPressMs` | How long B is held to register a long press. Default `350`. |
| `ModDelay` / `PaletteDelay` | Input timing in ms. Raise if inputs drop. |

## Troubleshooting

- **Keys 5–8 fire a main-palette skill instead of sub:** the long press isn't
  registering. Raise `LongPressMs` (try 450–550), and confirm `PaletteKey`
  matches the in-game "Custom Skill Palette Switch" bind (default `b`). If you
  rebound the switch to a non-long-press key, set `PaletteMode := "toggle"`.
- **Skills don't trigger at all:** confirm the game window is focused, `GameExe`
  is correct, and `MainSkill` matches your actual skill bindings. If the game
  ignores synthetic input, try raising `ModDelay`/`PaletteDelay` to ~50/120.
- **Number keys do something else in-game too:** the script consumes the number
  keys while the game is focused, so their default in-game action won't also fire.
  If you *want* a number key's old behavior back, remove it from the maps in the
  script.

## Run on startup (optional)

Press `Win + R`, type `shell:startup`, and drop a shortcut to
`SkillPaletteHotkeys.ahk` into that folder.

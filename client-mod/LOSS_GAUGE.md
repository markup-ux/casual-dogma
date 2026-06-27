# Loss gauge / recoverable HP (gray-white bar)

## What you see on the HUD

The HP bar has three visual zones (left to right):

| Zone | Meaning |
|------|---------|
| **Green** | Current HP — drops when you take damage |
| **Gray** | Healable gap — Priests, rest, potions can refill green up to the white cap line |
| **Dark** | Permanent loss — damage that consumed the loss gauge; cannot be healed back |

The **thin white vertical tick** on the bar is the **recoverable HP ceiling**. When the loss gauge mechanic fires, that tick moves left and the dark zone grows.

## Memory layout (client RE, stable across sessions)

From `RE_OFFLINE_GUIDE.md` / live `hp` probe:

| Field | Offset (char object) | Param block |
|-------|----------------------|-------------|
| Current HP | `+0x7e8` | `+0x00` |
| Recoverable cap (loss-gauge ceiling) | `+0x7f0` | `+0x08` |
| Absolute max HP | `+0x7f8` | `+0x10` |

Param block address = `char + 0x7e8`. Character object = `param - 0x7e8`.

## Server vs client

- **Combat HP is client-authoritative.** The server mirrors periodic RPC updates and can relay corrected packets to *other* players.
- **Your own HUD** reads live memory (`char+0x7f0`). RPC echo alone does not move your local white tick.
- **Sub-cap pin (job level &lt; 120):** server clamps `WhiteHP` in DB/RPC; **elevated applier** keeps `+0x7f0` pinned to max every frame while DDO is open.

## Settings

- Server: `DisableRecoverableHpLossBelowMaxLevel` (default `true`)
- Signal file: `client-mod/sync/recoverable_hp_state.json`
- Applier log: `client-mod/applier/applier.log` — look for `loss-gauge cap X -> Y`

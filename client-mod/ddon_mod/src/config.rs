//! Binding configuration: maps any key / controller button to a custom-skill action.
//!
//! File: C:\Users\Public\ddon_bindings.ini  (created with defaults if missing).
//! Format, one binding per line:
//!     <input> = <action>
//!   input   : a key name (e.g. `1`, `F5`, `Numpad7`, `MouseX1`) or pad button (`Pad:A`)
//!   action  : `slot1`..`slot8`  (1-4 = main palette, 5-8 = sub palette)
//!           : `skill:<id>`      (direct cast of a custom-skill id; needs direct backend)
//! Lines starting with `#` are comments.

use std::collections::HashMap;

#[derive(Clone, Copy, PartialEq, Eq, Hash, Debug)]
pub enum Input {
    /// Win32 virtual-key code (keyboard or mouse X buttons).
    Vk(u32),
    /// XInput button bitmask (single bit).
    Pad(u16),
}

#[derive(Clone, Copy, PartialEq, Eq, Debug)]
pub enum Action {
    /// Palette slot 1..=8 (1-4 main, 5-8 sub). Backend-agnostic.
    Slot(u8),
    /// Direct custom-skill id cast (direct backend only).
    Skill(u32),
}

#[derive(Clone, Default)]
pub struct Bindings {
    pub map: HashMap<Input, Action>,
}

pub const CONFIG_PATH: &str = "C:\\Users\\Public\\ddon_bindings.ini";

pub const DEFAULT_CONFIG: &str = "\
# DDON custom-skill bindings. Edit and save; changes apply live (~1s).
# input = action     |  slot1-4 = main palette, slot5-8 = sub palette
# Keyboard top-row + numpad fire all 8 slots from both palettes, no swapping:
1 = slot1
2 = slot2
3 = slot3
4 = slot4
5 = slot5
6 = slot6
7 = slot7
8 = slot8
Numpad1 = slot1
Numpad2 = slot2
Numpad3 = slot3
Numpad4 = slot4
Numpad5 = slot5
Numpad6 = slot6
Numpad7 = slot7
Numpad8 = slot8
# Controller example (uncomment to use):
# Pad:A = slot1
# Pad:B = slot2
# Pad:X = slot3
# Pad:Y = slot4
";

/// Parse one input token into an `Input`. Returns None if unrecognized.
pub fn parse_input(tok: &str) -> Option<Input> {
    let t = tok.trim();
    if let Some(rest) = t.strip_prefix("Pad:").or_else(|| t.strip_prefix("pad:")) {
        return parse_pad(rest).map(Input::Pad);
    }
    parse_vk(t).map(Input::Vk)
}

fn parse_action(tok: &str) -> Option<Action> {
    let t = tok.trim();
    if let Some(rest) = t.strip_prefix("slot") {
        if let Ok(n) = rest.trim().parse::<u8>() {
            if (1..=8).contains(&n) {
                return Some(Action::Slot(n));
            }
        }
        return None;
    }
    if let Some(rest) = t.strip_prefix("skill:") {
        let r = rest.trim();
        let parsed = if let Some(hex) = r.strip_prefix("0x") {
            u32::from_str_radix(hex, 16).ok()
        } else {
            r.parse::<u32>().ok()
        };
        return parsed.map(Action::Skill);
    }
    None
}

/// Read the bindings file, creating it with defaults if it does not exist yet.
pub fn load() -> Bindings {
    match std::fs::read_to_string(CONFIG_PATH) {
        Ok(text) => parse(&text),
        Err(_) => {
            let _ = std::fs::write(CONFIG_PATH, DEFAULT_CONFIG);
            parse(DEFAULT_CONFIG)
        }
    }
}

pub fn parse(text: &str) -> Bindings {
    let mut b = Bindings::default();
    for raw in text.lines() {
        let line = raw.trim();
        if line.is_empty() || line.starts_with('#') {
            continue;
        }
        let Some((lhs, rhs)) = line.split_once('=') else { continue };
        let (Some(input), Some(action)) = (parse_input(lhs), parse_action(rhs)) else {
            continue;
        };
        b.map.insert(input, action);
    }
    b
}

/// XInput button bit names. See XINPUT_GAMEPAD wButtons.
fn parse_pad(name: &str) -> Option<u16> {
    Some(match name.trim().to_ascii_uppercase().as_str() {
        "A" => 0x1000,
        "B" => 0x2000,
        "X" => 0x4000,
        "Y" => 0x8000,
        "LB" | "L1" => 0x0100,
        "RB" | "R1" => 0x0200,
        "BACK" | "SELECT" => 0x0020,
        "START" => 0x0010,
        "LS" | "L3" => 0x0040,
        "RS" | "R3" => 0x0080,
        "UP" | "DPADUP" => 0x0001,
        "DOWN" | "DPADDOWN" => 0x0002,
        "LEFT" | "DPADLEFT" => 0x0004,
        "RIGHT" | "DPADRIGHT" => 0x0008,
        _ => return None,
    })
}

/// Map a key name to a Win32 virtual-key code. Covers the common cases; extend freely.
fn parse_vk(name: &str) -> Option<u32> {
    let n = name.trim();
    if n.len() == 1 {
        let c = n.chars().next().unwrap().to_ascii_uppercase();
        if c.is_ascii_alphanumeric() {
            return Some(c as u32); // VK for '0'-'9' and 'A'-'Z' equal their ASCII.
        }
    }
    let up = n.to_ascii_uppercase();
    Some(match up.as_str() {
        "SPACE" => 0x20,
        "TAB" => 0x09,
        "ENTER" | "RETURN" => 0x0D,
        "ESC" | "ESCAPE" => 0x1B,
        "SHIFT" => 0x10,
        "LSHIFT" => 0xA0,
        "RSHIFT" => 0xA1,
        "CTRL" | "CONTROL" => 0x11,
        "LCTRL" => 0xA2,
        "RCTRL" => 0xA3,
        "ALT" => 0x12,
        "MOUSEX1" | "XBUTTON1" => 0x05,
        "MOUSEX2" | "XBUTTON2" => 0x06,
        "UP" => 0x26,
        "DOWN" => 0x28,
        "LEFT" => 0x25,
        "RIGHT" => 0x27,
        "NUMPAD0" => 0x60,
        "NUMPAD1" => 0x61,
        "NUMPAD2" => 0x62,
        "NUMPAD3" => 0x63,
        "NUMPAD4" => 0x64,
        "NUMPAD5" => 0x65,
        "NUMPAD6" => 0x66,
        "NUMPAD7" => 0x67,
        "NUMPAD8" => 0x68,
        "NUMPAD9" => 0x69,
        _ => {
            if let Some(num) = up.strip_prefix('F') {
                if let Ok(f) = num.parse::<u32>() {
                    if (1..=24).contains(&f) {
                        return Some(0x70 + (f - 1)); // VK_F1 = 0x70
                    }
                }
            }
            return None;
        }
    })
}

//! Key / controller binding poll loop. Queues casts for the input-thread hook in `skill.rs`.

use std::collections::HashMap;
use std::time::{Duration, Instant};

use windows_sys::Win32::UI::Input::KeyboardAndMouse::GetAsyncKeyState;

use crate::config::{load, Action, Bindings, Input};
use crate::log;
use crate::skill;

/// Poll interval for binding file reload + key state.
const POLL_MS: u64 = 16;
/// Per-binding debounce (ms).
const DEBOUNCE_MS: u64 = 120;

pub fn spawn() {
    std::thread::spawn(bind_loop);
}

fn bind_loop() {
    let mut bindings: Bindings = load();
    let mut last_reload = Instant::now();
    let mut edge: HashMap<Input, bool> = HashMap::new();
    let mut last_fire: HashMap<Input, Instant> = HashMap::new();
    log::line("[bind] poll loop started");
    // Let the game finish loading before we start polling keys.
    std::thread::sleep(std::time::Duration::from_secs(5));

    loop {
        std::thread::sleep(Duration::from_millis(POLL_MS));
        if !crate::probe::is_latest() {
            continue;
        }
        if last_reload.elapsed() >= Duration::from_secs(1) {
            bindings = load();
            last_reload = Instant::now();
        }
        for (input, action) in &bindings.map {
            let down = input_down(*input);
            let was = edge.get(input).copied().unwrap_or(false);
            edge.insert(*input, down);
            if !down || was {
                continue;
            }
            let now = Instant::now();
            if last_fire
                .get(input)
                .is_some_and(|t| now.duration_since(*t).as_millis() < DEBOUNCE_MS as u128)
            {
                continue;
            }
            last_fire.insert(*input, now);
            match action {
                Action::Slot(n) => {
                    if skill::queue_slot(*n).is_ok() {
                        log::line(&format!("[bind] key -> slot{} queued", n));
                    }
                }
                Action::Skill(id) => {
                    log::line(&format!("[bind] skill:{} (direct id not wired yet)", id));
                }
            }
        }
    }
}

fn input_down(input: Input) -> bool {
    match input {
        Input::Vk(vk) => unsafe { GetAsyncKeyState(vk as i32) as u16 & 0x8000 != 0 },
        Input::Pad(_bit) => false, // XInput poll TBD
    }
}

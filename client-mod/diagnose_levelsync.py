#!/usr/bin/env python3
"""
diagnose_levelsync.py - Read DDON level-sync logs and live state, then report likely problems.

Scans:
  - client-mod/sync/sync_state.json (server sync signal)
  - client-mod/applier/applier.log (client applier)
  - game server log files ([LEVELSYNC] lines, enemy spawns, stage 133 / caution spots)
  - DDONLevelSyncApplier scheduled task, DDO.exe, applier process

Usage:
  python diagnose_levelsync.py
  python diagnose_levelsync.py --server-log "D:\\path\\to\\latest.log.txt"
  python diagnose_levelsync.py --json --tail 500

Report is printed and written to client-mod/diagnose_report.txt
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from pathlib import Path
from typing import Iterable, List, Optional, Sequence, Tuple

ROOT = Path(__file__).resolve().parent
DEFAULT_SYNC = ROOT / "sync" / "sync_state.json"
DEFAULT_HP_SYNC = ROOT / "sync" / "recoverable_hp_state.json"
DEFAULT_APPLIER_LOG = ROOT / "applier" / "applier.log"
DEFAULT_REPORT = ROOT / "diagnose_report.txt"

GOOD_APPLIER_BUILD = "2026-06-27-status-scan2"
BAD_APPLIER_MARKERS = (
    "emLvUpOff",
    "spawnClamp",
    "disable_em",
    "clamp_enemy",
    "scale_em_param",
)
TASK_NAME = "DDONLevelSyncApplier"
STAGE_LIGHTHOUSE_OLD_WELL = 133
CAUTION_QUEST_ID = 79000001
REC_LV_OLD_WELL = 12

SERVER_LOG_GLOBS = (
    ROOT.parent / "Server" / "Arrowgene.Ddon.Cli" / "bin" / "Release" / "net10.0" / "Logs" / "*.log.txt",
    ROOT.parent / "Server" / "Arrowgene.Ddon.Cli" / "bin" / "Debug" / "net10.0" / "Logs" / "*.log.txt",
    ROOT.parent / "Client" / "Dragon's Dogma Online" / "nativePC" / "Server" / "Logs" / "*.log.txt",
    ROOT.parent / "Server" / "Logs" / "*.log.txt",
    ROOT.parent / "Logs" / "*.log.txt",
)

LOG_LINE = re.compile(
    r"^(?P<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}) - \w+ - (?P<component>[^:]+): (?P<msg>.*)$"
)
APPLIER_BUILD = re.compile(r"\[applier\] build=([^\s]+)")
LEVELSYNC = re.compile(r"\[LEVELSYNC\]\s*(.*)")
ENEMY_CAP = re.compile(
    r"enemy cap stage=(?P<stage>\d+) idx=(?P<idx>\d+) enemyId=(?P<eid>\S+) "
    r"lv (?P<from>\d+)->(?P<to>\d+) manualSet=true recLv=(?P<rec>\d+)"
)
STAGE_LINE = re.compile(r"StageId=(?P<layout>[\d.]+), SubGroupId=(?P<sub>\d+)")
SPAWN_LIST = re.compile(
    r"spawn-list stage=(?P<layout>[\d.]+) sub=(?P<sub>\d+) quest=(?P<quest>\S+) "
    r"enemies=\[(?P<enemies>[^\]]*)\]"
)


@dataclass
class Finding:
    severity: str  # OK, INFO, WARN, ERROR, CRIT
    code: str
    message: str
    fix: str = ""

    def format(self) -> str:
        line = f"[{self.severity}] {self.code}: {self.message}"
        if self.fix:
            line += f"\n         -> {self.fix}"
        return line


@dataclass
class Diagnosis:
    findings: List[Finding] = field(default_factory=list)
    meta: dict = field(default_factory=dict)

    def add(self, severity: str, code: str, message: str, fix: str = "") -> None:
        self.findings.append(Finding(severity, code, message, fix))

    def sort_key(self, f: Finding) -> Tuple[int, str]:
        order = {"CRIT": 0, "ERROR": 1, "WARN": 2, "INFO": 3, "OK": 4}
        return (order.get(f.severity, 9), f.code)


def read_text_tail(path: Path, max_lines: int) -> List[str]:
    if not path.is_file():
        return []
    try:
        with path.open("r", encoding="utf-8", errors="replace") as fh:
            lines = fh.readlines()
    except OSError:
        return []
    if max_lines <= 0 or len(lines) <= max_lines:
        return [ln.rstrip("\r\n") for ln in lines]
    return [ln.rstrip("\r\n") for ln in lines[-max_lines:]]


def discover_server_log(explicit: Optional[Path], tail_hint: int = 400) -> Optional[Path]:
    if explicit:
        return explicit if explicit.is_file() else None
    env = os.environ.get("DDON_SERVER_LOG")
    if env:
        p = Path(env)
        if p.is_file():
            return p
    candidates: List[Path] = []
    for pattern in SERVER_LOG_GLOBS:
        if pattern.parent.is_dir():
            candidates.extend(pattern.parent.glob(pattern.name))
    if not candidates:
        return None
    candidates = sorted(set(candidates), key=lambda p: p.stat().st_mtime, reverse=True)

    markers = ("[LEVELSYNC]", "StageId=", "LevelSyncManager", "InstanceGetEnemySetListHandler")
    for path in candidates[:12]:
        sample = read_text_tail(path, tail_hint)
        if any(any(m in ln for m in markers) for ln in sample):
            return path
    return candidates[0]


def run_powershell(script: str) -> str:
    try:
        out = subprocess.check_output(
            ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script],
            stderr=subprocess.STDOUT,
            text=True,
            timeout=15,
        )
        return out.strip()
    except (subprocess.SubprocessError, OSError):
        return ""


def process_running(name: str) -> bool:
    out = run_powershell(f"Get-Process -Name '{name}' -ErrorAction SilentlyContinue | Select-Object -First 1 Id")
    return bool(out.strip())


def applier_process_running() -> bool:
    out = run_powershell(
        "Get-CimInstance Win32_Process | Where-Object { "
        "$_.CommandLine -and ($_.CommandLine -like '*apply_sync*') "
        "} | Select-Object -First 1 ProcessId"
    )
    return bool(out.strip())


def query_scheduled_task() -> dict:
    info = {"exists": False, "state": "", "last_result": "", "action": ""}
    out = run_powershell(
        f"$t = Get-ScheduledTask -TaskName '{TASK_NAME}' -ErrorAction SilentlyContinue; "
        "if ($t) { "
        "  $i = Get-ScheduledTaskInfo -TaskName $t.TaskName; "
        "  $a = ($t.Actions | Select-Object -First 1).Execute + ' ' + ($t.Actions | Select-Object -First 1).Arguments; "
        "  Write-Output ('exists=1|state=' + $t.State + '|last=' + $i.LastTaskResult + '|action=' + $a) "
        "} else { Write-Output 'exists=0' }"
    )
    if not out:
        return info
    for part in out.split("|"):
        if part.startswith("exists="):
            info["exists"] = part.split("=", 1)[1] == "1"
        elif part.startswith("state="):
            info["state"] = part.split("=", 1)[1]
        elif part.startswith("last="):
            info["last_result"] = part.split("=", 1)[1]
        elif part.startswith("action="):
            info["action"] = part.split("=", 1)[1].strip()
    return info


def parse_sync_state(path: Path) -> Tuple[Optional[dict], Optional[str]]:
    if not path.is_file():
        return None, "file missing"
    try:
        with path.open("r", encoding="utf-8") as fh:
            data = json.load(fh)
    except (OSError, json.JSONDecodeError) as ex:
        return None, str(ex)
    if not isinstance(data, dict):
        return None, "expected JSON object"
    return data, None


def analyze_sync_state(data: Optional[dict], err: Optional[str], diag: Diagnosis) -> None:
    if err:
        diag.add("WARN", "SYNC_MISSING", f"sync_state.json not usable: {err}",
                 "Enter a level-sync dungeon so the server writes the signal, or check server LevelSync settings.")
        return
    if not data:
        diag.add("INFO", "SYNC_EMPTY", "sync_state.json is empty — not in a level-sync zone right now.",
                 "Enter Lighthouse Old Well (rec Lv 12) or another synced dungeon, then re-run diagnose.")
        return

    for name, row in data.items():
        if not isinstance(row, dict):
            continue
        synced = bool(row.get("Synced"))
        rec = int(row.get("RecLevel") or 0)
        true_lv = int(row.get("TrueLevel") or 0)
        phys = float(row.get("PhysFactor") or 1)
        mag = float(row.get("MagFactor") or 1)

        if rec > 0 and not synced and true_lv < rec:
            diag.add(
                "INFO",
                "SYNC_ZONE_CAP",
                f"{name}: zone enemy cap active (RecLevel={rec}, TrueLevel={true_lv}, Synced=false). "
                "Player combat is NOT scaled; server should cap enemy levels.",
                "If enemies still show Lv 60, check server [LEVELSYNC] enemy cap lines and leave/re-enter the area.",
            )
        elif synced:
            diag.add(
                "OK",
                "SYNC_PLAYER",
                f"{name}: full player sync active (RecLevel={rec}, TrueLevel={true_lv}, "
                f"PhysFactor={phys:.3f}, MagFactor={mag:.3f}).",
            )
        elif rec == 0:
            diag.add("INFO", "SYNC_OFF", f"{name}: no level-sync zone (RecLevel=0).")


def analyze_applier_log(lines: Sequence[str], diag: Diagnosis) -> dict:
    stats = {
        "build": None,
        "mode": None,
        "bad_hits": [],
        "last_sync": None,
        "last_cleared": None,
        "loop_errors": 0,
        "open_process_failed": False,
    }
    if not lines:
        diag.add(
            "WARN",
            "APPLIER_NO_LOG",
            f"No applier log at {DEFAULT_APPLIER_LOG}",
            "Start the applier (scheduled task DDONLevelSyncApplier) or run apply_sync.py elevated.",
        )
        return stats

    for ln in lines:
        m = APPLIER_BUILD.search(ln)
        if m:
            stats["build"] = m.group(1)
        if "file mode signal=" in ln:
            stats["mode"] = "file"
        elif "network mode server=" in ln:
            stats["mode"] = "network"
        for marker in BAD_APPLIER_MARKERS:
            if marker in ln:
                stats["bad_hits"].append((marker, ln))
        if "[applier] SYNC '" in ln:
            stats["last_sync"] = ln
        if "sync cleared -> restored" in ln:
            stats["last_cleared"] = ln
        if "OpenProcess failed" in ln:
            stats["open_process_failed"] = True
        if "loop error:" in ln:
            stats["loop_errors"] += 1

    build = stats["build"]
    if build is None:
        diag.add(
            "WARN",
            "APPLIER_NO_BUILD",
            "Applier log has no build= line (very old or truncated log).",
            "Restart DDONLevelSyncApplier so applier.log starts fresh with build id.",
        )
    elif build != GOOD_APPLIER_BUILD:
        diag.add(
            "ERROR",
            "APPLIER_OLD_BUILD",
            f"Applier build={build} (expected {GOOD_APPLIER_BUILD}).",
            "Rebuild/redeploy apply_sync.py or apply_sync.exe, then restart the scheduled task.",
        )
    else:
        diag.add("OK", "APPLIER_BUILD", f"Applier build={build} (current).")

    if stats["bad_hits"]:
        sample = stats["bad_hits"][-1][0]
        diag.add(
            "CRIT",
            "APPLIER_ENEMY_HACK",
            f"Applier log contains enemy memory patches ({sample}, {len(stats['bad_hits'])} hits). "
            "This breaks mob AI (passive enemies).",
            "Use build 2026-06-26-aifix only (player attack scaling). Restart task, leave dungeon, re-enter.",
        )

    if stats["open_process_failed"]:
        diag.add(
            "ERROR",
            "APPLIER_NOT_ELEVATED",
            "Applier could not OpenProcess — not running elevated or DDO not found.",
            "Reinstall scheduled task (setup.cmd) so it runs with highest privileges.",
        )

    if stats["loop_errors"]:
        diag.add("WARN", "APPLIER_LOOP_ERRORS", f"Applier logged {stats['loop_errors']} loop error(s).")

    if stats["last_sync"]:
        diag.add("INFO", "APPLIER_LAST_SYNC", stats["last_sync"].strip())
    elif stats["last_cleared"]:
        diag.add("INFO", "APPLIER_LAST_CLEARED", stats["last_cleared"].strip())

    return stats


def analyze_server_log(lines: Sequence[str], diag: Diagnosis) -> dict:
    stats = {
        "levelsync_lines": [],
        "enemy_caps": [],
        "cleared_stages": [],
        "stage_requests": [],
        "spawn_lists": [],
        "high_level_spawns": [],
    }
    if not lines:
        diag.add(
            "WARN",
            "SERVER_NO_LOG",
            "No server log found. Set DDON_SERVER_LOG or pass --server-log.",
            "Point at the active Arrowgene log (nativePC/Server/Logs/*.log.txt).",
        )
        return stats

    for ln in lines:
        m = LOG_LINE.match(ln)
        msg = m.group("msg") if m else ln

        ls = LEVELSYNC.search(msg)
        if ls:
            stats["levelsync_lines"].append(msg)

        cap = ENEMY_CAP.search(msg)
        if cap:
            stats["enemy_caps"].append(cap.groupdict())

        if "cleared enemy cache for stage=" in msg:
            stats["cleared_stages"].append(msg)

        st = STAGE_LINE.search(msg)
        if st:
            stats["stage_requests"].append(st.groupdict())

        sp = SPAWN_LIST.search(msg)
        if sp:
            stats["spawn_lists"].append(sp.groupdict())

    # Recent stage 133 activity
    old_well_requests = [s for s in stats["stage_requests"] if s["layout"].startswith(f"{STAGE_LIGHTHOUSE_OLD_WELL}.")]
    if old_well_requests:
        last = old_well_requests[-1]
        diag.add(
            "INFO",
            "SERVER_OLD_WELL",
            f"Recent Lighthouse Old Well enemy request: StageId={last['layout']} SubGroupId={last['sub']}.",
        )

    caps_133 = [c for c in stats["enemy_caps"] if c["stage"] == str(STAGE_LIGHTHOUSE_OLD_WELL)]
    spawns_133 = [s for s in stats["spawn_lists"] if s["layout"].startswith(f"{STAGE_LIGHTHOUSE_OLD_WELL}.")]
    caution_requests = [s for s in stats["stage_requests"] if s["layout"] == f"{STAGE_LIGHTHOUSE_OLD_WELL}.0.4"]

    if caps_133:
        last_cap = caps_133[-1]
        changed = [c for c in caps_133 if int(c["from"]) != int(c["to"])]
        if changed:
            last_changed = changed[-1]
            diag.add(
                "OK",
                "SERVER_ENEMY_CAP",
                f"Server capped enemy on stage {last_changed['stage']} idx={last_changed['idx']}: "
                f"Lv {last_changed['from']}->{last_changed['to']} (recLv={last_changed['rec']}).",
            )
        else:
            diag.add(
                "OK",
                "SERVER_ENEMY_CAP",
                f"Server enemy levels already at or below rec Lv on stage 133 "
                f"(last check idx={last_cap['idx']} Lv {last_cap['from']}).",
            )
    elif spawns_133:
        last_spawn = spawns_133[-1]
        levels = [int(p.rstrip("ma")) for p in last_spawn.get("enemies", "").split(",") if p and p[0].isdigit()]
        manual_flags = [p.endswith("m") for p in last_spawn.get("enemies", "").split(",") if p and p[0].isdigit()]
        if levels and max(levels) <= REC_LV_OLD_WELL + 3:
            diag.add(
                "OK",
                "SERVER_SPAWN_OK",
                f"Spawn-list for stage {last_spawn['layout']} shows capped levels [{last_spawn['enemies']}] "
                f"(recLv={REC_LV_OLD_WELL}).",
            )
            if manual_flags and all(manual_flags):
                diag.add(
                    "WARN",
                    "SERVER_ALL_MANUAL",
                    "All enemies in last spawn use IsManualSet (m). Without StartThinkTblNo the client leaves them idle.",
                    "Rebuild/restart server with latest LevelSyncManager (assigns think table on manual cap); leave dungeon and re-enter.",
                )
        else:
            diag.add(
                "ERROR",
                "SERVER_SPAWN_HIGH",
                f"Spawn-list for stage {last_spawn['layout']} may still be over-level: [{last_spawn.get('enemies', '')}]",
                "Rebuild/restart server; leave and re-enter the dungeon.",
            )
    elif old_well_requests:
        diag.add(
            "ERROR",
            "SERVER_NO_CAP",
            f"Stage {STAGE_LIGHTHOUSE_OLD_WELL} was requested but no enemy cap or spawn-list lines in log tail.",
            "Rebuild/restart game server with LevelSyncEnemyLevels=true. Leave and re-enter the dungeon.",
        )

    if old_well_requests and not caution_requests:
        diag.add(
            "INFO",
            "SERVER_NOT_RANCID",
            f"You are in Lighthouse Old Well (layout {old_well_requests[-1]['layout']}) but Rancid Haunt "
            f"(133.0.4 caution spot) has not been requested in this log tail.",
            "Enter Rancid Haunt (stage 133.0.4 / caution spot) to test Spark Blue + Blue Newt spawns.",
        )
    elif caution_requests:
        diag.add(
            "INFO",
            "SERVER_RANCID_VISIT",
            f"Rancid Haunt (133.0.4) requested {len(caution_requests)} time(s) in log tail.",
        )

    high_caps = [c for c in stats["enemy_caps"] if int(c.get("from", 0)) >= 50]
    if high_caps:
        diag.add(
            "INFO",
            "SERVER_HIGH_CAP",
            f"Found {len(high_caps)} cap(s) from Lv 50+ (likely caution-spot over-level fix).",
        )

    caution_spawns = [s for s in stats["spawn_lists"] if s.get("quest") == str(CAUTION_QUEST_ID)]
    for sp in caution_spawns[-3:]:
        enemies = sp.get("enemies", "")
        if any(part.startswith("6") for part in re.split(r"[,\s]+", enemies) if part and part[0].isdigit()):
            diag.add(
                "ERROR",
                "SERVER_CAUTION_LV60",
                f"Caution-spot spawn still sending Lv 60+ enemies: stage={sp['layout']} [{enemies}]",
                "Update ar12_rancid_haunt.csx levels and restart server; ensure enemy cap runs on cached spawns.",
            )
        else:
            diag.add(
                "OK",
                "SERVER_CAUTION_SPAWN",
                f"Caution-spot spawn stage={sp['layout']} enemies=[{enemies}]",
            )

    if stats["spawn_lists"]:
        last = stats["spawn_lists"][-1]
        diag.add(
            "INFO",
            "SERVER_LAST_SPAWN",
            f"Last spawn-list: stage={last['layout']} quest={last['quest']} enemies=[{last['enemies']}]",
        )

    if not stats["levelsync_lines"]:
        age_days = None
        if diag.meta.get("server_log_mtime"):
            age_days = (datetime.now() - datetime.fromtimestamp(diag.meta["server_log_mtime"])).days
        stale = age_days is not None and age_days > 2
        msg = "No [LEVELSYNC] lines in server log tail"
        if stale:
            msg += f" (log file looks stale, ~{age_days} days old)"
        diag.add(
            "WARN",
            "SERVER_NO_LEVELSYNC",
            msg + " - server may be old build or logging elsewhere.",
            "Set DDON_SERVER_LOG to your live server log, restart server after LevelSync changes.",
        )
    else:
        diag.add("INFO", "SERVER_LEVELSYNC_COUNT", f"{len(stats['levelsync_lines'])} [LEVELSYNC] line(s) in tail.")

    return stats


def analyze_processes(diag: Diagnosis) -> None:
    ddo = process_running("DDO")
    applier = applier_process_running()
    task = query_scheduled_task()

    if ddo:
        diag.add("INFO", "PROC_DDO", "DDO.exe is running.")
    else:
        diag.add("INFO", "PROC_DDO_OFF", "DDO.exe is not running (applier idles until game starts).")

    if applier:
        diag.add("OK", "PROC_APPLIER", "apply_sync process is running.")
    elif task.get("exists"):
        diag.add(
            "WARN",
            "PROC_APPLIER_OFF",
            f"Scheduled task exists (state={task.get('state')}) but no apply_sync process found.",
            "Log off/on or run: schtasks /Run /TN DDONLevelSyncApplier",
        )
    else:
        diag.add(
            "ERROR",
            "TASK_MISSING",
            f"Scheduled task '{TASK_NAME}' not registered.",
            "Run client-mod/applier/setup.cmd once (elevated).",
        )

    if task.get("exists") and task.get("action"):
        diag.add("INFO", "TASK_ACTION", f"Task action: {task['action']}")


def build_summary(diag: Diagnosis) -> str:
    problems = [f for f in diag.findings if f.severity in ("CRIT", "ERROR", "WARN")]
    if not any(f.severity in ("CRIT", "ERROR") for f in diag.findings):
        if not problems:
            return "No critical issues detected. If gameplay still looks wrong, leave the dungeon completely and re-enter."
        return f"{len(problems)} warning(s). See findings above."
    codes = [f.code for f in diag.findings if f.severity in ("CRIT", "ERROR")]
    return "Action needed: " + ", ".join(dict.fromkeys(codes))


def render_report(diag: Diagnosis, paths: dict) -> str:
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    lines = [
        "=" * 60,
        "DDON Level Sync Diagnostics",
        f"Generated: {now}",
        "=" * 60,
        "",
        "Paths:",
    ]
    for key, val in paths.items():
        lines.append(f"  {key}: {val or '(not found)'}")
    lines.extend(["", "Findings:", ""])

    for f in sorted(diag.findings, key=diag.sort_key):
        lines.append(f.format())

    lines.extend(["", "-" * 60, "Summary:", build_summary(diag), ""])
    return "\n".join(lines)


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="Diagnose DDON level-sync from logs and live state.")
    parser.add_argument("--sync-state", type=Path, default=DEFAULT_SYNC)
    parser.add_argument("--applier-log", type=Path, default=DEFAULT_APPLIER_LOG)
    parser.add_argument("--server-log", type=Path, default=None, help="Explicit server log file")
    parser.add_argument("--tail", type=int, default=800, help="Lines to read from each log")
    parser.add_argument("--json", action="store_true", help="Print findings as JSON")
    parser.add_argument("--no-write", action="store_true", help="Do not write diagnose_report.txt")
    args = parser.parse_args(argv)

    server_log = discover_server_log(args.server_log)
    applier_mtime = args.applier_log.stat().st_mtime if args.applier_log.is_file() else None
    server_mtime = server_log.stat().st_mtime if server_log else None

    diag = Diagnosis()
    paths = {
        "sync_state": str(args.sync_state),
        "applier_log": str(args.applier_log),
        "server_log": str(server_log) if server_log else None,
    }
    diag.meta["paths"] = paths
    diag.meta["server_log_mtime"] = server_mtime

    sync_data, sync_err = parse_sync_state(args.sync_state)
    analyze_sync_state(sync_data, sync_err, diag)

    applier_lines = read_text_tail(args.applier_log, args.tail)
    analyze_applier_log(applier_lines, diag)

    server_lines = read_text_tail(server_log, args.tail) if server_log else []
    analyze_server_log(server_lines, diag)

    analyze_processes(diag)

    if applier_mtime and (datetime.now() - datetime.fromtimestamp(applier_mtime)) > timedelta(hours=24):
        diag.add(
            "WARN",
            "APPLIER_LOG_STALE",
            "applier.log has not been updated in over 24 hours.",
            "Restart DDONLevelSyncApplier or launch the game so the applier runs.",
        )

    if args.json:
        payload = {
            "paths": paths,
            "summary": build_summary(diag),
            "findings": [
                {"severity": f.severity, "code": f.code, "message": f.message, "fix": f.fix}
                for f in sorted(diag.findings, key=diag.sort_key)
            ],
        }
        print(json.dumps(payload, indent=2))
    else:
        report = render_report(diag, paths)
        print(report)
        if not args.no_write:
            try:
                DEFAULT_REPORT.write_text(report, encoding="utf-8")
                print(f"Report saved: {DEFAULT_REPORT}")
            except OSError as ex:
                print(f"Could not write report: {ex}", file=sys.stderr)

    return 1 if any(f.severity in ("CRIT", "ERROR") for f in diag.findings) else 0


if __name__ == "__main__":
    sys.exit(main())

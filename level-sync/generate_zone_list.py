#!/usr/bin/env python3
"""Generate ZONE_LIST.md from StageRecommendedLevelTable and Stage.cs."""
import re
from pathlib import Path

PKG = Path(__file__).resolve().parent
table_file = PKG / "src" / "StageRecommendedLevelTable.cs"
out_file = PKG / "ZONE_LIST.md"

# Look for Arrowgene server sources (standalone clone or nested in a full DDON tree).
for root in (PKG.parent, PKG.parent.parent):
    stage_candidate = root / "Server" / "Arrowgene.Ddon.Shared" / "Model" / "Stage.cs"
    sm_candidate = root / "Server" / "Arrowgene.Ddon.GameServer" / "Characters" / "StageManager.cs"
    if stage_candidate.is_file() and sm_candidate.is_file():
        stage_file = stage_candidate
        sm_file = sm_candidate
        break
else:
    raise SystemExit(
        "Could not find Arrowgene server sources.\n"
        "Clone this repo next to a DDON server tree, or set SERVER_ROOT to the repo root."
    )

stage_text = stage_file.read_text(encoding="utf-8")
table_text = table_file.read_text(encoding="utf-8")
sm_text = sm_file.read_text(encoding="utf-8")

stages_by_no: dict[int, list[tuple[int, str]]] = {}
for m in re.finditer(
    r'new StageInfo\((\d+),\s*(\d+),\s*QuestAreaId\.\w+,\s*"([^"]+)"\)',
    stage_text,
):
    sid, sno, name = int(m.group(1)), int(m.group(2)), m.group(3)
    stages_by_no.setdefault(sno, []).append((sid, name))

field_to_id: dict[str, int] = {}
for m in re.finditer(
    r"public static readonly StageInfo (\w+) = new StageInfo\((\d+),",
    stage_text,
):
    field_to_id[m.group(1)] = int(m.group(2))

excluded: set[int] = set()
for block in re.findall(r"new HashSet<StageInfo>\s*\{(.*?)\}", sm_text, re.S):
    for fname in re.findall(r"Stage\.(\w+)", block):
        if fname in field_to_id:
            excluded.add(field_to_id[fname])

levels = [(int(a), int(b)) for a, b in re.findall(r"\{\s*(\d+),\s*(\d+)\s*\}", table_text)]
levels.sort(key=lambda x: (x[1], x[0]))

rows: list[tuple[int, int, int, str, bool]] = []
missing: list[int] = []
for sno, lvl in levels:
    if sno not in stages_by_no:
        missing.append(sno)
        rows.append((lvl, sno, 0, "(unknown StageNo)", False))
        continue
    for sid, name in stages_by_no[sno]:
        syncs = sid not in excluded
        rows.append((lvl, sno, sid, name, syncs))

lines = [
    "# Zone Recommended Levels (Test Reference)",
    "",
    "All zones with a non-zero recommended level from client `stage_list` data.",
    "Sorted by recommended level, then StageNo.",
    "",
    "**StageId** is what the server receives on area change and what you use in "
    "`StageRecommendedLevels` overrides.",
    "**StageNo** is the client stage number used internally for lookup.",
    "**Sync** = whether level sync actually applies (`IsDungeon` — excludes towns and open-world fields).",
    "",
    "| Lv | StageId | StageNo | Sync | Zone Name |",
    "|---:|--------:|--------:|:----:|-----------|",
]
for lvl, sno, sid, name, syncs in rows:
    sync_mark = "Yes" if syncs else "No"
    lines.append(f"| {lvl} | {sid} | {sno} | {sync_mark} | {name} |")

lines += [
    "",
    f"**Total:** {len(rows)} stage entries across {len(levels)} recommended-level StageNos.",
]
if missing:
    lines.append(f"**Unmapped StageNos:** {', '.join(str(x) for x in missing)}")

out_file.write_text("\n".join(lines) + "\n", encoding="utf-8")
print(f"Wrote {len(rows)} rows to {out_file}")
if missing:
    print(f"Missing StageNo mappings: {missing}")

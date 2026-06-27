#!/usr/bin/env python3
"""Audit gathering nodes: spots without drops in GatheringItem.csv or DefaultGatheringDrops."""

import csv
import json
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
STAGE_CS = ROOT / "Arrowgene.Ddon.Shared/Model/Stage.cs"
SPOT_INFO = ROOT / "Arrowgene.Ddon.Shared/Files/Assets/GatheringSpotInfo.json"
GATHER_CSV = ROOT / "Arrowgene.Ddon.Shared/Files/Assets/GatheringItem.csv"
DEFAULT_DROPS = ROOT / "Arrowgene.Ddon.Shared/Files/Assets/DefaultGatheringDrops.json"

BBM_STAGE_IDS = {
    602, 603, 604, 605, 610, 611, 612, 614, 615, 616, 617, 618, 619, 620, 621, 622, 623, 624,
    682, 683, 684, 685, 686, 687, 688, 689, 690, 691, 692, 693, 694, 695, 696, 697, 698, 699,
    700, 715, 716, 717,
}

EPITAPH_STAGE_IDS = {
    549, 550, 551, 552, 553, 554, 555, 556, 557, 558, 559, 560, 561, 563, 565, 566, 567,
}

OM_TO_TYPE = {
    520080: "Alchemy", 520081: "Alchemy", 520111: "Alchemy",
    520110: "Furniture", 520041: "Furniture",
    520070: "Box", 520071: "Box",
    520170: "Corpse/Twinkle", 520010: "Plants", 520011: "Plants", 520012: "Plants",
    520000: "Plants", 520001: "Plants", 520002: "Plants", 520003: "Plants", 520004: "Plants",
    520030: "Lumber", 520031: "Lumber", 520032: "Lumber", 520033: "Lumber",
    520050: "Gemstone", 520051: "Gemstone", 520052: "Gemstone",
    520160: "Gemstone/Ore", 520161: "Gemstone/Ore", 520162: "Gemstone/Ore", 520163: "Ore",
    520020: "Mushroom", 520021: "Mushroom", 520022: "Mushroom", 520023: "Mushroom", 520024: "Mushroom",
    513054: "OneOff", 520171: "OneOff",
    520060: "Sand", 520100: "Shell", 522552: "Twinkle", 523240: "Twinkle", 520090: "Water",
    523907: "SealedChest", 523908: "SealedChest",
    513050: "TreasureChest", 513051: "TreasureChest", 513052: "TreasureChest", 513053: "TreasureChest",
    513055: "TreasureChest", 513056: "TreasureChest", 513060: "TreasureChest", 513061: "TreasureChest",
    523241: "TreasureChest", 523242: "TreasureChest", 513134: "TreasureChest",
    513130: "SealedChest/BBM", 513133: "SealedChest/BBM",
}

DROP_CATEGORIES = {
    "Alchemy": ["liquids", "lumber", "other"],
    "Box": ["consumable", "dye", "thread", "fabric", "ingots", "ore", "gemstones", "scrolls", "leather"],
    "Corpse": ["meat", "claws", "bones", "fang", "hides", "horns", "furs", "feathers"],
    "Furniture": ["consumable", "dye", "thread", "fabric", "crestarmor", "crestweapon"],
    "Gemstone": ["gemstones"],
    "Lumber": ["lumber"],
    "Mushroom": ["mushrooms"],
    "Ore": ["ore"],
    "Plants": ["plants"],
    "Sand": ["sand"],
    "SealedChest": ["equipment", "jewelry", "unappraised", "regional"],
    "Shell": ["shell"],
    "TreasureChest": "ALL",
    "Twinkle": "ALL",
    "Water": ["liquids"],
}


def parse_stages():
    stage_id_to_no = {}
    stage_id_to_name = {}
    stage_no_to_ids = defaultdict(list)
    pattern = re.compile(r'new StageInfo\((\d+),\s*(\d+),\s*QuestAreaId\.\w+,\s*"(\w+)"\)')
    for sid, sno, name in pattern.findall(STAGE_CS.read_text(encoding="utf-8")):
        sid, sno = int(sid), int(sno)
        stage_id_to_no[sid] = sno
        stage_id_to_name[sid] = name
        stage_no_to_ids[sno].append(sid)
    return stage_id_to_no, stage_id_to_name, stage_no_to_ids


def load_csv_keys():
    keys = set()
    with GATHER_CSV.open(encoding="utf-8") as f:
        for row in csv.reader(f):
            if not row or row[0].startswith("#"):
                continue
            keys.add((int(row[0]), int(row[2]), int(row[3])))
    return keys


def load_default_drops_by_stage():
    with DEFAULT_DROPS.open(encoding="utf-8") as f:
        data = json.load(f)
    by_stage = defaultdict(set)
    for drops in data.values():
        for drop in drops:
            if not drop.get("is_spot"):
                by_stage[drop["stage_id"]].add(drop["type"].lower())
    return by_stage


def load_spots(stage_no_to_ids):
    with SPOT_INFO.open(encoding="utf-8") as f:
        data = json.load(f)
    spots = []
    for sno_str, entries in data.items():
        sno = int(sno_str)
        ids = stage_no_to_ids.get(sno, [])
        for e in entries:
            spots.append({
                "stage_no": sno,
                "stage_ids": ids,
                "group_no": e["GroupNo"],
                "pos_id": e["PosId"],
                "gathering_type": e["GatheringType"],
                "unit_id": e["UnitId"],
                "point_type": OM_TO_TYPE.get(e["UnitId"], "Unknown"),
                "x": e["Position"]["x"],
                "y": e["Position"]["y"],
                "z": e["Position"]["z"],
            })
    return spots


def covered_by_csv(spot, csv_keys):
    return any((sid, spot["group_no"], spot["pos_id"]) in csv_keys for sid in spot["stage_ids"])


def covered_by_default(spot, default_by_stage):
    ptype = spot["point_type"].split("/")[0]
    if ptype == "OneOff":
        return True
    if ptype in ("SealedChest", "Unknown"):
        return False
    cats = DROP_CATEGORIES.get(ptype)
    if not cats:
        return False
    for sid in spot["stage_ids"]:
        stage_cats = default_by_stage.get(sid, set())
        if cats == "ALL":
            if stage_cats:
                return True
        elif any(c in stage_cats for c in cats):
            return True
    return False


def main():
    _, stage_id_to_name, stage_no_to_ids = parse_stages()
    csv_keys = load_csv_keys()
    default_by_stage = load_default_drops_by_stage()
    spots = load_spots(stage_no_to_ids)

    excluded = covered_csv = covered_default_only = 0
    incomplete = []
    fixable_by_default = []

    for spot in spots:
        sid = spot["stage_ids"][0] if spot["stage_ids"] else None
        if sid in BBM_STAGE_IDS or sid in EPITAPH_STAGE_IDS:
            excluded += 1
            continue
        if covered_by_csv(spot, csv_keys):
            covered_csv += 1
            continue
        if covered_by_default(spot, default_by_stage):
            covered_default_only += 1
            fixable_by_default.append(spot)
        incomplete.append(spot)

    print("=== Gathering Node Audit ===")
    print("Server defaults: EnableToolGatheringDrops=true, EnableDefaultGatheringDrops=false")
    print(f"Total spots in GatheringSpotInfo.json: {len(spots)}")
    print(f"Excluded (BBM/Epitaph dedicated generators): {excluded}")
    print(f"Covered by GatheringItem.csv: {covered_csv}")
    print(f"Missing from CSV but would work if DefaultGatheringDrops enabled: {covered_default_only}")
    print(f"Incomplete (no drops with current config): {len(incomplete)}")
    print(f"  of which fixable by enabling DefaultGatheringDrops: {len(fixable_by_default)}")
    print(f"  truly missing (no CSV entry and no default drop table): {len(incomplete) - len(fixable_by_default)}")
    print()

    fixable_keys = {(s["stage_no"], s["group_no"], s["pos_id"]) for s in fixable_by_default}
    truly_missing = [s for s in incomplete if (s["stage_no"], s["group_no"], s["pos_id"]) not in fixable_keys]

    print("--- Incomplete by point type (current config) ---")
    for t, c in Counter(s["point_type"] for s in incomplete).most_common():
        print(f"  {t}: {c}")
    print()

    print("--- Incomplete by stage ---")
    stage_groups = defaultdict(list)
    for s in incomplete:
        sid = s["stage_ids"][0] if s["stage_ids"] else 0
        name = stage_id_to_name.get(sid, "Unknown")
        stage_groups[(s["stage_no"], sid, name)].append(s)
    for (sno, sid, name), group in sorted(stage_groups.items(), key=lambda x: -len(x[1])):
        types = Counter(s["point_type"] for s in group)
        type_str = ", ".join(f"{t}={n}" for t, n in types.most_common(3))
        print(f"  [{len(group):4d}] StageNo={sno} StageId={sid} {name} ({type_str})")
    print()

    print("--- Truly missing by stage ---")
    stage_groups = defaultdict(list)
    for s in truly_missing:
        sid = s["stage_ids"][0] if s["stage_ids"] else 0
        name = stage_id_to_name.get(sid, "Unknown")
        stage_groups[(s["stage_no"], sid, name)].append(s)
    for (sno, sid, name), group in sorted(stage_groups.items(), key=lambda x: -len(x[1])):
        types = Counter(s["point_type"] for s in group)
        type_str = ", ".join(f"{t}={n}" for t, n in types.most_common(3))
        print(f"  [{len(group):4d}] StageNo={sno} StageId={sid} {name} ({type_str})")
    print()

    print("--- Full incomplete node list (current config) ---")
    for s in sorted(incomplete, key=lambda x: (x["stage_no"], x["group_no"], x["pos_id"])):
        sid = s["stage_ids"][0] if s["stage_ids"] else 0
        name = stage_id_to_name.get(sid, "?")
        print(
            f"{name} (StageNo={s['stage_no']} StageId={sid}) "
            f"Group={s['group_no']} Pos={s['pos_id']} "
            f"Type={s['point_type']} UnitId=om{s['unit_id']} "
            f"Pos=({s['x']:.0f},{s['y']:.0f},{s['z']:.0f})"
        )


if __name__ == "__main__":
    main()

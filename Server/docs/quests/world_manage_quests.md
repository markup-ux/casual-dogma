# World Manage Quests

World Manage Quests (q7xxxxxxx) are special quests that control the global state of the game world. They determine which NPCs appear, which doors are open/closed, which environmental objects are active, and which areas are accessible.

Flags are extracted from four client data sources:

| Source | File Pattern | Field | Purpose |
|--------|-------------|-------|---------|
| **WMLF** | `stage/st*/…/*_p.gpl.json` (and _e, _s, _t) | `GuardData.LayoutFlagNo` + `GuardData.QuestNo` | Gates whether an NPC group/object/door appears in a stage |
| **WMLF-N** | `stage/st*/…/*_n.gpl.json` | `GuardData.LayoutFlagNo` (QuestNo=0, resolved via EVSI) | NPC group param files; same flag space as WMLF but quest association missing |
| **WMQF** | `quest/q700*/…/*.qst.json` | `QuestGrp.Condition` | Quest set condition; the flag the server sets when this state is active |
| **EVSI** | `event/st*/view_*/…/*.evsi.json` | `FlagArray[].{FlagNo,QuestId}` | Event viewer (cutscene) uses this flag to know which events to show |
| **NLL** | `npc/npc_common/etc/npc/npc.nll.json` | `NpcLedgerList[].InstitutionList[].FunctionOpenList[].FlagNo` | NPC menu option unlock; flag must be set for the NPC service to appear |

The relationship is: setting any of these flags causes the game to show/unlock the associated content. All flag numbers share the same numbering space.

**Note on `_n.gpl.json` files:** These NPC group parameter files contain `GuardData.LayoutFlagNo` values that gate which NPC group (a set of scripted NPCs in a fixed location) is visible in a stage. Unlike `_p.gpl.json`, they have `QuestNo=0` - the quest association is not stored in the file. Quest ownership is resolved by cross-referencing with EVSI data. Flags 1215–1224 (Mysial, Klaus, Joseph, Leo, Iris in audience chamber, and other NPC groups in st0200/st0201/st0400) were "missing" from earlier extraction because of this.

**Note:** Comments marked `(削除)` = "deleted" indicate features removed by that version. Each version carries the full history of all previous flags.

**Note on missing flags:** Some flags observed in packet traces do not appear in any decoded client data file. These may be: (1) NPC group layout flags in `_n.gpl.json` that couldn't be resolved via EVSI (no EVSI entry exists for that flag), (2) server-managed state flags with no client-side reference, or (3) flags in binary arc files not yet decoded.

---

## q70000001 - Season 1, Version 1.0

**Arc Stages:** st0100, st0200, st0201, st0203, st0400, st0403, st0404, st0408, st0411, st0412, st0418

**Additional stages with layout flags:** st0571, st0403

### Quest Flags (WMQF)

Flags below 1098 were confirmed via binary-search brute force (not in decoded qst.json files).

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 926 | Temple of Purification: South Chamber entrance | Allows entry to South Chamber (Lestania/st0100) - brute-forced |
| 1098 | ver1.2 Close_Diamantes Door (deleted) | |
| 1103 | ver1.2 3rd Arc OM (deleted) | |
| 1104 | ver1.2 Control (deleted) / Close_Front Door (deleted) | |
| 1105 | ver1.2 Open_Front Door (deleted) | |
| 1106 | ver1.2_Front Door Lever (deleted) | |
| 1109 | ver1.0 Chapel Door Close | Locks double doors to Chapel at (x:51,y:89) - DreedCastle/st0403 |
| 1110 | ver1.0 Chapel Door Open | Unlocks double doors to Chapel at (x:51,y:89) - DreedCastle/st0403 |
| 1111 | ver1.0 Close_Water Control Room Door | Locks Water Flow Control Room door - Temple of Purification/st0408 |
| 1112 | ver1.0 Open_Water Control Room Door | Opens Water Flow Control Room door - Temple of Purification/st0408 |
| 1113 | ver1.1 Close_Eltedinen Great Gate (deleted) | |
| 1114 | ver1.1 Open_Eltedinen Great Gate (deleted) | |
| 1115 | ver1.1 Gurdnoch Fort Gate (deleted) | |
| 1116 | ver1.1 Gurdnoch Great Gate Open (deleted) | |
| 1117 | ver1.1 Cleansing Temple Annex Close (deleted) | |
| 1118 | ver1.2 Cleansing Temple Annex Open (deleted) | |
| 1119 | ver1.2 Close_Alchemy Research Building Door (deleted) | |
| 1120 | ver1.2 Open_Alchemy Research Building Door (deleted) | |
| 1121 | ver1.2 Close_Military Instructor Door (deleted) | |
| 1122 | ver1.2 Open_Military Instructor Door (deleted) | |
| 1123 | ver1.2 Close_Special Research Area Door (deleted) | |
| 1124 | ver1.2 Open_Special Research Area Door (deleted) | |
| 1201 | ver1.0 NPC for q016 | |
| 1202 | ver1.2 OFF_Mergoda Warp (deleted) | |
| 1203 | ver1.2 ON_To Mergoda (deleted) | |
| 1261 | ver1.0 Pawn Dungeon Entrance | |
| 1262 | ver1.0 Pawn Dungeon Entrance | |
| 1263 | ver1.0 2nd Arc (Random) | Changes 2nd Arc entrance to warp to st0574 (Lestania) |
| 1293 | ver1.0 White Dragon 1 | The White Dragon Gravely Injured (st0201) |
| 1294 | ver1.0 White Dragon 2 | The White Dragon Slightly Healed (st0201) |
| 1295 | ver1.1 White Dragon 3 (deleted) | The White Dragon Mostly Healed (st0201) |
| 1296 | ver1.2 White Dragon 4 (deleted) | The White Dragon Fully Healed (st0201) |
| 1297 | ver1.0 Checkpoint | |
| 1309 | ver1.0 Checkpoint (Open) | |
| 1310 | ver1.0 Gritten Fort Door (Closed) | |
| 1311 | ver1.0 Gritten Fort Door (Open) | |
| 1317 | ver1.0 Waterfall | Waterfall gimmick - Temple of Purification/st0408 |
| 1658 | ver1.0 Cliff Collapse OM | |
| 1671 | ver1.0 Lever Door (Closed) | Closed stone door (middle) - Temple of Purification/st0408 |
| 1672 | ver1.0 Lever Door (Open) | Open stone door (middle) - Temple of Purification/st0408 |
| 1713 | ver1.2 Open_Diamantes Door (deleted) | |
| 2201 | ver1.0 1st Arc (Random) | Changes 1st Arc entrance to warp to st0573 (Lestania) |
| 2202 | ver1.0 1st Arc (Quest) | Changes 1st Arc entrance to warp to st0576 (Lestania) |
| 2204 | ver1.0 2nd Arc (Quest) | Changes 2nd Arc entrance to warp to st0571/st0576 (Lestania) |
| 2208 | ver1.0 Hunter's Secret Passage Entrance | |
| 2243 | Caretaker Klaus | |
| 2244 | Caretaker Joseph | Messenger |
| 2245 | Caretaker Meirova | |
| 2246 | Caretaker Fabio | Messenger |
| 2247 | Caretaker Heinz | Messenger |
| 2248 | Caretaker Vanessa | |
| 2380 | ver1.0 Hoborick Entrance Goblin | Josh, Donnell, Normel |

### Layout Flags (WMLF)

From `_p.gpl.json` files (QuestNo explicitly set):

| Flag | Stage | Notes |
|------|-------|-------|
| 977 | st0100 | Spawns Gerd and the White Knights outside for the 3rd MSQ - brute-forced |
| 1149 | st0200 | Spawns red/white flag decorations around the stage |
| 1150 | st0200/st3602 | |
| 1250 | st0571 | |
| 1251 | st0571 | |
| 1298 | st0571 | |
| 1316 | st0403 | |
| 2209 | st0100 | |
| 2210 | st0100 | |
| 2211 | st0100 | |
| 2212 | st0100 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 1215 | st0201 | Mysial NPC group (audience chamber) |
| 1216 | st0201 | Klaus NPC group (audience chamber) |
| 1217 | st0201 | Joseph NPC group (audience chamber) |
| 1218 | st0201 | Leo NPC group (audience chamber) |
| 1219 | st0201 | Iris NPC group (audience chamber) |
| 1220 | st0200/st3602 | Empty audience chamber variant |
| 1221 | st0400 | NPC group |
| 1222 | st0200/st3602 | NPC group |
| 1223 | st0200/st3602 | NPC group |
| 1224 | st0200/st3602 | NPC group |

### Event Viewer Flags (EVSI)

Flags used by event viewers (cutscene triggers) referencing q70000001:

364 551 569 570 589 590 591 607 616 657 658 659 660 662 663 703 704 707 718 760 764 765 766 768 770 771 772 784 786 787 830 831 843 846 847 848 850 851 891 926 927 932 1109 1110 1111 1112 1150 1201 1215 1216 1217 1218 1219 1220–1224 1250 1251 1261 1262 1263 1293 1294 1297 1298 1309 1310 1311 1316 1317 1658 1671 1672 2201 2202 2204 2208 2212 2243–2248 2380

(Flags 1215–1224 are NPC group layout flags; see WMLF-N table above for stage and NPC details.)

**Flags in packet traces not found in any client data:** 572 574 575 578 614 615 620 727 748 777 779 832 833 834 836 845 849 855 892

---

## q70001001 - Season 1 (Pawn-related)

**Arc Stages:** (minimal - special purpose)

**Additional stages with layout flags:** st0571

### Layout Flags (WMLF)

| Flag | Stage | Notes |
|------|-------|-------|
| 1954 | st0571 | |
| 1955 | st0571 | |

### Event Viewer Flags (EVSI)

1851 1954 1955

**Flags in packet traces not found in any client data:** 608 609 618

---

## q70002001 - Season 1, Version 1.1

**Arc Stages:** st0100, st0201, st0203, st0400, st0402, st0403, st0404, st0408, st0411, st0412, st0418

### Quest Flags (WMQF) - Active in v1.1 (non-deleted)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 925 | Gardnox Fortress entrance | Allows player to enter Gardnox Fortress (Lestania/st0100) - brute-forced |
| 1113 | ver1.1 Close_Eltedinen Great Gate | Large Door Closed in Erte Deenan (st0403) |
| 1114 | ver1.1 Open_Eltedinen Great Gate | Large Door Open in Erte Deenan (st0403) |
| 1115 | ver1.1 Gurdnoch Fort Gate | |
| 1116 | ver1.1 Gurdnoch Great Gate Open | Gardnox Fortress (st0402) |
| 1117 | ver1.1 Cleansing Temple Annex Close | |
| 1295 | ver1.1 White Dragon 3 | The White Dragon Mostly Healed (st0201) |
| 2402 | ver1.1 2nd Arc Entrance (blocked) | |
| 3859 | ver1.1 Gurdnoch Fort Interior Large Door / Floor Lever / Multi-OM Control | Floor Lever + Large Door Inside Gardnock Fort (st0402) |
| 3860 | ver1.1 Gurdnoch Fort Interior Large Door | Large Door Inside Gardnock Fort (st0402) |

*(All ver1.0 flags from q70000001 are carried as deleted in this version)*

### Event Viewer Flags (EVSI)

925 1113 1114 1115 1117 1295 1601 2402 3859 3860

### Layout Flags (WMLF)

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 1601 | st0400 | NPC group |

**Flags in packet traces not found in any client data:** 576 781 856

---

## q70003001 - Season 1, Version 1.2

**Arc Stages:** st0100, st0201, st0203, st0400, st0403, st0404, st0408, st0411, st0412, st0418

**Additional stages with layout flags:** st0100, st0410

### Quest Flags (WMQF) - Active in v1.2 (non-deleted)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 1098 | ver1.2 Close_Diamantes Door | |
| 1103 | ver1.2 3rd Arc OM | |
| 1104 | ver1.2 Control / Close_Front Door | Front Large Door Closed (st0411) |
| 1105 | ver1.2 Open_Front Door | Front Large Door Open (st0411) |
| 1106 | ver1.2 Front Door Lever | Floor-mounted lever (st0411) |
| 1118 | ver1.2 Cleansing Temple Annex Open | |
| 1119 | ver1.2 Close_Alchemy Research Building Door | Closed Alchemy Research Building Door (st0411) |
| 1120 | ver1.2 Open_Alchemy Research Building Door | Open Alchemy Research Building Door (st0411) |
| 1121 | ver1.2 Close_Military Instructor Door | Closed Military Instructor's Door - small door (st0411) |
| 1122 | ver1.2 Open_Military Instructor Door | Open Military Instructor's Door (st0411) |
| 1123 | ver1.2 Close_Special Research Area Door | Closed Special Research Door (st0411) |
| 1124 | ver1.2 Open_Special Research Area Door | Open Special Research Door (st0411) |
| 1202 | ver1.2 OFF_Mergoda Warp | Quest Specified Message OM (st0411) |
| 1203 | ver1.2 ON_To Mergoda | Lost City Dungeon warp (st0411) |
| 1296 | ver1.2 White Dragon 4 | The White Dragon Fully Healed (st0201) |
| 1713 | ver1.2 Open_Diamantes Door | Diamantes Door Open (st0418) |
| 2456 | ver1.2 Palace and Residential District Door Warp OM | Floor movement warp OM (st0418) |
| 2458 | ver1.2 Mergoda Large Door Message OM | Quest Specified Message (st0411) |

### Layout Flags (WMLF)

| Flag | Stage | Notes |
|------|-------|-------|
| 2429 | st0100 | |
| 2430 | st0100 | |
| 2431 | st0410 | |
| 2432 | st0410 | |

### Event Viewer Flags (EVSI)

580 1062 1098 1103 1104 1105 1106 1119 1120 1121 1122 1123 1124 1202 1203 1296 1713 2429 2430 2431 2432 2456 2458

**Flags in packet traces not found in any client data:** 762

---

## q70004001 - Season 1, Version 1.3

**Arc Stages:** st0100, st0201, st0203, st0400, st0403, st0404, st0408, st0411, st0412, st0418

### Quest Flags (WMQF) - Active in v1.3 (non-deleted)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 2509 | ver1.3 Boss Battle Sealing Door OM | |

*(All previous version flags are carried as deleted)*

### Event Viewer Flags (EVSI)

761 2509

---

## q70020001 - Season 2, Version 2.0

**Arc Stages:** st0200, st0201, st0212

**Additional stages with layout flags:** st0110, st0200, st0203, st0412, st0801

### Quest Flags (WMQF)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 1490 | Arisen's Room unlock | Unlocks the Arisen's Room - brute-forced |
| 3442 | ver2.0 Important NPC Set at End | Cecily, Elliot, Lise, Gurdolin (st0201) |
| 4037 | (unlabeled) | Julia1 - Controls if Julia appears in stage (Group 0, movement FSM for Arisen's Room) |

### Layout Flags (WMLF)

| Flag | Stage | Notes |
|------|-------|-------|
| 2736 | st0801 | |
| 3000 | st0110 | |
| 3001 | st0110 | |
| 3002 | st0412 | |
| 3003 | st0412 | |
| 3074 | st0203 | |
| 3805 | st0200 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 2736 | st0801 | NPC group |
| 3805 | st0200/st3602 | NPC group |

### Event Viewer Flags (EVSI)

1411 1416 1418 1479 1523 1524 1627 1672 1673 1675 2736 3000 3001 3002 3003 3074 3442 3805 4037

**Flags in packet traces not found in any client data:** 1400 1409 1410 1525 1529 1531 1532 1538 1540 1541 1756 1757 1760 1761 1762 1763

*(Flag 1490 removed from this list - confirmed as ArisensRoom WMQF via brute force)*

---

## q70021001 - Season 2, Version 2.1

**Arc Stages:** st0201

**Additional stages with layout flags:** st0430, st0532, st0801, st0808

### Quest Flags (WMQF)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 2477 | Play Point Shop | Adds Play Point Shop option to Renton - brute-forced |
| 3861 | 2.1 End Audience Room Cutscene NPC | |

### Layout Flags (WMLF)

| Flag | Stage | Notes |
|------|-------|-------|
| 1545 | st0532 | |
| 3278 | (unknown) | |
| 3445 | st0430 | |
| 3650 | st0808 | |
| 3651 | st0808 | |
| 3753 | st0801 | |

### Event Viewer Flags (EVSI)

1433 (Bloodbane Isle Precipice) 1489 (Bloodbane Isle Summit) 1545 3278 3650 3753

**Flags in packet traces not found in any client data:** 2439 2474

---

## q70022001 - Season 2, Version 2.2

**Arc Stages:** st0112, st0120, st0121, st0201, st0914

**Additional stages with layout flags:** st0110, st0111, st0112, st0120, st0121, st0813

### Quest Flags (WMQF)

Flags below 3953 were confirmed via brute force (not in decoded qst.json files).

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 1698 | Dana Gate | Opens the gates around Dana - brute-forced |
| 1699 | Path to Morrow | Unlocks Path to Morrow (Elan Water Grove) - brute-forced |
| 1703 | Kingal Canyon Border Checkpoint | Unlocks border checkpoint (Dana to Glyndwr) - brute-forced |
| 1998 | Glyndwr Gates | Opens the gates around Glyndwr (Kingal Canyon) - brute-forced |
| 1999 | Morfaul West Gate | Opens the Western Gate in Morfaul (Morrow Forest) - brute-forced |
| 3953 | Beautiful Forest Ferryman (front/back) | Treasa |
| 3977 | Temporary Continental Movement OM | |
| 4053 | 2.2 Clear Cutscene Set / Normal Garuda Placement | Normal Placement of Garuda (st0914) |
| 4063 | Beautiful Forest to Plains Area Sealing Wall | Mole |
| 4803 | (unlabeled) | |

### Layout Flags (WMLF)

| Flag | Stage | Notes |
|------|-------|-------|
| 3949 | st0913 | |
| 3950 | st0911 | |
| 3952 | st0910 | |
| 3954 | st0121 | |
| 4033 | st0121 | |
| 4053 | st0914 | |
| 4555 | st0110, st0111, st0112 | |
| 4556 | st0110, st0111, st0112 | |
| 4557 | st0813 | |
| 4803 | st0120 | |
| 4806 | st0914 | |
| 4968 | st0121 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 3949 | st0913 | NPC group |
| 3950 | st0911 | NPC group |
| 3952 | st0910 | NPC group |
| 4053 | st0914 | NPC group (Garuda area) |
| 4806 | st0914 | NPC group |
| 4968 | st0121 | NPC group |

### Event Viewer Flags (EVSI)

1698 (Dana Gates) 1699 (Path to Morrow) 1703 1704 1709 (Mucel Area Info) 1710 (Arthfael Area Info) 1711 (Razanailt Area Info) 1712 (Ciarán Area Info) 1998 (Glyndwr Gates) 1999 (Western Gate of Morfaul) 2432 2682 3949 3950 3952 3953 3954 3977 4033 4043 4063 4470 4555 4556 4557 4803 4806 4968

**Flags in packet traces not found in any client data:** 2428 2491 2806

---

## q70023001 - Season 2, Version 2.3

**Arc Stages:** st0201, st0914

**Additional stages with layout flags:** st0435

### Quest Flags (WMQF)

Flags below 4066 were confirmed via brute force (not in decoded qst.json files).

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 1997 | Vegasa Corridor South entrance | Unlocks southern Vegasa Corridor entrance (Farna Plains) - brute-forced |
| 2182 | Hollow of Beginnings Gathering Area | Unlocks gathering area in Hollow of Beginnings - brute-forced |
| 2183 | Valtable Hall | Unlocks Valtable Hall - brute-forced |
| 2184 | Valtable Hall Upper Area | Unlocks Valtable Hall Upper Area - brute-forced |
| 2245 | Manun Village Quest Board | Unlocks quest board in Manun Village - brute-forced |
| 2246 | Tower of Ivanos Quest Board | Unlocks quest board in Tower of Ivanos - brute-forced |
| 2400 | Vegasa Corridor East entrance | Unlocks eastern Vegasa Corridor entrance (Kingal Canyon) - brute-forced |
| 2402 | Vegasa Corridor West entrance | Unlocks western Vegasa Corridor entrance (Morrow Forest) - brute-forced |
| 4066 | Post-Clear Garuda | Garuda after clearing (st0914) |
| 4067 | Post-Clear Set | |

### Layout Flags (WMLF)

| Flag | Stage | Notes |
|------|-------|-------|
| 4372 | (unknown) | |
| 4373 | (unknown) | |
| 4500 | st0435 | |
| 4506 | st0882 | |
| 4507 | (unknown) | |
| 4508 | (unknown) | |
| 4511 | (unknown) | |
| 4512 | (unknown) | |
| 4515 | (unknown) | |
| 4516 | (unknown) | |
| 4517 | (unknown) | |
| 4972 | st0882 | |
| 4973 | st0881 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 4506 | st0882 | NPC group |
| 4972 | st0882 | NPC group |
| 4973 | st0881 | NPC group |

### Event Viewer Flags (EVSI)

1997 2179 2180 2181 2182 2183 2184 2245 2246 2400 2402 2564 4066 4067 4372 4373 4500 4506 4507 4508 4511 4512 4515 4516 4517 4972 4973

---

## q70030001 - Season 3, Version 3.0

**Arc Stages:** st0201, st0440, st0443, st0451, st0630, st0635

**Additional stages with layout flags:** st0122, st0130, st0131, st0132, st0136, st0441, st0443, st0448, st0451, st0482, st0630, st0631, st0635

### Quest Flags (WMQF)

Flags below 5671 were confirmed via brute force (not in decoded qst.json files) or from EVSI/QuestFlags.cs annotations.

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 2814 | Gate to Bertha's Bandit Group Hideout | Opens front gate to Bertha's Bandit Group Hideout (Lakeside Grotto) |
| 2816 | Rothgill Front Gate | Opens the front gate to Rothgill (Rathnite Foothills Lakeside) |
| 2914 | Orc Encampment Gate | Toggles the gate for Orc Encampment after being populated with shops |
| 2915 | Fort Thines Gate | Opens the gate in fortress in front of Fort Thines |
| 2930 | West Feryana Gate | Opens gate next to West Feryana Wilderness warp point |
| 3239 | Season 3 Warp | Enables warp crystal in shopping district to warp to Hidden Village Piremoth and Mergoda Golden Palace |
| 3284 | Demon Army War Machine Gate | Opens the Demon Army War Machine Gate (Rathnite Foothills Lakeside) |
| 3403 | Harbour Door - Lookout Castle | Opens the harbour door in Lookout Castle (st0478) |
| 3414 | Cave of Hell's Descent | Opens the entrance to the Cave of Hell's Descent |
| 3483 | Orc Encampment Inner Gate | Opens the gate in the back of the fortress (2nd MSQ in 3.0) |
| 3518 | Epitaph Road: Rathnite Foothills | Opens the door to Epitaph Road: Rathnite Foothills (Fort Thines) |
| 5671 | 3.0 Post-Clear Audience Room Set / 3.0 Post-Clear Set | |

### Layout Flags (WMLF)

| Flag | Stage(s) | Notes |
|------|----------|-------|
| 5078 | st0130 | |
| 5396 | st0630, (unknown) | Spawns area master Endale (Piremoth Traveler's Inn) |
| 5397 | st0443 | Spawns area master Endale (Fort Thines) |
| 5407 | st0130, st0131, st0136 | Opens gate to Fort Thines (st0443) |
| 5552 | (unknown) | Dragon spring present on map (Before the Secret Spring) |
| 5557 | (unknown) | Spawns area master Nayajiku/Nazik (Mephite Traveler's Inn) |
| 5558 | st0451 | |
| 5581 | st0130, st0131, st0136 | Opens gate to Fort Thines (st0440) |
| 5588 | (unknown) | Spawns NPC Bertha (Bertha's Bandit Group Hideout) |
| 5589 | st0131, st0136 | Closes hole in wall (Dacrium Fortress wall breach) |
| 5590 | st0131, st0136 | |
| 5614 | st0130 | Fixes wall used to breach fortress (2nd MSQ in 3.0) - Orc Encampment |
| 5615 | st0130 | Spawns debris and barriers for 2nd MSQ in 3.0 - Orc Encampment |
| 5632 | st0130 | Fixes wall in camp outside Fort Thines |
| 5633 | st0130 | Spawns debris and barriers for 4th MSQ in 3.0 - Fort Thines area |
| 5634 | st0130 | |
| 5635 | st0130 | Spawns buildings for NPCs outside Fort Thines |
| 5636 | st0130 | |
| 5637 | st0130 | |
| 5642 | st0631, (unknown) | |
| 5656 | st0441, st0482 | |
| 5658 | st0131 | |
| 5669 | (unknown) | |
| 5705 | st0130 | |
| 5706 | st0130 | Spawns kingdom flags inside orc settlement |
| 5708 | st0130 | Enables building for the orc encampment |
| 6196 | st0130 | Spawns NPCs for orc encampment shop (OrcCampSettlementNPCs) |
| 6197 | st0130 | Spawns NPCs in area outside Fort Thines (FortThinesNpcs) |
| 6291 | st0131 | Enables Dacrium Fortress Entrance warp (teleports to st0443) |
| 6305 | st0130 | |
| 6306 | st0132 | |
| 6322 | st0132 | |
| 6323 | st0132 | |
| 6409 | st0448 | |
| 6463 | st0448 | |
| 6464 | st0448 | |
| 6465 | st0448 | |
| 6466 | st0448 | |
| 6467 | st0448 | |
| 6468 | st0448 | |
| 6469 | st0448 | |
| 6470 | st0448 | |
| 6471 | st0448 | |
| 6472 | st0448 | |
| 6473 | st0448 | |
| 6474 | st0448 | |
| 6481 | st0448 | |
| 6482 | st0448 | |
| 6483 | st0448 | |
| 6484 | st0448 | |
| 6485 | st0448 | |
| 6486 | st0448 | |
| 6487 | st0448 | |
| 6488 | st0448 | |
| 6489 | st0448 | |
| 6490 | st0448 | |
| 6491 | st0448 | |
| 6539 | st0131 | |
| 6540 | st0131 | |
| 6561 | st0132 | |
| 6562 | st0132 | |
| 6619 | st0448 | |
| 6620 | st0448 | |
| 6621 | st0448 | |
| 6750 | st0122 | |
| 6751 | st0122 | |
| 6763 | st0448 | |
| 6772 | st1001 | |
| 6876 | st0448 | |
| 6877 | st0448 | |
| 6916 | st0448 | |
| 6917 | st0448 | |
| 6963 | st0130 | |
| 6968 | st1001 | |
| 6969 | st0132 | |
| 7206 | (unknown) | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 5396 | st0630 | NPC group |
| 5397 | st0443 | NPC group |
| 5557 | st0635 | NPC group |
| 5558 | st0451 | NPC group |
| 5588 | st1001 | NPC group |
| 5642 | st0631 | NPC group |
| 6196 | st0130 | NPC group |
| 6197 | st0130 | NPC group |
| 6409 | st0448 | NPC group |
| 6463 | st0448 | NPC group |
| 6481 | st0448 | NPC group |
| 6482 | st0448 | NPC group |
| 6483 | st0448 | NPC group |
| 6484 | st0448 | NPC group |
| 6485 | st0448 | NPC group |
| 6486 | st0448 | NPC group |
| 6487 | st0448 | NPC group |
| 6488 | st0448 | NPC group |
| 6489 | st0448 | NPC group |
| 6490 | st0448 | NPC group |
| 6491 | st0448 | NPC group |
| 6619 | st0448 | NPC group |
| 6620 | st0448 | NPC group |
| 6621 | st0448 | NPC group |
| 6763 | st0448 | NPC group |
| 6772 | st1001 | NPC group |
| 6876 | st0448 | NPC group |
| 6877 | st0448 | NPC group |
| 6916 | st0448 | NPC group |
| 6917 | st0448 | NPC group |
| 6963 | st0130 | NPC group |
| 6968 | st1001 | NPC group |
| 6969 | st0132 | NPC group |

### Event Viewer Flags (EVSI)

2814 2816 2913 2914 2915 2928 2930 2932 3218 3224 3225 3227 3229 3231 3239 3240 3241 3403 3414 3483 3508 3604 3774 (Achievements: Royal Family Restoration) 5396 5397 5407 5552 5557 5558 5581 5584 5587 5588 5589 5590 5614 5615 5632–5637 5642 5658 5669 5671 5705 5706 5708 6151 6196 6197 6291 6322 6323 6469–6474 6539 6540 6561 6562 6750 6751 6963 6968 6969 7206

### NPC Service Flags (NLL)

| Flag | NPC | Service |
|------|-----|---------|
| 2922 | Endale | Extreme Missions |
| 2946 | Renton | Vocation Emblem |
| 3240 | Endale | Area Information |
| 3241 | Nazik | Area Information |
| 3301 | The White Dragon | Special Skill Augmentation |
| 3592 | Craig | Custom-made Arms / Equipment Disassembly |
| 3774 | Endale | Achievements: Royal Family Restoration |

**Flags in packet traces not found in any client data:** 3219 3220 3221 3222 3226 3236 3517 3538 3539 3540 3578 3579

*(Flags 3284 and 3518 removed from this list - confirmed via brute force: 3284=DemonArmyWarMachineGate, 3518=EpitaphRoadRathniteFoothills)*

**Note:** An earlier research document referenced flag **3283** at st0130 as "Enables NPCs and Shops in Rathnite Foothills Orc Encampment". Flag 3283 does not appear in any decoded client file. The correct flag is **3284** (DemonArmyWarMachineGate), confirmed via brute force testing - 3283 was likely a typo or off-by-one in earlier notes.

---

## q70031001 - Season 3, Version 3.1

**Arc Stages:** st0201, st0443, st0451

**Additional stages with layout flags:** st0132, st0137, st0451, st0472, st0473

### Quest Flags (WMQF)

Flags below 5672 were confirmed via brute force (not in decoded qst.json files).

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 2956 | Lookout Castle warp activation | Activates the door to Lookout Castle (Royal Family's Secret Path) |
| 3111 | Dark Path to the Secret Spring | Opens the dungeon entrance (Feryana Wilderness) |
| 3286 | Old Tekia Grotto | Opens the entrance to Old Tekia Grotto (Feryana Wilderness) |
| 3520 | Epitaph Road: Feryana Wilderness | Opens the gate to Epitaph Road: Feryana Wilderness (Lookout Castle) |
| 5672 | 3.1 Post-Clear Quintus / 3.1 Post-Clear Nedo et al. / 3.1 Post-Clear Set | |

### Layout Flags (WMLF)

| Flag | Stage(s) | Notes |
|------|----------|-------|
| 5553 | (unknown) | Turns the dragon spring on (Before the Secret Spring) |
| 5662 | st0132, st0137 | |
| 6176 | st0132, st0137 | |
| 6194 | (unknown) | Enables warp to Lookout Castle (st0450) - Royal Family's Secret Path |
| 6195 | (unknown) | Enables warp to Lookout Castle (st0451) - Royal Family's Secret Path |
| 6377 | st0451 | Spawns Nedo's table, chair, and seats (Lookout Castle) |
| 6404 | (unknown) | |
| 6425 | st0451 | |
| 7217 | st0137 | |
| 7218 | st0137 | |
| 7219 | st0137 | |
| 7220 | st0137 | |
| 7221 | st0137 | |
| 7222 | st0137 | |
| 7223 | st0137 | |
| 7224 | st0137 | |
| 7226 | st0137 | |
| 7227 | st0137 | |
| 7228 | st0137 | |
| 7237 | st0137 | |
| 7238 | st0137 | |
| 7239 | st0137 | |
| 7298 | st0137 | |
| 7302 | st0137 | |
| 7316 | st0132 | |
| 7317 | st0132 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 6425 | st0451 | NPC group |
| 7239 | st0137 | NPC group |

### Event Viewer Flags (EVSI)

2944 2952 2953 2956 3111 3272 3421 3603 4242 4243 4263 (Nazik: Achievements: Royal Family Restoration) 5553 5672 6138 6194 6195 6377 7316

### NPC Service Flags (NLL)

| Flag | NPC | Service |
|------|-----|---------|
| 2923 | Nazik | Extreme Missions |
| 3241 | Nazik | Area Information |
| 4263 | Nazik | Achievements: Royal Family Restoration |

**Flags in packet traces not found in any client data:** 3285 3519 3580 3581 4412

*(Flags 3286 and 3520 removed from this list - confirmed via brute force: 3286=OldTekiaGrotto, 3520=EpitaphRoadFeryanaWilderness)*

---

## q70032001 - Season 3, Version 3.2

**Arc Stages:** st0130, st0201, st0451, st1026

**Additional stages with layout flags:** st0133, st0203, st0451, st0460, st0461, st0462, st0464, st0472, st0473, st0636

### Quest Flags (WMQF)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 4978 | Bitterblack Maze Entrance | Controls if the portal to Bitterblack Maze is usable (Cave Harbor) - brute-forced |
| 7169 | Post-Clear | Gurdolin, Lise, Elliot |
| 7387 | White Dragon 4 | Gravely Injured |
| 7388 | White Dragon 5 | Slightly Healed |
| 7389 | White Dragon 6 | Mostly Healed |
| 7390 | White Dragon 7 | Fully Healed |
| 7498 | st133 and st130 Checkpoint Movement OM | |
| 7512 | Adult Hitbox OM | |
| 7514 | Floor Movement OM | |

### Layout Flags (WMLF)

| Flag | Stage(s) | Notes |
|------|----------|-------|
| 7240 | st0133 | |
| 7241 | st0133 | |
| 7336 | st0133 | |
| 7341 | (unknown) | |
| 7342 | (unknown) | |
| 7386 | st0460 | |
| 7438 | st0133, st0451 | |
| 7439 | st0133, st0460 | |
| 7440 | st0133 | |
| 7447 | st0203 | |
| 7496 | st0636 | |
| 7497 | st0461, st0472 | |
| 7544 | st0462 | |
| 7545 | st0462 | |
| 7549 | st0462 | |
| 7550 | st0462 | |
| 7551 | st0462 | |
| 7552 | st0462 | |
| 7553 | st0462 | |
| 7554 | st0462 | |
| 7555 | st0462 | |
| 7556 | st0462 | |
| 7557 | st0462 | |
| 7558 | st0462 | |
| 7562 | st0464 | |
| 7654 | st0461, st0472 | |
| 7722 | st0472 | |
| 7746 | (unknown) | |
| 7747 | st0461 | |
| 7830 | st0133 | |
| 7893 | st0473 | |
| 7894 | st0473 | |
| 7918 | st0472 | |
| 7919 | st0472 | |
| 7920 | st0472 | |
| 7921 | st0472 | |
| 7922 | st0472 | |
| 7923 | st0472 | |
| 7924 | st0472 | |
| 7927 | (unknown) | |
| 7928 | (unknown) | |
| 7929 | (unknown) | |
| 7930 | (unknown) | |
| 7946 | st0133 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 7336 | st0133 | NPC group |
| 7496 | st0636 | NPC group |
| 7497 | st0461/st0472 | NPC group |
| 7650 | st0474 | NPC group |
| 7722 | st0472 | NPC group |
| 7747 | st0461 | NPC group |
| 7918 | st0472 | NPC group |
| 7919 | st0472 | NPC group |
| 7920 | st0472 | NPC group |
| 7921 | st0472 | NPC group |
| 7922 | st0472 | NPC group |
| 7923 | st0472 | NPC group |
| 7924 | st0472 | NPC group |

### Event Viewer Flags (EVSI)

4149 4179 4311 4312 4331 4408 4471 4472 4473 4721 4744 4834 4835 4836 4845 4916 7169 7240 7241 7336 7341 7342 7386 7387 7388 7389 7390 7438 7439 7440 7496 7497 7498 7512 7514 7544–7558 7562 7746 7927–7930

### NPC Service Flags (NLL)

| Flag | NPC | Service |
|------|-----|---------|
| 4407 | Doris | Achievements: Royal Family Restoration |
| 4408 | Doris | Area Information |
| 4410 | Doris | Extreme Missions |

**Flags in packet traces not found in any client data:** 4402 4449 4450 4451 4452 4575 4580 4734 4872

*(Flag 4978 removed from this list - confirmed as BitterblackMazeEntrance WMQF via brute force)*

---

## q70033001 - Season 3, Version 3.3

**Arc Stages:** st0139, st0201, st0443, st0451, st1100

**Additional stages with layout flags:** st0132, st0134, st0139, st0464, st0465, st0481

### Quest Flags (WMQF)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 7916 | Post-Clear | |
| 7917 | Post-Clear | |
| 8166 | Quintus | |
| 8201 | (unlabeled) | |
| 8202 | (unlabeled) | |

### Layout Flags (WMLF)

| Flag | Stage(s) | Notes |
|------|----------|-------|
| 7826 | st0464 | |
| 7827 | st0464 | |
| 7888 | (unknown) | |
| 7954 | st0139 | |
| 7955 | (unknown) | |
| 7972 | st0134 | |
| 7973 | st0134 | |
| 8033 | st0134/st0637 | |
| 8034 | st0139 | |
| 8035 | st0139 | |
| 8036 | st0134 | |
| 8037 | st0637 | |
| 8038 | st0139 | |
| 8043 | st0465 | |
| 8044 | st0465 | |
| 8045 | st0465 | |
| 8046 | st0465 | |
| 8047 | st0465 | |
| 8048 | st0465 | |
| 8049 | st0465 | |
| 8050 | st0465 | |
| 8051 | st0465 | |
| 8052 | st0465 | |
| 8066 | st0481 | |
| 8113 | st0134 | |
| 8130 | st0132 | |
| 8131 | st0637 | |
| 8133 | (unknown) | |
| 8134 | (unknown) | |
| 8159 | (unknown) | |
| 8211 | (unknown) | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 8033 | st0134/st0637 | NPC group |
| 8034 | st0139 | NPC group |
| 8036 | st0134 | NPC group |
| 8037 | st0637 | NPC group |
| 8038 | st0139 | NPC group |
| 8131 | st0637 | NPC group |

### Event Viewer Flags (EVSI)

4814 4914 4920 4921 4922 4923 4924 4940 4974 4975 4998 4999 5000 5095 5097 5100 5190 5205 5206 5207 5208 7826 7827 7876 7888 7889 7916 7917 7954 7955 7965–7968 7972 7973 8016 8033–8038 8043–8052 8066 8113 8130 8131 8133 8134 8166 8171 8201 8202 8211

### NPC Service Flags (NLL)

| Flag | NPC | Service |
|------|-----|---------|
| 4940 | Bacias | Area Information |

**Flags in packet traces not found in any client data:** 4962 4965 5332

---

## q70034001 - Season 3, Version 3.4

**Arc Stages:** st0201, st0451

**Additional stages with layout flags:** st0122, st0200, st0461

### Quest Flags (WMQF)

| Flag | Comment | NPCs/Notes |
|------|---------|------------|
| 8630 | Post-Clear | |
| 8631 | Post-Clear | |

### Layout Flags (WMLF)

| Flag | Stage(s) | Notes |
|------|----------|-------|
| 8486 | st0200/st3602 | |
| 8843 | st0122 | |
| 8844 | st0461 | |

From `_n.gpl.json` files (NPC group params, QuestNo=0, resolved via EVSI):

| Flag | Stage | Notes |
|------|-------|-------|
| 8486 | st0200/st3602 | NPC group |

### Event Viewer Flags (EVSI)

5106 5184 (Shadolean Great Temple) 5185 5186 (Dreed Castle Entrance) 5187 (Mergoda Security District) 5215–5218 5282–5286 5293 5296 5306–5310 5357–5360 5378 5379 8486

### NPC Service Flags (NLL)

| Flag | NPC | Service |
|------|-----|---------|
| 5274 | Craig / Suleiman | Synthesis of Dragon Abilities |
| 5380 | Craig / Suleiman | Dragon Armor Appraisal |
| 5403 | Isaac | Appraisal Exchange |

**Flags in packet traces not found in any client data:** 5273 5311 5313 5314 5315 5316 5399 5400 5454

---

## Global NPC Service Flags (NLL)

These flags unlock NPC services regardless of which world manage quest version is active. Sourced from `npc/npc_common/etc/npc/npc.nll.json`.

| Flag | NPC | Service | Quest Association |
|------|-----|---------|------------------|
| 657 | Travers / Logan / Hancock / Cornelia | Grand Mission | q70000001 |
| 658 | The White Dragon / Gilstan / Alvar | Dragon Force Augmentation / Orb Exchange | q70000001 |
| 659 | Emilia / Chloe / Nathaniel / Alma / Toby / Julienne / Ciarán / Majori / Metrophanes / Beatrix / Augusto | Party Creation | q70000001 |
| 660 | O'Neill / Roy / Sunny / Nadia / Scherzo / Christine / Dian / Rondejeel / Patricia / Hayden / Arthfael / Ciarán | Area Information | q70000001 |
| 661 | Kibiza | Clan Management | q70000001 |
| 662 | Sonia | Craft | q70000001 |
| 663 | Archibald / Poe | Vocation / Arts Support | q70000001 |
| 761 | Seneka | Extreme Missions | q70004001 |
| 765 | Second Pawn | Second Pawn | q70000001 |
| 772 | Seneka | Lestania News | q70000001 |
| 787 | The White Dragon | Myrmidon's Pledge | q70000001 |
| 1479 | Bertrand | Area Information | q70020001 |
| 1491 | Julia | Achievements | q70020001 |
| 1524 | Isaac | Extreme Missions | q70020001 |
| 1709 | Mucel | Area Information | q70022001 |
| 1710 | Arthfael | Area Information | q70022001 |
| 1711 | Razanailt | Area Information | q70022001 |
| 1712 | Ciarán | Area Information | q70022001 |
| 2477 | Renton | Play Point Shop | q70021001 |
| 2922 | Endale | Extreme Missions | q70030001 |
| 2923 | Nazik | Extreme Missions | q70031001 |
| 2946 | Renton | Vocation Emblem | q70030001 |
| 3240 | Endale | Area Information | q70030001 |
| 3241 | Nazik | Area Information | q70030001 |
| 3301 | The White Dragon | Special Skill Augmentation | q70030001 |
| 3592 | Craig | Custom-made Arms / Equipment Disassembly | q70030001 |
| 3774 | Endale | Achievements: Royal Family Restoration | q70030001 |
| 4263 | Nazik | Achievements: Royal Family Restoration | q70031001 |
| 4407 | Doris | Achievements: Royal Family Restoration | q70032001 |
| 4408 | Doris | Area Information | q70032001 |
| 4410 | Doris | Extreme Missions | q70032001 |
| 4940 | Bacias | Area Information | q70033001 |
| 5274 | Craig / Suleiman | Synthesis of Dragon Abilities | q70034001 |
| 5380 | Craig / Suleiman | Dragon Armor Appraisal | q70034001 |
| 5403 | Isaac | Appraisal Exchange | q70034001 |

---

## Summary: Flag Ranges by Season/Version

| Quest | Season | Version | WMQF Range | WMLF Range |
|-------|--------|---------|------------|------------|
| q70000001 | 1 | 1.0 | 1098–2380 | 1149–2212 |
| q70001001 | 1 | - | - | 1954–1955 |
| q70002001 | 1 | 1.1 | 2402–3860 | - |
| q70003001 | 1 | 1.2 | 2456–2458 | 2429–2432 |
| q70004001 | 1 | 1.3 | 2509 | - |
| q70020001 | 2 | 2.0 | 3442–4037 | 2736–3805 |
| q70021001 | 2 | 2.1 | 3861 | 1545–3753 |
| q70022001 | 2 | 2.2 | 3953–4803 | 3949–4968 |
| q70023001 | 2 | 2.3 | 4066–4067 | 4372–4973 |
| q70030001 | 3 | 3.0 | 5671 | 5078–7206 |
| q70031001 | 3 | 3.1 | 5672 | 5553–7317 |
| q70032001 | 3 | 3.2 | 7169–7514 | 7240–7946 |
| q70033001 | 3 | 3.3 | 7916–8202 | 7826–8211 |
| q70034001 | 3 | 3.4 | 8630–8631 | 8486–8844 |

---

## Notes on Incomplete Data

- Entries marked `(unknown)` for stage have stage path extraction failures - the flag exists in decoded data but its stage association could not be determined
- Quest flag 3284 (q70030001) is confirmed as **DemonArmyWarMachineGate** via brute-force testing - it does not appear in any decoded client file and is set server-side only
- Flags listed under "not found in any client data" per quest were captured from live packet traces but have no matching entry in gpl, qst, evsi, or nll files
- The `(削除)` pattern in quest flag comments means the feature was removed by that version but the flag entry remains in the data for historical tracking
- Layout flags can appear in multiple stages (the same flag guards objects across several zones)
- Some stages not listed in the arc.json for a quest still contain layout flags for that quest (e.g., st0448, st0130, st0462 for season 3 quests) - these are world areas whose objects are controlled by that quest

## Extraction Scripts and Data Files

The flags were extracted using `docs/quests/extract_wm_flags.sh` which produces:

| CSV | Source | Contents |
|-----|--------|----------|
| `world_manage_layout_flags_full.csv` | `.gpl.json` (stage/) | stage, flag_no, quest_no, file_type |
| `world_manage_quest_flags_full.csv` | `.qst.json` (quest/q700*/) | quest_no, stage_no, flag_no, comment, npc_names |
| `world_manage_event_flags_full.csv` | `.evsi.json` (event/) | stage, event_id, flag_no, quest_no |
| `world_manage_npc_flags_full.csv` | `npc.nll.json` | npc_id, npc_name, function_id_type, function_name, flag_no |

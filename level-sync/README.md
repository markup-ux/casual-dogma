# Zone Level Sync (Arrowgene DDON)

Per-zone level sync for private DDON servers. When a player enters a dungeon whose recommended level is lower than their current job level, their **base combat stats** (physical/magick attack and defense) are temporarily reduced to match that recommended level. Stats restore automatically when they leave the zone.

**Repository:** https://github.com/markup-ux/ddon-level-sync

This is **down-only** sync: under-leveled players are never boosted, and open-world fields / towns are never synced.

## Status

- **Build verified:** compiles cleanly against the current `Arrowgene.Ddon.GameServer` tree in this repo.
- **Runtime:** triggered on every stage area change via `StageAreaChangeHandler`.
- **Pawns:** yes — your active party pawns are synced alongside you on zone entry/exit.
- **Automated tests:** none included; validate in-game (see Test plan below).

## What gets synced (and what does not)

| Affected | Not affected |
|----------|--------------|
| Base `Atk`, `Def`, `MAtk`, `MDef` on the active job | Job level number (HUD shows your real level) |
| Player and active pawns | EXP gauge — stays on your real level's "Next X XP" while synced |
| Dungeon / instanced content only | Equipped gear, equip requirements, JP |
| | Secondary stats (`Strength`, `Constitution`, `Guts`, blow power, etc.) |
| | Open-world field maps and town hubs |

## What determines the capped stats?

1. **Zone recommended level** — resolved in this order:
   - `GameServerSettings.StageRecommendedLevels` override for a specific `StageId` (admin wins; set to `0` to disable sync for that stage).
   - Otherwise, `StageRecommendedLevelTable` (baked from the client's `base.arc → scr/stage_list`), but **only** if `StageManager.IsDungeon(stageId)` is true (excludes towns and overworld fields).

2. **Sync condition** — sync applies only when `realJobLevel > recommendedLevel`.

3. **Capped values** — `ExpManager.CalculateBaseStats(jobId, recommendedLevel)`:
   ```
   stat = BASE_STATS_TABLE[job].base + (recommendedLevel - 1) * BASE_STATS_TABLE[job].rate
   ```
   Applied to `Atk`, `Def`, `MAtk`, and `MDef` only. Growth tables live in `ExpManager.BASE_STATS_TABLE` (Famitsu wiki sourced).

4. **Persistence safety** — while synced, `ExpManager.AddExp` / `AddJp` temporarily restore real stats before DB writes, then re-apply sync afterward so your database never stores reduced values.

## Installation

Target: an existing [Arrowgene DDON](https://github.com/sebastian-heinz/Ddo-server) server checkout.

### 1. Copy new files

| From this package | To your server tree |
|-------------------|---------------------|
| `src/LevelSyncManager.cs` | `Server/Arrowgene.Ddon.GameServer/Characters/LevelSyncManager.cs` |
| `src/StageRecommendedLevelTable.cs` | `Server/Arrowgene.Ddon.Shared/Model/StageRecommendedLevelTable.cs` |

### 2. Apply integration patches

See `integration/` for copy-paste snippets. Summary of touch points:

| File | Change |
|------|--------|
| `DdonGameServer.cs` | Construct and expose `LevelSyncManager` |
| `StageAreaChangeHandler.cs` | Call `HandleStageChange` on zone entry |
| `ExpManager.cs` | Wrap `AddExp` / `AddJp` with persistence-safe sync tokens |
| `GameServerSettings.cs` | Add optional `StageRecommendedLevels` setting |
| `ClientCommand.cs` | Add `recommend=` CLI to regenerate the stage table |

Detailed snippets: `integration/*.md`

### 3. Build

```powershell
cd Server
dotnet build
```

### 4. (Optional) Regenerate stage table

If your client ROM differs from the one used to bake `StageRecommendedLevelTable.cs`:

```powershell
cd Server
dotnet run --project Arrowgene.Ddon.Cli -- client "<path-to-client-rom>" recommend="Arrowgene.Ddon.Shared/Model/StageRecommendedLevelTable.cs"
```

## Configuration

Normally no config is needed. Optional overrides in server settings JSON:

```json
"StageRecommendedLevels": {
  "90": 13,
  "301": 0
}
```

- Key = `StageId` (not `StageNo`)
- Value = forced recommended level, or `0` to disable sync for that stage

## Test plan

1. Take a character above a dungeon's recommended level (e.g. Lv 60 into a Lv 13 dungeon).
2. Enter the dungeon — combat stats on the status screen should drop to the recommended level's base values; level number stays real.
3. Check your pawns — same stat reduction should apply.
4. Kill enemies and gain EXP — level/EXP should progress normally; DB should store real stats after logout.
5. Leave the dungeon — stats restore to full values.
6. Visit an open-world field — no sync should occur regardless of field data.

Quick picks for testing (high-level character, sync should trigger):

| Lv | StageId | Zone |
|---:|--------:|------|
| 3 | 135 | TrainingChapel |
| 13 | 85 | HidellCatacombs |
| 35 | 28 | ForestRuinsCellar |
| 60 | 92 | DeadLordsLabyrinth |

Full list of all 203 zones with recommended levels, StageIds, and whether sync applies: **[ZONE_LIST.md](ZONE_LIST.md)** (sorted by level).

Regenerate after updating client data:

```powershell
python level-sync/generate_zone_list.py
```

## File list

```
level-sync/
├── README.md
├── ZONE_LIST.md                     # All zones with recommended levels (test reference)
├── generate_zone_list.py            # Regenerates ZONE_LIST.md from server sources
├── src/
│   ├── LevelSyncManager.cs          # Core sync logic
│   └── StageRecommendedLevelTable.cs # Baked StageNo → RecommendLevel map
└── integration/
    ├── DdonGameServer.md
    ├── StageAreaChangeHandler.md
    ├── ExpManager.md
    ├── GameServerSettings.md
    └── ClientCommand.md
```

## License

Same as the upstream Arrowgene DDON server (GPL-3.0).

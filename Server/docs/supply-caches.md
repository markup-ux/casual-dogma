# Persistent Supply Caches (Player Item Drops)

Discarding items from your bag normally removes them with no world object on the DDON server. **Supply caches** replace that behavior with public, persistent drop bags that anyone can loot.

## Player experience

- Discard an item from your bag or key items (if allowed) → a bag appears at your feet.
- Walk up and interact → loot UI opens; items return with full stats (enhancement, augments, etc.).
- Other players in the same area can loot the same cache.
- Caches persist at the exact spot where they were first placed for **7 days** (configurable), survive server restarts, and merge when drops land within the configured radius.
- Other players entering the area see and can loot persisted bags.

Protected items (lock icon) cannot be dropped into supply caches.

## Server operator setup

1. **Update the server** to a build that includes supply caches (database migration **66** or later).
2. **Run migrations** before starting the game server:
   ```bash
   dotnet Arrowgene.Ddon.Cli.dll dbmigration
   ```
   Or use `MigrateDatabase.cmd` from your publish folder.
3. **Start the server** as usual. On first boot, existing caches are loaded before scheduled maintenance runs.
4. **Configure** via `GameServerSettings` (see below). All supply-cache settings default to enabled.

No client patch is required beyond a normal DDON client pointed at your server.

## Configuration

Settings live in `GameServerSettings` (JSON config or `scripts/settings/GameServerSettings.csx`).

| Setting | Default | Description |
|---------|---------|-------------|
| `SupplyCachesEnabled` | `true` | Master toggle. When off, discards behave like vanilla (item removed, no bag). |
| `SupplyCacheMergeRadius` | `8.0` | Meters. Drops within this distance merge into the nearest cache. |
| `SupplyCacheSpawnRadiusMin` | `0.0` | Minimum offset from the player when placing a new cache (meters). |
| `SupplyCacheSpawnRadiusMax` | `0.0` | Maximum offset when placing a new cache (meters). |
| `SupplyCacheMaxItemsPerCache` | `200` | Item stacks per cache before spawning another nearby. |
| `SupplyCacheMaxCachesPerMap` | `500` | Cap per map/area. |
| `SupplyCacheMaxCachesServerWide` | `10000` | Global cap. |
| `SupplyCacheSpawnRetries` | `10` | Placement attempts before falling back to the player position. |
| `SupplyCachePersistAcrossRestart` | `true` | Load caches from SQLite on startup. |
| `SupplyCacheCleanupWeekly` | `true` | Run scheduled removal of caches past their lifetime. |
| `SupplyCacheLifetimeDays` | `7` | Days after creation before a cache is deleted. Set to `0` to disable expiry. |
| `SupplyCacheAllowQuestItems` | `false` | Allow key-item discards into caches. |

Expiry cleanup runs **daily at 05:00** server time (scheduler task `SupplyCacheReset`, type **27**). Expired caches are also purged on server startup.

## Technical notes

- **Database**: tables `ddon_supply_cache` and `ddon_supply_cache_item` (migration `00000066_SupplyCacheMigration`).
- **Drop flow**: `ItemConsumeStorageItemHandler` → `SupplyCacheManager` → `PopDropItemNtc` + `InstanceGetDropItemSetList` / `InstanceGetDropItemList` / `InstanceGetDropItem`.
- **Item data**: full item instances are JSON-serialized in `ddon_supply_cache_item` so equipment stats survive storage and loot.
- **Set IDs**: the client uses small sequential drop IDs; the server tracks wire IDs per client and maps them to persistent cache rows.

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| Item removed, no bag | `SupplyCachesEnabled` is false, or item is protected. |
| `DROP_MISSING` when opening bag | Server build without set-ID mapping fix, or migration not applied. |
| Error after picking up item | Usually a stale list refresh; fixed builds return an empty list instead of erroring. |
| Caches gone after restart | `SupplyCachePersistAcrossRestart` false, cache exceeded `SupplyCacheLifetimeDays`, or dungeon instance was reset. |

Check game-server logs for lines starting with `SupplyCacheManager:` or **`[SUPPLY_CACHE_DIAG]`**.

### In-game diagnosis

After discarding an item, run **`/scdiag`** in chat. The report shows a verdict:

| Verdict | Meaning |
|---------|---------|
| `WORKING` | PopDrop sent, client polled `GetDropItemList`, and cache contents returned without auto-loot. |
| `CLIENT_PENDING` | Server sent PopDrop but client has not polled the drop list yet — wait a moment and run `/scdiag` again. |
| `BROKEN` | A critical step failed (wrong PopDrop setId, empty drop list, auto-loot, etc.). |
| `INCONCLUSIVE` | Discard not seen yet, or client has not polled the drop list. |
| `DISABLED` | `SupplyCachesEnabled` is false. |

`/scdiag clear` resets session history. Server log lines use the prefix `[SUPPLY_CACHE_DIAG]` (toggle with `SupplyCacheDiagnosticsEnabled`, default **true**).

## Contributing

Supply-cache code is self-contained. A focused upstream PR includes roughly:

- `SupplyCacheManager`, `SupplyCacheDropTracker`, models, DB layer, migration 66
- Handler changes: consume, drop list/set/item, stage area change
- `GameServerSettings`, scheduler task `supply_cache_reset.csx`, `TaskType.SupplyCacheReset`
- Packet types for `InstanceGetDropItemSetList`

See the [Arrowgene.DragonsDogmaOnline](https://github.com/sebastian-heinz/Arrowgene.DragonsDogmaOnline) repository for contribution guidelines and issue discussion.

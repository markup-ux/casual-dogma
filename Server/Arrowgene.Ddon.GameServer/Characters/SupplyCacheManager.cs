using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Ddon.Server.Settings;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Characters;

public class SupplyCacheManager
{
    private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupplyCacheManager));

    private readonly DdonGameServer _server;
    private readonly object _lock = new();
    private readonly Dictionary<long, SupplyCache> _cachesById = new();
    private readonly Dictionary<uint, List<SupplyCache>> _cachesByMap = new();
    private readonly Dictionary<uint, SupplyCache> _cachesBySetId = new();

    public SupplyCacheManager(DdonGameServer server)
    {
        _server = server;
    }

    private const double DefaultNearbySyncRadius = 200.0;
    private const int MaxCachesPerProximityBurst = 4;

    public void Load()
    {
        if (!_server.GameSettings.GameServerSettings.SupplyCachesEnabled
            || !_server.GameSettings.GameServerSettings.SupplyCachePersistAcrossRestart)
        {
            return;
        }

        List<SupplyCache> caches = _server.Database.SelectAllSupplyCaches();
        _server.Database.ExecuteInTransaction(connection =>
        {
            foreach (SupplyCache cache in caches)
            {
                NormalizeOpenWorldLayout(cache, connection);
            }

            ClearExpiredCaches(connection);
        });

        lock (_lock)
        {
            _cachesById.Clear();
            _cachesByMap.Clear();
            _cachesBySetId.Clear();
            foreach (SupplyCache cache in caches)
            {
                if (!IsExpired(cache))
                {
                    RegisterCache(cache);
                }
            }
        }

        int cacheCount = GetCacheCount();
        Logger.Info($"Loaded {cacheCount} supply caches from database.");
        TrimExcessPersistedCaches(maxCaches: 25);
        SupplyCacheEventLog.RecordServerStart(GetCacheCount());
        SupplyCacheSelfTest.RunCoreChecksAndLog();
    }

    public void ClearAll(DbConnection connection)
    {
        _server.Database.DeleteAllSupplyCaches(connection);
        lock (_lock)
        {
            _cachesById.Clear();
            _cachesByMap.Clear();
            _cachesBySetId.Clear();
        }
        Logger.Info("Supply cache full cleanup completed.");
    }

    public void ClearExpiredCaches(DbConnection connection)
    {
        if (_server.GameSettings.GameServerSettings.SupplyCacheLifetimeDays <= 0)
        {
            return;
        }

        List<SupplyCache> expired;
        lock (_lock)
        {
            expired = _cachesById.Values.Where(IsExpired).ToList();
        }

        foreach (SupplyCache cache in expired)
        {
            RemoveCache(cache, connection);
        }

        if (expired.Count > 0)
        {
            Logger.Info($"Supply cache expiry cleanup: removed={expired.Count}");
        }
    }

    /// <summary>
    /// Keeps the newest persisted rows so dev/test sessions do not flood proximity sync with dozens of wires.
    /// </summary>
    private void TrimExcessPersistedCaches(int maxCaches)
    {
        List<SupplyCache> allCaches;
        lock (_lock)
        {
            allCaches = _cachesById.Values.OrderByDescending(cache => cache.Updated).ToList();
        }

        if (allCaches.Count <= maxCaches)
        {
            return;
        }

        List<SupplyCache> toRemove = allCaches.Skip(maxCaches).ToList();
        _server.Database.ExecuteInTransaction(connection =>
        {
            foreach (SupplyCache cache in toRemove)
            {
                RemoveCache(cache, connection);
            }
        });

        Logger.Info($"Supply cache dev trim: kept={maxCaches} removed={toRemove.Count}");
        SupplyCacheEventLog.Record(null, "dev_trim", $"kept={maxCaches} removed={toRemove.Count}");
    }

    public void HandlePeriodicPositionUpdate(GameClient client, PacketQueue? queue = null)
    {
        if (!IsEnabled || client.Character == null)
        {
            return;
        }

        if (!HasValidCharacterPosition(client.Character.X, client.Character.Y, client.Character.Z))
        {
            return;
        }

        if (!client.SupplyCachePositionKnown)
        {
            client.SupplyCachePositionKnown = true;
            SyncCachesNearCharacter(client, queue, force: true);
        }
    }

    public bool IsEnabled => _server.GameSettings.GameServerSettings.SupplyCachesEnabled;

    public int GetCacheCountForMap(uint mapId) => GetCachesForMap(mapId).Count;

    public List<string> DetectLegacyLowByteCollisions(uint mapId)
    {
        return GetCachesForMap(mapId)
            .GroupBy(cache => SupplyCacheDropTracker.GetPersistedDropSetId(cache.Id))
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"lowByte={group.Key} caches=[{string.Join(", ", group.Select(cache => cache.Id))}]")
            .ToList();
    }

    public readonly struct SupplyCacheProximitySummary
    {
        public bool PositionKnown { get; init; }
        public double PosX { get; init; }
        public float PosY { get; init; }
        public double PosZ { get; init; }
        public uint MapId { get; init; }
        public int MapCacheCount { get; init; }
        public int NearbyCacheCount { get; init; }
        public long? NearestCacheId { get; init; }
        public double? NearestDistance { get; init; }
    }

    public SupplyCacheProximitySummary GetProximitySummary(GameClient client)
    {
        StageLayoutId layout = ResolveActiveLayout(client);
        List<SupplyCache> mapCaches = GetCachesForMap(layout.Id);
        (double x, float y, double z) = ResolveCharacterPosition(client);
        bool positionKnown = HasValidCharacterPosition(x, y, z);

        long? nearestCacheId = null;
        double? nearestDistance = null;
        if (positionKnown && mapCaches.Count > 0)
        {
            var nearest = mapCaches
                .Select(cache => (cache, Distance(x, y, z, cache.X, cache.Y, cache.Z)))
                .OrderBy(entry => entry.Item2)
                .First();
            nearestCacheId = nearest.cache.Id;
            nearestDistance = nearest.Item2;
        }

        return new SupplyCacheProximitySummary
        {
            PositionKnown = positionKnown,
            PosX = x,
            PosY = y,
            PosZ = z,
            MapId = layout.Id,
            MapCacheCount = mapCaches.Count,
            NearbyCacheCount = positionKnown ? GetCachesNearCharacter(client, layout).Count : 0,
            NearestCacheId = nearestCacheId,
            NearestDistance = nearestDistance,
        };
    }

    public bool TryGetCache(uint setId, out SupplyCache? cache)
    {
        lock (_lock)
        {
            if (_cachesBySetId.TryGetValue(setId, out cache))
            {
                return true;
            }

            if (setId < SupplyCache.SetIdBase)
            {
                return _cachesBySetId.TryGetValue(SupplyCache.MakeSetId(setId), out cache);
            }

            cache = null;
            return false;
        }
    }

    public bool TryGetCacheById(long cacheId, out SupplyCache? cache)
    {
        lock (_lock)
        {
            return _cachesById.TryGetValue(cacheId, out cache);
        }
    }

    public bool TryResolveCache(GameClient client, StageLayoutId layout, uint setId, out SupplyCache? cache)
    {
        cache = null;

        if (client.SupplyCacheDropTracker.TryGetCacheId(setId, out long mappedCacheId)
            && TryGetCacheById(mappedCacheId, out cache)
            && cache != null
            && MatchesLayout(cache, layout))
        {
            return AdoptClientLayout(cache, layout);
        }

        if (client.SupplyCacheDropTracker.TryResolveCacheId(setId, out mappedCacheId)
            && TryGetCacheById(mappedCacheId, out cache)
            && cache != null
            && MatchesLayout(cache, layout))
        {
            return AdoptClientLayout(cache, layout);
        }

        if (TryGetCache(layout, setId, out cache) && cache != null)
        {
            return true;
        }

        cache = null;
        return false;
    }

    /// <summary>
    /// Keeps the server's active instance layout in sync with client instance packets.
    /// Required for dungeons and level-sync field areas where GroupId comes from layout load.
    /// </summary>
    public void AdoptClientLayout(GameClient client, StageLayoutId layout)
    {
        if (layout.Id == 0)
        {
            return;
        }

        if (layout.GroupId != 0)
        {
            client.InstanceLayoutId = layout;
            if (client.Character.Stage.Id == layout.Id)
            {
                client.Character.Stage = layout;
            }
        }
    }

    public void ClearCachesForInstancedStage(uint mapId)
    {
        if (!StageManager.IsDungeon(mapId))
        {
            return;
        }

        List<SupplyCache> toRemove = GetCachesForMap(mapId)
            .Where(cache => cache.GroupId != 0)
            .ToList();
        if (toRemove.Count == 0)
        {
            return;
        }

        _server.Database.ExecuteInTransaction(connection =>
        {
            foreach (SupplyCache cache in toRemove)
            {
                RemoveCache(cache, connection);
            }
        });

        Logger.Info($"SupplyCache instance cleanup: map={mapId} removed={toRemove.Count}");
    }

    private static bool MatchesLayout(SupplyCache cache, StageLayoutId layout)
    {
        if (cache.MapId != layout.Id)
        {
            return false;
        }

        if (layout.GroupId == 0)
        {
            return true;
        }

        return cache.LayerNo == layout.LayerNo
            && (cache.GroupId == layout.GroupId || cache.GroupId == 0);
    }

    private static bool AdoptClientLayout(SupplyCache cache, StageLayoutId layout)
    {
        if (layout.GroupId != 0 && (cache.GroupId != layout.GroupId || cache.LayerNo != layout.LayerNo))
        {
            cache.LayerNo = layout.LayerNo;
            cache.GroupId = layout.GroupId;
        }

        return true;
    }

    public List<CDataDropItemSetInfo> GetDropSetList(GameClient client, StageLayoutId layout, uint? focusWireSetId = null)
    {
        AdoptClientLayout(client, layout);
        List<CDataDropItemSetInfo> result = [];
        HashSet<long> seenCaches = [];

        foreach (SupplyCache cache in GetCachesForLayoutSync(client, layout))
        {
            if (!seenCaches.Add(cache.Id))
            {
                continue;
            }

            if (!TryResolveDropSetWireId(client, cache, focusWireSetId, out uint wireSetId))
            {
                continue;
            }

            result.Add(CreateDropSetInfo(cache, wireSetId));
        }

        return result;
    }

    private List<CDataDropItemSetInfo> GetPlayerDropSetList(
        GameClient client,
        StageLayoutId layout,
        uint? focusWireSetId = null)
    {
        AdoptClientLayout(client, layout);
        List<CDataDropItemSetInfo> result = [];
        HashSet<long> seenCaches = [];

        foreach (KeyValuePair<uint, long> mapping in client.SupplyCacheDropTracker.WireSetMappings)
        {
            uint wireSetId = mapping.Key;
            if (!SupplyCacheDropTracker.IsPlayerWireSetId(wireSetId)
                || client.SupplyCacheDropTracker.IsConsumed(wireSetId))
            {
                continue;
            }

            long cacheId = mapping.Value;
            if (!seenCaches.Add(cacheId))
            {
                continue;
            }

            if (!TryGetCacheById(cacheId, out SupplyCache? cache)
                || cache == null
                || !MatchesLayout(cache, layout))
            {
                continue;
            }

            result.Add(CreateDropSetInfo(cache, wireSetId));
        }

        if (focusWireSetId != null
            && client.SupplyCacheDropTracker.TryGetCacheId(focusWireSetId.Value, out long focusedCacheId)
            && TryGetCacheById(focusedCacheId, out SupplyCache? focusedCache)
            && focusedCache != null
            && !result.Any(dropSet => dropSet.Id == SupplyCacheDropTracker.ToClientWireSetId(focusWireSetId.Value)))
        {
            result.Add(CreateDropSetInfo(focusedCache, focusWireSetId.Value));
        }

        return result;
    }

    /// <summary>
    /// Drop sets that still need client registration. Used for GetDropItemSetList so a fresh
    /// discard does not re-register every bag already visible in the area.
    /// </summary>
    public List<CDataDropItemSetInfo> GetPendingDropSetList(GameClient client, StageLayoutId layout)
    {
        AdoptClientLayout(client, layout);
        List<CDataDropItemSetInfo> result = [];
        HashSet<long> seenCaches = [];

        foreach (KeyValuePair<uint, long> mapping in client.SupplyCacheDropTracker.WireSetMappings)
        {
            uint wireSetId = mapping.Key;
            if (client.SupplyCacheDropTracker.IsConsumed(wireSetId)
                || !client.SupplyCacheDropTracker.NeedsDropSetRegistration(wireSetId))
            {
                continue;
            }

            long cacheId = mapping.Value;
            if (!seenCaches.Add(cacheId))
            {
                continue;
            }

            if (!TryGetCacheById(cacheId, out SupplyCache? cache) || cache == null || !MatchesLayout(cache, layout))
            {
                continue;
            }

            result.Add(CreateDropSetInfo(cache, wireSetId));
        }

        return result;
    }

    private static bool TryResolveDropSetWireId(
        GameClient client,
        SupplyCache cache,
        uint? focusWireSetId,
        out uint wireSetId)
    {
        if (focusWireSetId != null
            && client.SupplyCacheDropTracker.TryGetCacheId(focusWireSetId.Value, out long focusedCacheId)
            && focusedCacheId == cache.Id)
        {
            wireSetId = focusWireSetId.Value;
            return true;
        }

        uint? canonicalWireSetId = client.SupplyCacheDropTracker.GetCanonicalWireSetIdForCache(cache.Id);
        if (canonicalWireSetId != null)
        {
            wireSetId = canonicalWireSetId.Value;
            return true;
        }

        wireSetId = 0;
        return false;
    }

    private uint ResolveDropSetWireId(GameClient client, SupplyCache cache, uint? focusWireSetId)
    {
        if (TryResolveDropSetWireId(client, cache, focusWireSetId, out uint wireSetId))
        {
            return wireSetId;
        }

        return ResolveSessionWireSetId(client, cache);
    }

    /// <summary>
    /// Registers one pending drop set without replacing the layout's full drop-set table.
    /// </summary>
    public bool SendFocusedDropSetRegistration(GameClient client, StageLayoutId layout, uint focusWireSetId)
    {
        if (!IsEnabled || layout.Id == 0)
        {
            SupplyCacheEventLog.Record(
                client,
                "reg_fail",
                $"wire={focusWireSetId} layout={layout} reason={(IsEnabled ? "no_map" : "disabled")}");
            return false;
        }

        if (!client.SupplyCacheDropTracker.TryGetCacheId(focusWireSetId, out long cacheId)
            || !TryGetCacheById(cacheId, out SupplyCache? focusedCache)
            || focusedCache == null)
        {
            Logger.Error(
                $"SupplyCache drop-set register failed: character={client.Character.CharacterId} " +
                $"layout={layout} focusWire={focusWireSetId} (no cache mapping)");
            SupplyCacheEventLog.Record(
                client,
                "reg_fail",
                $"wire={focusWireSetId} layout={layout} reason=no_cache_mapping");
            return false;
        }

        List<CDataDropItemSetInfo> dropSets = GetDropSetList(client, layout);
        if (!dropSets.Any(dropSet => dropSet.Id == SupplyCacheDropTracker.ToClientWireSetId(focusWireSetId)))
        {
            dropSets.Add(CreateDropSetInfo(focusedCache, focusWireSetId));
        }

        dropSets = FinalizeRegistrationPayload(client, layout, dropSets, "reg_focus");
        StageLayoutId regLayout = GetCachePopLayout(focusedCache, layout);
        client.Send(new S2CInstanceGetDropItemSetListRes
        {
            LayoutId = regLayout.ToCDataStageLayoutId(),
            DropItemSetList = dropSets,
        });
        SendDropSetRegistrationViaEnemySetList(client, regLayout, dropSets, alreadyValidated: true);

        Logger.Info(
            $"SupplyCache drop-set register: character={client.Character.CharacterId} reqLayout={layout} " +
            $"regLayout={regLayout} focusWire={focusWireSetId} dropSets={dropSets.Count} (GetDropItemSetListRes)");
        SupplyCacheEventLog.Record(
            client,
            "reg_focus",
            $"wire={focusWireSetId} cache={focusedCache.Id} reqLayout={layout} regLayout={regLayout} path=GetDropItemSetListRes");
        return true;
    }

    /// <summary>
    /// Registers drop sets after sync pops. The client replaces its entire drop-set table on each
    /// GetDropItemSetListRes, so always send the full active list—not just newly pending entries.
    /// </summary>
    public void SendPendingDropSetRegistrations(GameClient client, StageLayoutId layout, PacketQueue? queue = null)
    {
        if (!IsEnabled || layout.Id == 0)
        {
            return;
        }

        List<CDataDropItemSetInfo> pending = GetPendingDropSetList(client, layout);
        if (pending.Count == 0)
        {
            return;
        }

        List<CDataDropItemSetInfo> dropSets = GetDropSetList(client, layout);
        if (dropSets.Count == 0)
        {
            dropSets = pending;
        }

        dropSets = FinalizeRegistrationPayload(client, layout, dropSets, "reg_batch");
        StageLayoutId regLayout = ResolveDropSetRegistrationLayout(client, layout, dropSets);
        S2CInstanceGetDropItemSetListRes registration = new()
        {
            LayoutId = regLayout.ToCDataStageLayoutId(),
            DropItemSetList = dropSets,
        };

        if (queue != null)
        {
            client.Enqueue(registration, queue);
        }
        else
        {
            client.Send(registration);
        }

        foreach (CDataDropItemSetInfo dropSet in pending)
        {
            client.SupplyCacheDropTracker.ClearDropSetRegistration(dropSet.Id);
        }

        Logger.Info(
            $"SupplyCache drop-set register: character={client.Character.CharacterId} reqLayout={layout} " +
            $"regLayout={regLayout} dropSets={dropSets.Count} pending={pending.Count} (GetDropItemSetListRes batch)");
        SupplyCacheEventLog.Record(
            client,
            "reg_batch",
            $"reqLayout={layout} regLayout={regLayout} count={dropSets.Count} pending={pending.Count}");

        SendDropSetRegistrationViaEnemySetList(client, layout, dropSets, queue, alreadyValidated: true);
    }

    /// <summary>
    /// Registers drop sets through GetEnemySetListRes. Used for layout entry where the client
    /// expects the full active drop-set list bundled with the area load response.
    /// </summary>
    public bool SendDropSetRegistrationViaEnemySetList(
        GameClient client,
        StageLayoutId layout,
        List<CDataDropItemSetInfo>? dropSets = null,
        PacketQueue? queue = null,
        bool alreadyValidated = false)
    {
        if (!IsEnabled || layout.Id == 0)
        {
            return false;
        }

        dropSets ??= GetDropSetList(client, layout);
        if (dropSets.Count == 0)
        {
            return false;
        }

        if (!alreadyValidated)
        {
            dropSets = FinalizeRegistrationPayload(client, layout, dropSets, "reg_enemyset");
        }

        S2CInstanceGetEnemySetListRes response = client.BuildEnemySetListResWithDropSets(layout, dropSets);
        client.RememberEnemySetListRes(layout, response);
        if (queue != null)
        {
            client.Enqueue(response, queue);
        }
        else
        {
            client.Send(response);
        }

        Logger.Info(
            $"SupplyCache drop-set register: character={client.Character.CharacterId} layout={layout} dropSets={dropSets.Count} (GetEnemySetListRes)");
        SupplyCacheEventLog.Record(
            client,
            "reg_enemyset",
            $"layout={layout} count={dropSets.Count}");
        return true;
    }

    public S2CInstanceGetDropItemSetListRes BuildClientDropItemSetListResponse(
        GameClient client,
        StageLayoutId layout)
    {
        List<CDataDropItemSetInfo> dropSets =
            FinalizeRegistrationPayload(client, layout, GetDropSetList(client, layout), "reg_client_req");
        return new S2CInstanceGetDropItemSetListRes
        {
            LayoutId = layout.ToCDataStageLayoutId(),
            DropItemSetList = dropSets,
        };
    }

    public void ClearPendingDropSetRegistration(GameClient client, StageLayoutId layout)
    {
        foreach (KeyValuePair<uint, long> mapping in client.SupplyCacheDropTracker.WireSetMappings)
        {
            if (TryGetCacheById(mapping.Value, out SupplyCache? cache)
                && cache != null
                && MatchesLayout(cache, layout))
            {
                client.SupplyCacheDropTracker.ClearDropSetRegistration(mapping.Key);
            }
        }

        foreach (SupplyCache cache in GetCachesForLayoutSync(client, layout))
        {
            if (TryGetWireSetIdForCache(client, cache.Id, out uint wireSetId))
            {
                client.SupplyCacheDropTracker.ClearDropSetRegistration(wireSetId);
            }
        }
    }

    /// <summary>
    /// Respawns persisted supply caches when entering a layout. Uses the session wire id for
    /// the player's last discard when available; otherwise assigns stable ids from cache rows.
    /// </summary>
    /// <summary>
    /// Field layout loads (open world and level-sync sub-areas). Proximity sync and player-drop
    /// refresh cover areas without flooding wrong sub-layouts from GetEnemySetList.
    /// </summary>
    public void HandleFieldLayoutLoad(
        GameClient client,
        StageLayoutId layout,
        StageLayoutId previousLayout,
        PacketQueue queue)
    {
        if (!IsEnabled || layout.Id == 0 || StageManager.IsDungeon(layout.Id))
        {
            return;
        }

        AdoptClientLayout(client, layout);
        RestoreRecentPlayerDropBag(client, layout, queue);
        RefreshPlayerDropBags(client, layout, queue);

        bool subAreaReload = previousLayout.Id == layout.Id && !previousLayout.Equals(layout);
        if (subAreaReload)
        {
            ClearSyncedMarkersNearCharacter(client, layout);
            SyncCachesNearCharacter(client, queue, force: true);
            return;
        }

        if (HasValidCharacterPosition(client.Character.X, client.Character.Y, client.Character.Z))
        {
            SyncCachesNearCharacter(client, queue, force: true);
        }
    }

    public void SyncCachesForLayout(GameClient client, StageLayoutId layout, PacketQueue? queue = null)
    {
        if (!IsEnabled || layout.Id == 0 || layout.GroupId == 0)
        {
            return;
        }

        AdoptClientLayout(client, layout);

        List<SupplyCache> layoutCaches = HasValidCharacterPosition(client.Character.X, client.Character.Y, client.Character.Z)
            ? GetCachesNearCharacter(client, layout)
            : GetCachesForLayoutSync(client, layout);
        if (layoutCaches.Count == 0)
        {
            return;
        }

        long? priorityCacheId = client.SupplyCacheDropTracker.TryGetLastDroppedCacheId(layout.Id);
        uint priorityWireSetId = client.SupplyCacheDropTracker.TryGetLastDroppedWireSetId(layout.Id);

        List<(SupplyCache Cache, uint WireSetId)> prepared = [];
        foreach (SupplyCache cache in layoutCaches)
        {
            uint wireSetId = ResolveSessionWireSetId(client, cache, priorityCacheId, priorityWireSetId, "layout-sync");
            if (PreparePersistedCachePop(client, cache, layout, wireSetId))
            {
                prepared.Add((cache, wireSetId));
            }
        }

        if (prepared.Count == 0)
        {
            return;
        }

        SendPendingDropSetRegistrations(client, layout, queue);
        foreach ((SupplyCache cache, uint wireSetId) in prepared)
        {
            SendWorldCachePop(client, cache, layout, wireSetId, queue);
        }

        Logger.Info(
            $"SupplyCache layout sync: character={client.Character.CharacterId} layout={layout} caches={prepared.Count}");
        SupplyCacheEventLog.Record(
            client,
            "layout_sync",
            $"layout={layout} caches={prepared.Count} group={layout.GroupId}");
    }

    /// <summary>
    /// Respawns persisted caches when the client reports position via RPC. GetEnemySetList often
    /// runs before the first PERIODIC_TOP update after login, so proximity sync would otherwise
    /// see (0,0,0) and skip every nearby bag.
    /// </summary>
    public void SyncCachesNearCharacter(GameClient client, PacketQueue? queue = null, bool force = false)
    {
        if (!IsEnabled)
        {
            return;
        }

        StageLayoutId layout = ResolveActiveLayout(client);
        if (layout.Id == 0 || StageManager.IsDungeon(layout.Id))
        {
            return;
        }

        if (!HasValidCharacterPosition(client.Character.X, client.Character.Y, client.Character.Z))
        {
            return;
        }

        if (client.SupplyCacheDropTracker.IsProximitySyncPaused)
        {
            SupplyCacheEventLog.Record(
                client,
                "proximity_sync_skip",
                $"layout={layout} reason=player_drop_pause");
            return;
        }

        if (client.SupplyCacheDropTracker.HasAnyActivePlayerDrop())
        {
            SupplyCacheEventLog.Record(
                client,
                "proximity_sync_skip",
                $"layout={layout} reason=active_player_bag");
            return;
        }

        if (!force && client.SupplyCacheDropTracker.IsLayoutProximitySyncComplete(layout))
        {
            return;
        }

        if (!client.SupplyCacheDropTracker.TryBeginProximitySyncBurst(force))
        {
            return;
        }

        List<SupplyCache> nearbyCaches = GetCachesNearCharacter(client, layout);
        if (nearbyCaches.Count == 0)
        {
            return;
        }

        List<(SupplyCache Cache, uint WireSetId)> prepared = [];
        foreach (SupplyCache cache in nearbyCaches)
        {
            if (prepared.Count >= MaxCachesPerProximityBurst)
            {
                break;
            }

            if (client.SupplyCacheDropTracker.HasActivePlayerDrop(cache.Id))
            {
                continue;
            }

            if (client.SupplyCacheDropTracker.IsRecentPlayerDropCache(layout.Id, cache.Id))
            {
                continue;
            }

            if (!client.SupplyCacheDropTracker.TryMarkCacheSynced(cache.Id, layout))
            {
                continue;
            }

            RetirePersistedWireMappingsForCache(client, layout, cache.Id);

            uint wireSetId = ResolveSessionWireSetId(client, cache, reason: "proximity-sync");
            PrepareWorldCacheDrop(client, cache, layout, wireSetId);
            prepared.Add((cache, wireSetId));
        }

        if (prepared.Count == 0)
        {
            return;
        }

        SendPendingDropSetRegistrations(client, layout, queue);
        foreach ((SupplyCache cache, uint wireSetId) in prepared)
        {
            SendWorldCachePop(client, cache, layout, wireSetId, queue);
        }

        Logger.Info(
            $"SupplyCache proximity sync: character={client.Character.CharacterId} layout={layout} caches={prepared.Count}");
        SupplyCacheEventLog.Record(
            client,
            "proximity_sync",
            $"layout={layout} caches={prepared.Count} group={layout.GroupId}");
        client.SupplyCacheDropTracker.MarkLayoutProximitySyncComplete(layout);
    }

    public bool EnqueueDropSetRegistration(
        GameClient client,
        StageLayoutId layout,
        uint wireSetId,
        PacketQueue queue)
    {
        if (!IsEnabled || layout.Id == 0)
        {
            return false;
        }

        if (!client.SupplyCacheDropTracker.TryGetCacheId(wireSetId, out long cacheId)
            || !TryGetCacheById(cacheId, out SupplyCache? cache)
            || cache == null)
        {
            return false;
        }

        List<CDataDropItemSetInfo> dropSets = GetPlayerDropSetList(client, layout, wireSetId);
        SupplyCacheRegistrationValidation validation = SupplyCacheRegistrationGuard.ValidatePlayerWireRegistration(
            client.SupplyCacheDropTracker,
            dropSets.Select(dropSet => (uint)dropSet.Id));
        client.RegistrationAudit.Record("reg_drop", layout, validation);
        SupplyCacheEventLog.Record(
            client,
            validation.Passed ? "reg_ok" : "reg_incomplete",
            $"path=reg_drop layout={layout} {validation.Summary}");
        if (!validation.Passed)
        {
            Logger.Error(
                $"[SUPPLY_CACHE_VERIFY] incomplete player-drop registration layout={layout} {validation.Summary}");
        }

        StageLayoutId regLayout = GetCachePopLayout(cache, layout);
        client.Enqueue(new S2CInstanceGetDropItemSetListRes
        {
            LayoutId = regLayout.ToCDataStageLayoutId(),
            DropItemSetList = dropSets,
        }, queue);
        SendDropSetRegistrationViaEnemySetList(client, regLayout, dropSets, queue, alreadyValidated: true);
        client.SupplyCacheDropTracker.ClearDropSetRegistration(wireSetId);
        SupplyCacheEventLog.Record(
            client,
            "reg_drop",
            $"wire={wireSetId} cache={cache.Id} reqLayout={layout} regLayout={regLayout} count={dropSets.Count} path=queued");
        return true;
    }

    private StageLayoutId ResolveDropSetRegistrationLayout(
        GameClient client,
        StageLayoutId layout,
        List<CDataDropItemSetInfo> dropSets)
    {
        if (layout.GroupId != 0)
        {
            return layout;
        }

        if (dropSets.Count == 0)
        {
            return layout;
        }

        if (client.SupplyCacheDropTracker.TryGetCacheId(dropSets[0].Id, out long firstCacheId)
            && TryGetCacheById(firstCacheId, out SupplyCache? firstCache)
            && firstCache != null)
        {
            return GetCachePopLayout(firstCache, layout);
        }

        return layout;
    }

    private void RefreshPlayerDropBags(GameClient client, StageLayoutId layout, PacketQueue queue)
    {
        List<(SupplyCache Cache, uint WireSetId)> prepared = [];
        foreach (KeyValuePair<uint, long> mapping in client.SupplyCacheDropTracker.WireSetMappings.ToList())
        {
            uint wireSetId = mapping.Key;
            if (!SupplyCacheDropTracker.IsPlayerWireSetId(wireSetId)
                || !client.SupplyCacheDropTracker.HasTrackedItems(wireSetId))
            {
                continue;
            }

            if (!TryGetCacheById(mapping.Value, out SupplyCache? cache) || cache == null || cache.MapId != layout.Id)
            {
                continue;
            }

            client.SupplyCacheDropTracker.ClearCacheSynced(cache.Id);
            PrepareWorldCacheDrop(client, cache, layout, wireSetId);
            prepared.Add((cache, wireSetId));
        }

        if (prepared.Count == 0)
        {
            return;
        }

        SendPendingDropSetRegistrations(client, layout, queue);
        foreach ((SupplyCache cache, uint wireSetId) in prepared)
        {
            SendWorldCachePop(client, cache, layout, wireSetId, queue);
        }

        SupplyCacheEventLog.Record(client, "player_drop_refresh", $"layout={layout}");
    }

    /// <summary>
    /// After a zone change the wire tracker is reset but the last discard cache remains in SQLite.
    /// Re-bind it to wire 1 and respawn PopDrop before persisted proximity sync can steal the id.
    /// </summary>
    private void RestoreRecentPlayerDropBag(GameClient client, StageLayoutId layout, PacketQueue queue)
    {
        long? cacheId = client.SupplyCacheDropTracker.TryGetLastDroppedCacheId(layout.Id);
        if (cacheId == null
            || !TryGetCacheById(cacheId.Value, out SupplyCache? cache)
            || cache == null
            || cache.Items.Count == 0
            || cache.MapId != layout.Id
            || !MatchesLayout(cache, layout))
        {
            return;
        }

        if (client.SupplyCacheDropTracker.HasActivePlayerDrop(cache.Id))
        {
            return;
        }

        uint wireSetId = 1;
        client.SupplyCacheDropTracker.RegisterDrop(wireSetId, cache.Id, "player-drop-restore");
        foreach ((ushort slot, _) in cache.Items)
        {
            client.SupplyCacheDropTracker.TrackPlayerDrop(wireSetId, cache.Id, slot);
        }

        client.SupplyCacheDropTracker.MarkPendingDropSetRegistration(wireSetId);
        RegisterDropItems(client, cache, wireSetId, layout);
        client.SupplyCacheDropTracker.ClearCacheSynced(cache.Id);

        if (!EnqueueDropSetRegistration(client, layout, wireSetId, queue))
        {
            Logger.Error(
                $"SupplyCache player-drop restore registration failed: character={client.Character.CharacterId} cache={cache.Id} layout={layout}");
            return;
        }

        StageLayoutId popLayout = GetCachePopLayout(cache, layout);
        client.Enqueue(CreatePopDropNtc(cache, popLayout, wireSetId), queue);
        client.SupplyCacheDropTracker.TryMarkCacheSynced(cache.Id, layout);

        Logger.Info(
            $"SupplyCache player-drop restore: character={client.Character.CharacterId} cache={cache.Id} wire={wireSetId} layout={layout}");
        SupplyCacheEventLog.Record(
            client,
            "player_drop_restore",
            $"cache={cache.Id} wire={wireSetId} layout={layout} items={cache.Items.Count}");
    }

    private void ClearSyncedMarkersNearCharacter(GameClient client, StageLayoutId layout)
    {
        foreach (SupplyCache cache in GetCachesNearCharacter(client, layout))
        {
            if (!client.SupplyCacheDropTracker.HasActivePlayerDrop(cache.Id))
            {
                client.SupplyCacheDropTracker.ClearCacheSynced(cache.Id);
            }
        }
    }

    private uint ResolveSessionWireSetId(
        GameClient client,
        SupplyCache cache,
        long? priorityCacheId = null,
        uint priorityWireSetId = 1,
        string reason = "drop-set-list")
    {
        if (TryGetWireSetIdForCache(client, cache.Id, out uint existingWireSetId))
        {
            return existingWireSetId;
        }

        if (priorityCacheId == cache.Id && SupplyCacheDropTracker.IsPlayerWireSetId(priorityWireSetId))
        {
            client.SupplyCacheDropTracker.RegisterDrop(priorityWireSetId, cache.Id, "layout-sync-priority");
            return priorityWireSetId;
        }

        return AllocateUniquePersistedWireSetId(client, cache.Id, reason);
    }

    private uint AllocateUniquePersistedWireSetId(GameClient client, long cacheId, string reason)
    {
        for (int attempt = 0; attempt < 128; attempt++)
        {
            uint wireSetId = client.SupplyCacheDropTracker.AllocatePersistedWireSetId();
            if (!client.SupplyCacheDropTracker.TryGetCacheId(wireSetId, out long mappedCacheId) || mappedCacheId == cacheId)
            {
                client.SupplyCacheDropTracker.RegisterDrop(wireSetId, cacheId, reason);
                return wireSetId;
            }
        }

        uint fallbackWireSetId = client.SupplyCacheDropTracker.AllocateWireSetId();
        client.SupplyCacheDropTracker.RegisterDrop(fallbackWireSetId, cacheId, reason + "-fallback");
        Logger.Error(
            $"SupplyCache wire fallback: character={client.Character.CharacterId} cache={cacheId} wire={fallbackWireSetId}");
        return fallbackWireSetId;
    }

    private bool PreparePersistedCachePop(
        GameClient client,
        SupplyCache cache,
        StageLayoutId layout,
        uint wireSetId)
    {
        if (client.SupplyCacheDropTracker.HasActivePlayerDrop(cache.Id))
        {
            return false;
        }

        if (!client.SupplyCacheDropTracker.TryMarkCacheSynced(cache.Id, layout))
        {
            return false;
        }

        RetirePersistedWireMappingsForCache(client, layout, cache.Id);
        PrepareWorldCacheDrop(client, cache, layout, wireSetId);
        Logger.Info(
            $"SupplyCache pop persisted: character={client.Character.CharacterId} cache={cache.Id} wire={wireSetId} layout={layout}");
        return true;
    }

    public bool TryGetCache(StageLayoutId layout, uint setId, out SupplyCache? cache)
    {
        cache = null;
        if (!TryGetCache(setId, out cache) || cache == null)
        {
            return false;
        }

        if (cache.MapId != layout.Id)
        {
            cache = null;
            return false;
        }

        if (!MatchesLayout(cache, layout))
        {
            cache = null;
            return false;
        }

        // Stage area changes reset GroupId to 0 until the client loads a layout set.
        // Adopt the client's layout identifiers on first interaction.
        if (layout.GroupId != 0 && (cache.GroupId != layout.GroupId || cache.LayerNo != layout.LayerNo))
        {
            cache.LayerNo = layout.LayerNo;
            cache.GroupId = layout.GroupId;
        }

        return true;
    }

    public List<CDataGatheringItemElement> GetDropList(SupplyCache cache)
    {
        return BuildDropListElements(cache.Items.Select((entry, index) => (entry, index)));
    }

    public List<CDataGatheringItemElement> GetDropList(SupplyCache cache, uint wireSetId, SupplyCacheDropTracker tracker)
    {
        if (tracker.TryGetTrackedItemSlots(wireSetId, out HashSet<ushort>? trackedSlots))
        {
            return BuildDropListElements(
                cache.Items
                    .Select((entry, index) => (entry, index))
                    .Where(x => trackedSlots.Contains(x.entry.Slot)));
        }

        return GetDropList(cache);
    }

    private static List<CDataGatheringItemElement> BuildDropListElements(
        IEnumerable<((ushort Slot, SupplyCacheItemData Item) Entry, int Index)> entries)
    {
        return entries
            .Select(x => new CDataGatheringItemElement
            {
                SlotNo = (uint)x.Index,
                ItemId = x.Entry.Item.ItemId,
                ItemNum = x.Entry.Item.Num,
                Quality = x.Entry.Item.PlusValue,
                IsHidden = false,
            })
            .ToList();
    }

    public SupplyCacheItemData? GetItemAtSlot(SupplyCache cache, uint slotNo)
    {
        if (slotNo >= cache.Items.Count)
        {
            return null;
        }

        return cache.Items[(int)slotNo].Item;
    }

    public void NotifyCachesForClient(GameClient client)
    {
        if (!IsEnabled)
        {
            return;
        }

        client.SupplyCacheDropTracker.ResetForStage(client.Character.Stage.Id);
        client.SupplyCachePositionKnown = false;
        client.RegistrationAudit.Clear();
        client.ClearEnemySetListCache(client.Character.Stage.Id);
    }

    private List<CDataDropItemSetInfo> FinalizeRegistrationPayload(
        GameClient client,
        StageLayoutId layout,
        List<CDataDropItemSetInfo> dropSets,
        string path)
    {
        SupplyCacheRegistrationValidation validation =
            SupplyCacheRegistrationGuard.ValidateRegistrationList(this, client, layout, dropSets);
        if (!validation.Passed)
        {
            dropSets = GetDropSetList(client, layout);
            validation = SupplyCacheRegistrationGuard.ValidateRegistrationList(this, client, layout, dropSets);
        }

        client.RegistrationAudit.Record(path, layout, validation);
        SupplyCacheEventLog.Record(
            client,
            validation.Passed ? "reg_ok" : "reg_incomplete",
            $"path={path} layout={layout} {validation.Summary}");
        if (!validation.Passed)
        {
            Logger.Error(
                $"[SUPPLY_CACHE_VERIFY] incomplete registration path={path} layout={layout} {validation.Summary}");
        }

        return dropSets;
    }

    public S2CInstancePopDropItemNtc? HandleDrop(GameClient client, StorageType storageType, Item item, uint num, DbConnection connection)
    {
        GameServerSettings settings = _server.GameSettings.GameServerSettings;
        if (!settings.SupplyCachesEnabled)
        {
            return null;
        }

        if (item.SafetySetting != 0)
        {
            throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_INVALID_ITEM_NUM, "Protected items cannot be dropped.");
        }

        if (storageType == StorageType.KeyItems && !settings.SupplyCacheAllowQuestItems)
        {
            throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_INVALID_ITEM_NUM, "Quest items cannot be placed in supply caches.");
        }

        if (!ItemManager.ItemBagStorageTypes.Contains(storageType) && storageType != StorageType.KeyItems)
        {
            return null;
        }

        DateTime now = DateTime.UtcNow;
        SupplyCacheItemData itemData = SupplyCacheItemData.FromItem(new Item(item), num);
        StageLayoutId layout = ResolveDropLayout(client);
        AdoptClientLayout(client, layout);
        SupplyCache cache = FindOrCreateCache(client, layout, now, connection);

        if (cache.Items.Count >= settings.SupplyCacheMaxItemsPerCache)
        {
            cache = CreateCacheNear(client, layout, now, connection);
        }

        ushort slot = GetNextSlot(cache);
        cache.Items.Add((slot, itemData));
        cache.Updated = now;
        _server.Database.UpsertSupplyCacheItem(cache.Id, slot, itemData, connection);
        _server.Database.UpdateSupplyCacheTimestamp(cache.Id, now, connection);

        RetireWireMappingsForCache(client, layout, cache.Id);
        client.SupplyCacheDropTracker.ClearCacheSynced(cache.Id);

        uint wireSetId = client.SupplyCacheDropTracker.AllocateWireSetId();
        client.SupplyCacheDropTracker.TrackPlayerDrop(wireSetId, cache.Id, slot);
        client.SupplyCacheDropTracker.RememberLastDrop(cache.MapId, cache.Id, wireSetId);
        client.SupplyCacheDropTracker.MarkPendingDropSetRegistration(wireSetId);
        RegisterDropItems(client, cache, wireSetId, layout, [slot]);

        S2CInstancePopDropItemNtc dropNtc = CreatePopDropNtc(cache, GetCachePopLayout(cache, layout), wireSetId);
        BroadcastPopDrop(cache, client, layout, slot, dropNtc);
        client.SupplyCacheDropTracker.TryMarkCacheSynced(cache.Id, layout);

        Logger.Info(
            $"SupplyCache drop: character={client.Character.CharacterId} map={cache.MapId} layout={layout} cache={cache.Id} wireSetId={wireSetId} popSetId={wireSetId} cacheSetId={cache.SetId} item={(ItemId)item.ItemId} x{num} pos=({cache.X:F1}, {cache.Y:F1}, {cache.Z:F1})");
        SupplyCacheEventLog.Record(
            client,
            "drop",
            $"cache={cache.Id} wire={wireSetId} layout={layout} group={layout.GroupId} item={(ItemId)item.ItemId}x{num}");
        SupplyCacheEventLog.Record(
            client,
            "proximity_pause",
            $"seconds={(int)SupplyCacheDropTracker.ProximitySyncPauseAfterPlayerDrop.TotalSeconds} wire={wireSetId} cache={cache.Id}");

        client.SupplyCacheDiagnostics.RecordDropAttempt(
            client,
            storageType,
            item.ItemId,
            num,
            layout,
            cache.Id,
            wireSetId,
            wireSetId,
            dropNtc);
        client.SupplyCacheDiagnostics.LogDropEvent(_server, client, client.Character.CharacterId, "drop");

        return dropNtc;
    }

    public void RetirePersistedWireMappingsForCache(GameClient client, StageLayoutId layout, long cacheId)
    {
        List<uint> staleWireSetIds = client.SupplyCacheDropTracker.GetWireSetIdsForCache(cacheId)
            .Where(wireSetId => SupplyCacheDropTracker.IsPersistedWireSetId(wireSetId))
            .ToList();

        foreach (uint wireSetId in staleWireSetIds)
        {
            client.SupplyCacheDropTracker.MarkConsumed(wireSetId);
            client.InstanceDropItemManager.Remove(layout, wireSetId);
            client.Send(new S2CInstanceGetDropItemListRes
            {
                LayoutId = layout.ToCDataStageLayoutId(),
                SetId = wireSetId,
                ItemList = [],
            });

            Logger.Info(
                $"[SUPPLY_CACHE_WIRE] retire persisted wire={wireSetId} cache={cacheId} cha={client.Character.CharacterId}");
        }
    }

    public bool TryLoot(GameClient client, SupplyCache cache, uint slotNo, uint requestedNum, S2CItemUpdateCharacterItemNtc ntc, DbConnection connection)
    {
        if (slotNo >= cache.Items.Count)
        {
            throw new ResponseErrorException(ErrorCode.ERROR_CODE_INSTANCE_AREA_INVALID_DROP_ITEM_ID);
        }

        (ushort slot, SupplyCacheItemData itemData) = cache.Items[(int)slotNo];
        uint lootNum = Math.Min(requestedNum, itemData.Num);
        if (lootNum == 0)
        {
            throw new ResponseErrorException(ErrorCode.ERROR_CODE_INSTANCE_AREA_INVALID_DROP_ITEM_ID);
        }

        Item item = itemData.ToItem();
        if (!_server.ItemManager.CanAddItem(client.Character, _server.AssetRepository.ClientItemInfos[item.ItemId].StorageType, item.ItemId, lootNum))
        {
            throw new ResponseErrorException(ErrorCode.ERROR_CODE_ITEM_STORAGE_OVERFLOW);
        }

        CDataItemUpdateResult result = _server.ItemManager.AddNewItem(_server, client.Character, true, item, lootNum, connection);
        ntc.UpdateItemList.Add(result);

        itemData.Num -= lootNum;
        if (itemData.Num == 0)
        {
            cache.Items.RemoveAt((int)slotNo);
            _server.Database.DeleteSupplyCacheItem(cache.Id, slot, connection);
        }
        else
        {
            _server.Database.UpsertSupplyCacheItem(cache.Id, slot, itemData, connection);
        }

        cache.Updated = DateTime.UtcNow;
        _server.Database.UpdateSupplyCacheTimestamp(cache.Id, cache.Updated, connection);

        if (cache.Items.Count == 0)
        {
            RemoveCache(cache, connection);
        }

        Logger.Info(
            $"SupplyCache loot: character={client.Character.CharacterId} map={cache.MapId} cache={cache.Id} item={(ItemId)item.ItemId} x{lootNum}");

        return cache.Items.Count > 0;
    }

    public void FinalizeSupplyCacheLoot(GameClient client, StageLayoutId layout, uint setId, long cacheId, PacketQueue queue)
    {
        if (!TryGetCacheById(cacheId, out SupplyCache? cache) || cache == null)
        {
            client.SupplyCacheDropTracker.RemoveMappingsForCache(cacheId);
            DismissAllPlayerBags(client, layout, cacheId, queue);
            return;
        }

        HashSet<ushort> remainingItemSlots = cache.Items.Select(x => x.Slot).ToHashSet();
        client.SupplyCacheDropTracker.PruneTrackedSlots(setId, remainingItemSlots);

        if (!client.SupplyCacheDropTracker.HasTrackedItems(setId))
        {
            DismissPlayerBag(client, layout, setId, queue);
        }
        else
        {
            SyncDropItems(client, cache, setId, layout);
        }
    }

    public void DismissAllPlayerBags(GameClient client, StageLayoutId layout, long cacheId, PacketQueue queue)
    {
        foreach (uint wireSetId in client.SupplyCacheDropTracker.GetWireSetIdsForCache(cacheId))
        {
            DismissPlayerBag(client, layout, wireSetId, queue);
        }
    }

    public void DismissPlayerBag(GameClient client, StageLayoutId layout, uint wireSetId, PacketQueue queue)
    {
        if (client.SupplyCacheDropTracker.IsConsumed(wireSetId))
        {
            return;
        }

        client.SupplyCacheDropTracker.MarkConsumed(wireSetId);
        client.InstanceDropItemManager.Remove(layout, wireSetId);
        client.Enqueue(new S2CInstanceGetDropItemListRes
        {
            LayoutId = layout.ToCDataStageLayoutId(),
            SetId = wireSetId,
            ItemList = [],
        }, queue);
    }

    public void RetireWireMappingsForCache(GameClient client, StageLayoutId layout, long cacheId, uint? exceptWireSetId = null)
    {
        List<uint> staleWireSetIds = client.SupplyCacheDropTracker.GetWireSetIdsForCache(cacheId)
            .Where(wireSetId => exceptWireSetId == null || wireSetId != exceptWireSetId.Value)
            .ToList();

        foreach (uint wireSetId in staleWireSetIds)
        {
            client.SupplyCacheDropTracker.MarkConsumed(wireSetId);
            client.InstanceDropItemManager.Remove(layout, wireSetId);
            client.Send(new S2CInstanceGetDropItemListRes
            {
                LayoutId = layout.ToCDataStageLayoutId(),
                SetId = wireSetId,
                ItemList = [],
            });

            Logger.Info(
                $"[SUPPLY_CACHE_WIRE] retire wire={wireSetId} cache={cacheId} cha={client.Character.CharacterId}");
        }
    }

    public void SyncDropItems(GameClient client, SupplyCache cache, uint wireSetId, StageLayoutId layout)
    {
        if (client.SupplyCacheDropTracker.TryGetTrackedItemSlots(wireSetId, out HashSet<ushort>? trackedSlots))
        {
            RegisterDropItems(client, cache, wireSetId, layout, trackedSlots);
            return;
        }

        RegisterDropItems(client, cache, wireSetId, layout);
    }

    private SupplyCache FindOrCreateCache(GameClient client, StageLayoutId layout, DateTime now, DbConnection connection)
    {
        GameServerSettings settings = _server.GameSettings.GameServerSettings;
        List<SupplyCache> mapCaches = GetCachesForMap(layout.Id);
        double mergeRadius = settings.SupplyCacheMergeRadius;

        SupplyCache? nearest = mapCaches
            .Select(cache => (cache, Distance(client.Character.X, client.Character.Y, client.Character.Z, cache.X, cache.Y, cache.Z)))
            .Where(x => x.Item2 <= mergeRadius)
            .OrderBy(x => x.Item2)
            .Select(x => x.cache)
            .FirstOrDefault();

        if (nearest != null)
        {
            nearest.Updated = now;
            if (StageManager.IsDungeon(layout.Id) && layout.GroupId != 0)
            {
                nearest.LayerNo = layout.LayerNo;
                nearest.GroupId = layout.GroupId;
                _server.Database.UpdateSupplyCachePosition(nearest, connection);
            }

            _server.Database.UpdateSupplyCacheTimestamp(nearest.Id, now, connection);
            return nearest;
        }

        if (mapCaches.Count >= settings.SupplyCacheMaxCachesPerMap
            || _cachesById.Count >= settings.SupplyCacheMaxCachesServerWide)
        {
            return CreateCacheNear(client, layout, now, connection);
        }

        return CreateCacheNear(client, layout, now, connection);
    }

    private SupplyCache CreateCacheNear(GameClient client, StageLayoutId layout, DateTime now, DbConnection connection)
    {
        GameServerSettings settings = _server.GameSettings.GameServerSettings;
        List<SupplyCache> mapCaches = GetCachesForMap(layout.Id);
        (double x, float y, double z) = FindSpawnPosition(client, mapCaches, settings);
        bool isDungeon = StageManager.IsDungeon(layout.Id);

        SupplyCache cache = new()
        {
            MapId = layout.Id,
            LayerNo = isDungeon ? layout.LayerNo : (byte)0,
            GroupId = isDungeon ? layout.GroupId : 0U,
            X = x,
            Y = y,
            Z = z,
            Rotation = 0,
            Created = now,
            Updated = now,
        };

        long cacheId = _server.Database.InsertSupplyCache(cache, connection);
        cache.Id = cacheId;
        RegisterCache(cache);

        Logger.Info($"SupplyCache spawned: map={cache.MapId} cache={cache.Id} setId={cache.SetId} at ({x:F1}, {y:F1}, {z:F1})");
        return cache;
    }

    private (double X, float Y, double Z) FindSpawnPosition(GameClient client, List<SupplyCache> mapCaches, GameServerSettings settings)
    {
        if (settings.SupplyCacheSpawnRadiusMin <= 0 && settings.SupplyCacheSpawnRadiusMax <= 0)
        {
            return (client.Character.X, client.Character.Y, client.Character.Z);
        }

        for (int attempt = 0; attempt < settings.SupplyCacheSpawnRetries; attempt++)
        {
            double angle = Random.Shared.NextDouble() * Math.PI * 2;
            double distance = settings.SupplyCacheSpawnRadiusMin
                + (Random.Shared.NextDouble() * (settings.SupplyCacheSpawnRadiusMax - settings.SupplyCacheSpawnRadiusMin));
            double x = client.Character.X + (Math.Cos(angle) * distance);
            double z = client.Character.Z + (Math.Sin(angle) * distance);
            float y = client.Character.Y;

            if (!OverlapsCache(mapCaches, x, y, z, minSeparation: 1.0))
            {
                return (x, y, z);
            }
        }

        return (client.Character.X, client.Character.Y, client.Character.Z);
    }

    private static bool OverlapsCache(List<SupplyCache> mapCaches, double x, float y, double z, double minSeparation)
    {
        return mapCaches.Any(cache => Distance(x, y, z, cache.X, cache.Y, cache.Z) < minSeparation);
    }

    private static double Distance(double x1, float y1, double z1, double x2, float y2, double z2)
    {
        double dx = x1 - x2;
        double dy = y1 - y2;
        double dz = z1 - z2;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    private static ushort GetNextSlot(SupplyCache cache)
    {
        if (cache.Items.Count == 0)
        {
            return 0;
        }

        return (ushort)(cache.Items.Max(x => x.Slot) + 1);
    }

    private void RemoveCache(SupplyCache cache, DbConnection connection)
    {
        _server.Database.DeleteSupplyCache(cache.Id, connection);
        UnregisterCache(cache);
        Logger.Info($"SupplyCache removed: map={cache.MapId} cache={cache.Id}");
    }

    private List<SupplyCache> GetCachesForLayout(StageLayoutId layout) =>
        GetCachesForMap(layout.Id).Where(cache => MatchesLayout(cache, layout)).ToList();

    private static StageLayoutId ResolveActiveLayout(GameClient client)
    {
        if (client.InstanceLayoutId.Id != 0 && client.InstanceLayoutId.GroupId != 0)
        {
            return client.InstanceLayoutId;
        }

        if (client.Character.Stage.Id != 0 && client.Character.Stage.GroupId != 0)
        {
            return client.Character.Stage;
        }

        if (client.InstanceLayoutId.Id != 0)
        {
            return client.InstanceLayoutId;
        }

        return client.Character.Stage;
    }

    private static (double X, float Y, double Z) ResolveCharacterPosition(GameClient client) =>
        (client.Character.X, client.Character.Y, client.Character.Z);

    private static bool HasValidCharacterPosition(double x, float y, double z) =>
        Math.Abs(x) > 1.0 || Math.Abs(z) > 1.0;

    private double GetNearbySyncRadius()
    {
        GameServerSettings settings = _server.GameSettings.GameServerSettings;
        return Math.Max(settings.SupplyCacheMergeRadius * 4, DefaultNearbySyncRadius);
    }

    /// <summary>
    /// Layout-based cache selection for rezone and drop-set registration. Open-world caches use
    /// group_id 0 so they match every area subgroup on the map.
    /// </summary>
    private List<SupplyCache> GetCachesForLayoutSync(GameClient client, StageLayoutId layout)
    {
        AdoptClientLayout(client, layout);
        return GetCachesForLayout(layout);
    }

    /// <summary>
    /// Proximity filter for post-login walk-back when layout sync ran before position was known.
    /// </summary>
    private List<SupplyCache> GetCachesNearCharacter(GameClient client, StageLayoutId layout)
    {
        List<SupplyCache> mapCaches = GetCachesForMap(layout.Id);
        if (mapCaches.Count == 0 || StageManager.IsDungeon(layout.Id))
        {
            return StageManager.IsDungeon(layout.Id)
                ? mapCaches.Where(cache => MatchesLayout(cache, layout)).ToList()
                : mapCaches;
        }

        (double x, float y, double z) = ResolveCharacterPosition(client);
        if (!HasValidCharacterPosition(x, y, z))
        {
            return [];
        }

        double syncRadius = GetNearbySyncRadius();
        return mapCaches
            .Where(cache => Distance(x, y, z, cache.X, cache.Y, cache.Z) <= syncRadius)
            .ToList();
    }

    private void NormalizeOpenWorldLayout(SupplyCache cache, DbConnection connection)
    {
        if (StageManager.IsDungeon(cache.MapId) || (cache.GroupId == 0 && cache.LayerNo == 0))
        {
            return;
        }

        cache.GroupId = 0;
        cache.LayerNo = 0;
        _server.Database.UpdateSupplyCachePosition(cache, connection);
    }

    private List<SupplyCache> GetCachesForMap(uint mapId)
    {
        lock (_lock)
        {
            return _cachesByMap.TryGetValue(mapId, out List<SupplyCache>? caches)
                ? caches.Where(cache => !IsExpired(cache)).ToList()
                : [];
        }
    }

    private int GetCacheCount()
    {
        lock (_lock)
        {
            return _cachesById.Count;
        }
    }

    private bool IsExpired(SupplyCache cache)
    {
        uint lifetimeDays = _server.GameSettings.GameServerSettings.SupplyCacheLifetimeDays;
        if (lifetimeDays <= 0)
        {
            return false;
        }

        return DateTime.UtcNow >= cache.Created.AddDays(lifetimeDays);
    }

    private void RegisterCache(SupplyCache cache)
    {
        _cachesById[cache.Id] = cache;
        _cachesBySetId[cache.SetId] = cache;
        if (!_cachesByMap.TryGetValue(cache.MapId, out List<SupplyCache>? mapCaches))
        {
            mapCaches = [];
            _cachesByMap[cache.MapId] = mapCaches;
        }

        mapCaches.Add(cache);
    }

    private void UnregisterCache(SupplyCache cache)
    {
        _cachesById.Remove(cache.Id);
        _cachesBySetId.Remove(cache.SetId);
        if (_cachesByMap.TryGetValue(cache.MapId, out List<SupplyCache>? mapCaches))
        {
            mapCaches.RemoveAll(x => x.Id == cache.Id);
            if (mapCaches.Count == 0)
            {
                _cachesByMap.Remove(cache.MapId);
            }
        }
    }

    private void RegisterDropItems(
        GameClient client,
        SupplyCache cache,
        uint wireSetId,
        StageLayoutId layout,
        IEnumerable<ushort>? itemSlots = null)
    {
        HashSet<ushort>? slotFilter = itemSlots?.ToHashSet();
        List<InstancedGatheringItem> items = cache.Items
            .Where(entry => slotFilter == null || slotFilter.Contains(entry.Slot))
            .Select(entry => new InstancedGatheringItem
            {
                ItemId = (ItemId)entry.Item.ItemId,
                ItemNum = entry.Item.Num,
                Quality = entry.Item.PlusValue,
                IsHidden = false,
            })
            .ToList();

        client.InstanceDropItemManager.Assign(layout, wireSetId, items, force: true);
    }

    private void BroadcastPopDrop(
        SupplyCache cache,
        GameClient sourceClient,
        StageLayoutId layout,
        ushort itemSlot,
        S2CInstancePopDropItemNtc ntc)
    {
        foreach (GameClient client in _server.ClientLookup.GetAll())
        {
            if (client == sourceClient || client.Character == null || client.Character.Stage.Id != cache.MapId)
            {
                continue;
            }

            StageLayoutId viewerLayout = ResolveDropLayout(client, cache);
            uint viewerWireSetId = GetOrAllocateWireSetIdForCache(client, cache.Id);
            StageLayoutId popLayout = GetCachePopLayout(cache, viewerLayout);
            if (SupplyCacheDropTracker.IsPlayerWireSetId(viewerWireSetId))
            {
                client.SupplyCacheDropTracker.TrackPlayerDrop(viewerWireSetId, cache.Id, itemSlot);
                RegisterDropItems(client, cache, viewerWireSetId, viewerLayout, [itemSlot]);
            }
            else
            {
                RegisterDropItems(client, cache, viewerWireSetId, viewerLayout);
            }

            client.Send(CreatePopDropNtc(cache, popLayout, viewerWireSetId));
        }
    }

    private static StageLayoutId ResolveDropLayout(GameClient client, SupplyCache? cache = null)
    {
        StageLayoutId stage = client.Character.Stage;
        StageLayoutId instance = client.InstanceLayoutId;

        if (instance.Id != 0 && instance.GroupId != 0)
        {
            return instance;
        }

        if (stage.Id != 0 && stage.GroupId != 0)
        {
            return stage;
        }

        if (instance.Id == stage.Id && instance.GroupId != 0)
        {
            return instance;
        }

        if (cache != null && cache.MapId == stage.Id && cache.GroupId != 0)
        {
            return new StageLayoutId(cache.MapId, cache.LayerNo, cache.GroupId);
        }

        if (instance.Id == stage.Id && instance.Id != 0)
        {
            return instance;
        }

        return stage;
    }

    private static CDataDropItemSetInfo CreateDropSetInfo(SupplyCache cache, uint wireSetId)
    {
        wireSetId = SupplyCacheDropTracker.ToClientWireSetId(wireSetId);
        return new CDataDropItemSetInfo
        {
            Id = (byte)wireSetId,
            MdlType = 0,
            X = cache.X,
            Y = cache.Y,
            Z = cache.Z,
        };
    }

    private static bool TryGetWireSetIdForCache(GameClient client, long cacheId, out uint wireSetId)
    {
        uint? canonicalWireSetId = client.SupplyCacheDropTracker.GetCanonicalWireSetIdForCache(cacheId);
        if (canonicalWireSetId != null)
        {
            wireSetId = canonicalWireSetId.Value;
            return true;
        }

        wireSetId = 0;
        return false;
    }

    private void PrepareWorldCacheDrop(
        GameClient client,
        SupplyCache cache,
        StageLayoutId clientLayout,
        uint wireSetId)
    {
        client.SupplyCacheDropTracker.MarkPendingDropSetRegistration(wireSetId);
        RegisterDropItems(client, cache, wireSetId, clientLayout);
    }

    private void SendWorldCachePop(
        GameClient client,
        SupplyCache cache,
        StageLayoutId clientLayout,
        uint wireSetId,
        PacketQueue? queue = null)
    {
        StageLayoutId popLayout = GetCachePopLayout(cache, clientLayout);
        S2CInstancePopDropItemNtc ntc = CreatePopDropNtc(cache, popLayout, wireSetId);
        if (queue != null)
        {
            client.Enqueue(ntc, queue);
        }
        else
        {
            client.Send(ntc);
        }

        Logger.Info(
            $"SupplyCache zone sync: character={client.Character.CharacterId} map={cache.MapId} cache={cache.Id} popLayout={popLayout} clientLayout={clientLayout} wireSetId={wireSetId} pos=({cache.X:F1}, {cache.Y:F1}, {cache.Z:F1})");
    }

    private void PopWorldCache(
        GameClient client,
        SupplyCache cache,
        StageLayoutId clientLayout,
        uint wireSetId,
        PacketQueue? queue = null)
    {
        PrepareWorldCacheDrop(client, cache, clientLayout, wireSetId);
        SendWorldCachePop(client, cache, clientLayout, wireSetId, queue);
    }

    private static StageLayoutId GetCachePopLayout(SupplyCache cache, StageLayoutId clientLayout)
    {
        if (cache.GroupId != 0)
        {
            return cache.StageLayoutId;
        }

        if (clientLayout.GroupId != 0)
        {
            return clientLayout;
        }

        return cache.StageLayoutId;
    }

    private uint GetOrAllocateWireSetIdForCache(GameClient client, long cacheId)
    {
        if (TryGetWireSetIdForCache(client, cacheId, out uint wireSetId))
        {
            return wireSetId;
        }

        return AllocateUniquePersistedWireSetId(client, cacheId, "broadcast-viewer");
    }

    private static S2CInstancePopDropItemNtc CreatePopDropNtc(
        SupplyCache cache,
        StageLayoutId layout,
        uint setId,
        double? posX = null,
        float? posY = null,
        double? posZ = null)
    {
        return new S2CInstancePopDropItemNtc
        {
            LayoutId = layout.ToCDataStageLayoutId(),
            SetId = SupplyCacheDropTracker.ToClientWireSetId(setId),
            MdlType = 0,
            PosX = posX ?? cache.X,
            PosY = posY ?? cache.Y,
            PosZ = posZ ?? cache.Z,
        };
    }
}

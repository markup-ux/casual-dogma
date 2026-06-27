using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Characters;

/// <summary>
/// Tracks client-side drop set IDs for supply caches. The game client assigns its own
/// sequential setId when discarding items; server PopDropItemNtc must use the same value.
/// </summary>
public class SupplyCacheDropTracker
{
    private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupplyCacheDropTracker));

    private readonly Dictionary<uint, long> _wireSetIdToCacheId = new();
    private readonly Dictionary<uint, HashSet<ushort>> _wireSetIdToItemSlots = new();
    private readonly Queue<long> _pendingCacheIds = new();
    private readonly HashSet<uint> _consumedWireSetIds = new();
    private readonly Dictionary<long, StageLayoutId> _syncedCacheLayouts = new();
    private readonly HashSet<uint> _pendingDropSetRegistration = new();
    private readonly Dictionary<uint, long> _lastDroppedCacheIdByMap = new();
    private readonly Dictionary<uint, uint> _lastDroppedWireSetIdByMap = new();
    private uint _nextPlayerWireSetId = 1;
    private uint _nextPersistedWireSetId = PersistedWireSetIdMin;
    private DateTime? _proximitySyncPausedUntilUtc;
    private DateTime _lastProximitySyncUtc = DateTime.MinValue;
    private readonly HashSet<StageLayoutId> _completedProximityLayouts = new();

    public static readonly TimeSpan ProximitySyncPauseAfterPlayerDrop = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ProximitySyncMinInterval = TimeSpan.FromSeconds(8);

    public const uint PlayerWireSetIdMax = 127;
    public const uint PersistedWireSetIdMin = 128;

    public static bool IsPlayerWireSetId(uint wireSetId) =>
        wireSetId >= 1 && wireSetId <= PlayerWireSetIdMax;

    public static bool IsPersistedWireSetId(uint wireSetId) =>
        wireSetId >= PersistedWireSetIdMin && wireSetId < SupplyCache.SetIdBase;

    /// <summary>
    /// DropItemSetList and PopDrop only expose the low byte. World-cache ids in the 0x80000000
    /// range must be normalized before registration or the client cannot resolve loot requests.
    /// </summary>
    public static uint ToClientWireSetId(uint wireSetId)
    {
        if (wireSetId >= SupplyCache.SetIdBase)
        {
            wireSetId &= 0xFF;
        }

        return wireSetId == 0 ? 1 : wireSetId;
    }

    public static uint GetPersistedDropSetId(long cacheId) => ToClientWireSetId(SupplyCache.MakeSetId(cacheId));

    /// <summary>
    /// Assigns the next player-drop wire set id. The client increments its local drop id on
    /// each discard (1, 2, 3...), so the server must match that sequence rather than reuse
    /// the lowest free id after a bag is emptied.
    /// </summary>
    public uint AllocateWireSetId()
    {
        if (!_wireSetIdToCacheId.ContainsKey(1) || !HasTrackedItems(1))
        {
            _nextPlayerWireSetId = 1;
        }

        uint id = _nextPlayerWireSetId;
        _nextPlayerWireSetId = id >= PlayerWireSetIdMax ? 1 : id + 1;
        _consumedWireSetIds.Remove(id);
        return id;
    }

    public void RememberLastDrop(uint mapId, long cacheId, uint wireSetId)
    {
        _lastDroppedCacheIdByMap[mapId] = cacheId;
        _lastDroppedWireSetIdByMap[mapId] = wireSetId;
        PauseProximitySync(ProximitySyncPauseAfterPlayerDrop);
    }

    public void PauseProximitySync(TimeSpan duration)
    {
        DateTime until = DateTime.UtcNow.Add(duration);
        if (_proximitySyncPausedUntilUtc == null || until > _proximitySyncPausedUntilUtc)
        {
            _proximitySyncPausedUntilUtc = until;
        }
    }

    public bool IsProximitySyncPaused =>
        _proximitySyncPausedUntilUtc != null && DateTime.UtcNow < _proximitySyncPausedUntilUtc;

    public bool TryBeginProximitySyncBurst(bool force = false)
    {
        if (IsProximitySyncPaused)
        {
            return false;
        }

        if (force)
        {
            _lastProximitySyncUtc = DateTime.UtcNow;
            return true;
        }

        DateTime now = DateTime.UtcNow;
        if (_lastProximitySyncUtc != DateTime.MinValue
            && now - _lastProximitySyncUtc < ProximitySyncMinInterval)
        {
            return false;
        }

        _lastProximitySyncUtc = now;
        return true;
    }

    public long? TryGetLastDroppedCacheId(uint mapId) =>
        _lastDroppedCacheIdByMap.TryGetValue(mapId, out long cacheId) ? cacheId : null;

    public uint TryGetLastDroppedWireSetId(uint mapId) =>
        _lastDroppedWireSetIdByMap.TryGetValue(mapId, out uint wireSetId) ? wireSetId : 1;

    /// <summary>
    /// Wire set ids for persisted caches respawned on zone entry. Starts at 2 so id 1 stays
    /// reserved for the client's discard sequence.
    /// </summary>
    public uint AllocatePersistedWireSetId()
    {
        uint id = _nextPersistedWireSetId;
        _nextPersistedWireSetId = id >= byte.MaxValue ? PersistedWireSetIdMin : id + 1;
        if (_nextPersistedWireSetId <= PlayerWireSetIdMax)
        {
            _nextPersistedWireSetId = PersistedWireSetIdMin;
        }

        _consumedWireSetIds.Remove(id);
        return id;
    }

    public bool CanUsePlayerWireId1() =>
        _nextPlayerWireSetId == 1 && !_wireSetIdToCacheId.ContainsKey(1);

    public void ReservePlayerWireId1()
    {
        _nextPlayerWireSetId = 2;
    }

    /// <summary>
    /// Clears persisted PopDrop wire mappings so nearby caches can be re-assigned as the player moves.
    /// Keeps active player discard bags (tracked item slots on wire id 1..127).
    /// </summary>
    public void ClearPersistedPopMappings()
    {
        List<uint> staleWireSetIds = _wireSetIdToCacheId
            .Where(x => IsPersistedWireSetId(x.Key) && !HasTrackedItems(x.Key))
            .Select(x => x.Key)
            .ToList();

        foreach (uint wireSetId in staleWireSetIds)
        {
            _wireSetIdToCacheId.Remove(wireSetId);
            _wireSetIdToItemSlots.Remove(wireSetId);
        }

        _syncedCacheLayouts.Clear();
        _nextPersistedWireSetId = PersistedWireSetIdMin;
        if (!_wireSetIdToCacheId.ContainsKey(1))
        {
            _nextPlayerWireSetId = 1;
        }
    }

    public void ClearCacheSynced(long cacheId) => _syncedCacheLayouts.Remove(cacheId);

    public bool HasActivePlayerDrop(long cacheId) =>
        GetPlayerWireSetIdsForCache(cacheId).Any(wire => HasTrackedItems(wire));

    /// <summary>
    /// True while the player still has an unlooted discard bag (wire 1..127 with tracked slots).
    /// Proximity sync must not send GetDropItemSetListRes during this window — the client replaces
    /// its entire drop-set table and breaks interact on the active bag.
    /// </summary>
    public bool HasAnyActivePlayerDrop() =>
        _wireSetIdToItemSlots.Any(entry =>
            IsPlayerWireSetId(entry.Key) && entry.Value.Count > 0 && !IsConsumed(entry.Key));

    public bool IsLayoutProximitySyncComplete(StageLayoutId layout) =>
        _completedProximityLayouts.Contains(layout);

    public void MarkLayoutProximitySyncComplete(StageLayoutId layout) =>
        _completedProximityLayouts.Add(layout);

    public bool IsRecentPlayerDropCache(uint mapId, long cacheId) =>
        _lastDroppedCacheIdByMap.TryGetValue(mapId, out long lastCacheId) && lastCacheId == cacheId;

    public uint? GetCanonicalWireSetIdForCache(long cacheId)
    {
        List<uint> wires = GetWireSetIdsForCache(cacheId);
        if (wires.Count == 0)
        {
            return null;
        }

        uint? activePlayerWire = wires
            .Where(wire => IsPlayerWireSetId(wire) && HasTrackedItems(wire) && !IsConsumed(wire))
            .OrderBy(wire => wire)
            .Cast<uint?>()
            .FirstOrDefault();
        if (activePlayerWire != null)
        {
            return activePlayerWire;
        }

        uint? openPlayerWire = wires
            .Where(wire => IsPlayerWireSetId(wire) && !IsConsumed(wire))
            .OrderBy(wire => wire)
            .Cast<uint?>()
            .FirstOrDefault();
        if (openPlayerWire != null)
        {
            return openPlayerWire;
        }

        uint? openPersistedWire = wires
            .Where(wire => !IsConsumed(wire))
            .OrderBy(wire => wire)
            .Cast<uint?>()
            .FirstOrDefault();
        return openPersistedWire ?? wires.OrderBy(wire => wire).First();
    }

    public void RegisterDrop(uint wireSetId, long cacheId, string reason = "unspecified")
    {
        wireSetId = ToClientWireSetId(wireSetId);
        bool collision = _wireSetIdToCacheId.TryGetValue(wireSetId, out long existing) && existing != cacheId;
        if (collision)
        {
            Logger.Error(
                $"[SUPPLY_CACHE_WIRE] collision wire={wireSetId} wasCache={existing} nowCache={cacheId} reason={reason}");
            return;
        }

        _wireSetIdToCacheId[wireSetId] = cacheId;

        if (reason is "layout-sync" or "proximity-sync" or "player-drop" or "drop-set-list")
        {
            Logger.Info($"[SUPPLY_CACHE_WIRE] map wire={wireSetId} cache={cacheId} reason={reason}");
        }
        else
        {
            Logger.Debug($"[SUPPLY_CACHE_WIRE] map wire={wireSetId} cache={cacheId} reason={reason}");
        }
    }

    public void MarkPendingDropSetRegistration(uint wireSetId) =>
        _pendingDropSetRegistration.Add(ToClientWireSetId(wireSetId));

    public bool NeedsDropSetRegistration(uint wireSetId) =>
        _pendingDropSetRegistration.Contains(ToClientWireSetId(wireSetId));

    public void ClearDropSetRegistration(uint wireSetId) =>
        _pendingDropSetRegistration.Remove(ToClientWireSetId(wireSetId));

    public void ClearAllDropSetRegistration() => _pendingDropSetRegistration.Clear();

    public void TrackPlayerDrop(uint wireSetId, long cacheId, ushort itemSlot)
    {
        RegisterDrop(wireSetId, cacheId, "player-drop");
        if (!_wireSetIdToItemSlots.TryGetValue(wireSetId, out HashSet<ushort>? slots))
        {
            slots = new HashSet<ushort>();
            _wireSetIdToItemSlots[wireSetId] = slots;
        }

        slots.Add(itemSlot);
    }

    public bool TryGetTrackedItemSlots(uint wireSetId, out HashSet<ushort> slots) =>
        _wireSetIdToItemSlots.TryGetValue(wireSetId, out slots!) && slots.Count > 0;

    public bool HasTrackedItems(uint wireSetId) =>
        _wireSetIdToItemSlots.TryGetValue(wireSetId, out HashSet<ushort>? slots) && slots.Count > 0;

    public void PruneTrackedSlots(uint wireSetId, HashSet<ushort> remainingItemSlots)
    {
        if (!_wireSetIdToItemSlots.TryGetValue(wireSetId, out HashSet<ushort>? tracked))
        {
            return;
        }

        tracked.RemoveWhere(slot => !remainingItemSlots.Contains(slot));
        if (tracked.Count == 0)
        {
            _wireSetIdToItemSlots.Remove(wireSetId);
        }
    }

    public List<uint> GetWireSetIdsForCache(long cacheId) =>
        _wireSetIdToCacheId
            .Where(x => x.Value == cacheId)
            .Select(x => x.Key)
            .ToList();

    public List<uint> GetPlayerWireSetIdsForCache(long cacheId) =>
        _wireSetIdToCacheId
            .Where(x => x.Value == cacheId && IsPlayerWireSetId(x.Key))
            .Select(x => x.Key)
            .ToList();

    public void EnqueuePendingCache(long cacheId)
    {
        _pendingCacheIds.Enqueue(cacheId);
    }

    public bool TryResolveCacheId(uint setId, out long cacheId)
    {
        setId = ToClientWireSetId(setId);
        if (_wireSetIdToCacheId.TryGetValue(setId, out cacheId))
        {
            return true;
        }

        if (_pendingCacheIds.Count > 0)
        {
            cacheId = _pendingCacheIds.Dequeue();
            _wireSetIdToCacheId[setId] = cacheId;
            return true;
        }

        cacheId = 0;
        return false;
    }

    public bool TryGetCacheId(uint wireSetId, out long cacheId) =>
        _wireSetIdToCacheId.TryGetValue(ToClientWireSetId(wireSetId), out cacheId);

    public bool TryMarkCacheSynced(long cacheId, StageLayoutId layout)
    {
        if (_syncedCacheLayouts.TryGetValue(cacheId, out StageLayoutId prior)
            && prior.Id == layout.Id
            && prior.LayerNo == layout.LayerNo
            && prior.GroupId == layout.GroupId)
        {
            return false;
        }

        _syncedCacheLayouts[cacheId] = layout;
        return true;
    }

    private readonly HashSet<uint> _mapsWithEntryProximitySync = new();

    public bool TryMarkMapEntryProximitySync(uint mapId) => _mapsWithEntryProximitySync.Add(mapId);

    public bool IsConsumed(uint wireSetId) =>
        _consumedWireSetIds.Contains(ToClientWireSetId(wireSetId));

    public void MarkConsumed(uint wireSetId)
    {
        wireSetId = ToClientWireSetId(wireSetId);
        _consumedWireSetIds.Add(wireSetId);
        _wireSetIdToCacheId.Remove(wireSetId);
        _wireSetIdToItemSlots.Remove(wireSetId);
        if (!_wireSetIdToCacheId.ContainsKey(1))
        {
            _nextPlayerWireSetId = 1;
        }
    }

    public void ResetForStage(uint mapId)
    {
        _pendingCacheIds.Clear();
        _wireSetIdToCacheId.Clear();
        _wireSetIdToItemSlots.Clear();
        _consumedWireSetIds.Clear();
        _syncedCacheLayouts.Clear();
        _pendingDropSetRegistration.Clear();
        _mapsWithEntryProximitySync.Clear();
        _completedProximityLayouts.Clear();
        _proximitySyncPausedUntilUtc = null;
        _lastProximitySyncUtc = DateTime.MinValue;
        _nextPlayerWireSetId = 1;
        _nextPersistedWireSetId = PersistedWireSetIdMin;
    }

    public void RemoveMappingsForCache(long cacheId)
    {
        List<uint> stale = _wireSetIdToCacheId
            .Where(x => x.Value == cacheId)
            .Select(x => x.Key)
            .ToList();
        foreach (uint wireSetId in stale)
        {
            MarkConsumed(wireSetId);
        }
    }

    public IEnumerable<KeyValuePair<uint, long>> WireSetMappings => _wireSetIdToCacheId;

    public string FormatWireSnapshot() => FormatWireSnapshot(maxEntries: 0, maxLength: 0);

    public string FormatWireSnapshot(int maxEntries, int maxLength)
    {
        if (_wireSetIdToCacheId.Count == 0)
        {
            return "no wire mappings";
        }

        StringBuilder sb = new();
        int written = 0;
        foreach (KeyValuePair<uint, long> mapping in _wireSetIdToCacheId.OrderBy(x => x.Key))
        {
            if (maxEntries > 0 && written >= maxEntries)
            {
                sb.Append($"... +{_wireSetIdToCacheId.Count - written} more");
                break;
            }

            sb.Append(
                $"w{mapping.Key}->c{mapping.Value}" +
                $"(t={HasTrackedItems(mapping.Key)},p={NeedsDropSetRegistration(mapping.Key)},x={IsConsumed(mapping.Key)}); ");
            written++;
        }

        string snapshot = sb.ToString().TrimEnd();
        if (maxLength > 0 && snapshot.Length > maxLength)
        {
            return snapshot[..(maxLength - 3)] + "...";
        }

        return snapshot;
    }

    public int CountWireCollisionsByLowByte()
    {
        return _wireSetIdToCacheId
            .GroupBy(x => x.Key)
            .Count(g => g.Select(x => x.Value).Distinct().Count() > 1);
    }

    /// <summary>
    /// Wire set id for a persisted world cache. Uses the high-bit set id range so player
    /// drops can keep using low sequential ids (1, 2, 3...) that match the client.
    /// </summary>
    public static uint GetWorldCacheWireSetId(long cacheId) => SupplyCache.MakeSetId(cacheId);

    public void Reset()
    {
        _wireSetIdToCacheId.Clear();
        _wireSetIdToItemSlots.Clear();
        _pendingCacheIds.Clear();
        _consumedWireSetIds.Clear();
        _syncedCacheLayouts.Clear();
        _pendingDropSetRegistration.Clear();
        _nextPlayerWireSetId = 1;
        _nextPersistedWireSetId = PersistedWireSetIdMin;
    }
}

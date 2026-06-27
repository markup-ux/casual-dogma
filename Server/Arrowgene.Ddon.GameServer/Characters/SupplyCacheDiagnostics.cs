using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Characters;

public enum SupplyCacheDropListOutcome
{
    SupplyCacheList,
    EmptyConsumed,
    EmptyStaleCache,
    SupplyCacheMissing,
    EnemyAutoLoot,
    EnemyList,
}

public sealed class SupplyCacheDropRecord
{
    public DateTime UtcTime { get; init; }
    public StorageType StorageType { get; init; }
    public uint ItemId { get; init; }
    public uint ItemNum { get; init; }
    public StageLayoutId CharacterStage { get; init; }
    public StageLayoutId InstanceLayout { get; init; }
    public StageLayoutId ResolvedLayout { get; init; }
    public long CacheId { get; init; }
    public uint WireSetId { get; init; }
    public uint PopSetId { get; init; }
    public double PosX { get; init; }
    public float PosY { get; init; }
    public double PosZ { get; init; }
    public bool PopDropBuilt { get; init; }
    public bool PopDropSent { get; set; }
    public string? SkipReason { get; init; }
}

public sealed class SupplyCacheDropListRecord
{
    public DateTime UtcTime { get; init; }
    public uint SetId { get; init; }
    public StageLayoutId RequestLayout { get; init; }
    public SupplyCacheDropListOutcome Outcome { get; init; }
    public int ItemCount { get; init; }
    public long? CacheId { get; init; }
    public bool AutoLooted { get; init; }
    public string ResponsePath { get; init; } = string.Empty;
    public bool NeededRegistration { get; init; }
    public long? MappedCacheId { get; init; }
}

public sealed class SupplyCacheHealthCheck
{
    public string Name { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string Detail { get; init; } = string.Empty;
}

public sealed class SupplyCacheHealthReport
{
    public string Verdict { get; init; } = "INCONCLUSIVE";
    public List<SupplyCacheHealthCheck> Checks { get; init; } = [];
    public List<string> Hints { get; init; } = [];
}

/// <summary>
/// Per-session supply cache diagnostics for operators and log grep ([SUPPLY_CACHE_DIAG]).
/// </summary>
public class SupplyCacheSessionDiagnostics
{
    private const int MaxDropListRecords = 12;
    private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupplyCacheSessionDiagnostics));

    public SupplyCacheDropRecord? LastDrop { get; private set; }
    public IReadOnlyList<SupplyCacheDropListRecord> RecentDropLists => _recentDropLists;

    private readonly List<SupplyCacheDropListRecord> _recentDropLists = [];

    public void Clear()
    {
        LastDrop = null;
        _recentDropLists.Clear();
    }

    public void RecordDropAttempt(
        GameClient client,
        StorageType storageType,
        uint itemId,
        uint itemNum,
        StageLayoutId resolvedLayout,
        long cacheId,
        uint wireSetId,
        uint popSetId,
        S2CInstancePopDropItemNtc? dropNtc)
    {
        LastDrop = new SupplyCacheDropRecord
        {
            UtcTime = DateTime.UtcNow,
            StorageType = storageType,
            ItemId = itemId,
            ItemNum = itemNum,
            CharacterStage = client.Character.Stage,
            InstanceLayout = client.InstanceLayoutId,
            ResolvedLayout = resolvedLayout,
            CacheId = cacheId,
            WireSetId = wireSetId,
            PopSetId = popSetId,
            PosX = client.Character.X,
            PosY = client.Character.Y,
            PosZ = client.Character.Z,
            PopDropBuilt = dropNtc != null,
            PopDropSent = false,
        };
    }

    public void RecordDropSkipped(GameClient client, StorageType storageType, uint itemId, string reason)
    {
        LastDrop = new SupplyCacheDropRecord
        {
            UtcTime = DateTime.UtcNow,
            StorageType = storageType,
            ItemId = itemId,
            ItemNum = 0,
            CharacterStage = client.Character.Stage,
            InstanceLayout = client.InstanceLayoutId,
            ResolvedLayout = client.Character.Stage,
            SkipReason = reason,
            PopDropBuilt = false,
            PopDropSent = false,
        };
    }

    public void MarkPopDropSent()
    {
        if (LastDrop != null)
        {
            LastDrop.PopDropSent = true;
        }
    }

    public void RecordGetDropItemList(
        DdonGameServer server,
        GameClient client,
        uint setId,
        CDataStageLayoutId layout,
        SupplyCacheDropListOutcome outcome,
        int itemCount,
        long? cacheId,
        bool autoLooted,
        string responsePath,
        bool neededRegistration,
        long? mappedCacheId)
    {
        SupplyCacheDropListRecord record = new()
        {
            UtcTime = DateTime.UtcNow,
            SetId = setId,
            RequestLayout = layout.AsStageLayoutId(),
            Outcome = outcome,
            ItemCount = itemCount,
            CacheId = cacheId,
            AutoLooted = autoLooted,
            ResponsePath = responsePath,
            NeededRegistration = neededRegistration,
            MappedCacheId = mappedCacheId,
        };

        _recentDropLists.Add(record);

        if (_recentDropLists.Count > MaxDropListRecords)
        {
            _recentDropLists.RemoveAt(0);
        }

        LogGetDropItemList(server, client, record);
    }

    public SupplyCacheHealthReport BuildReport(DdonGameServer server, GameClient client)
    {
        List<SupplyCacheHealthCheck> checks = [];
        List<string> hints = [];

        bool enabled = server.GameSettings.GameServerSettings.SupplyCachesEnabled;
        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Server build",
            Passed = true,
            Detail = $"supply-cache build={SupplyCacheEventLog.BuildStamp}",
        });

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Feature enabled",
            Passed = enabled,
            Detail = enabled ? "SupplyCachesEnabled=true" : "SupplyCachesEnabled=false",
        });

        if (!enabled)
        {
            return new SupplyCacheHealthReport
            {
                Verdict = "DISABLED",
                Checks = checks,
                Hints = ["Enable SupplyCachesEnabled in server settings."],
            };
        }

        AppendWireDiagnostics(server, client, checks, hints);

        if (LastDrop == null)
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Discard observed",
                Passed = false,
                Detail = "No discard recorded this session. Drop an item, then run /scdiag again.",
            });

            SupplyCacheManager.SupplyCacheProximitySummary proximity = server.SupplyCacheManager.GetProximitySummary(client);
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Player position known",
                Passed = proximity.PositionKnown,
                Detail = proximity.PositionKnown
                    ? $"pos=({proximity.PosX:F1}, {proximity.PosY:F1}, {proximity.PosZ:F1})"
                    : "Position still (0,0,0) — walk a few steps after login, then retry",
            });

            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Persisted caches on map",
                Passed = proximity.MapCacheCount > 0,
                Detail = proximity.MapCacheCount > 0
                    ? $"map={proximity.MapId} total={proximity.MapCacheCount} nearby={proximity.NearbyCacheCount}"
                    : $"map={proximity.MapId} has no persisted supply caches",
            });

            if (proximity.MapCacheCount > 0 && proximity.PositionKnown && proximity.NearbyCacheCount == 0
                && proximity.NearestCacheId != null && proximity.NearestDistance != null)
            {
                hints.Add(
                    $"Nearest persisted cache is #{proximity.NearestCacheId} at {proximity.NearestDistance:F0}m — walk closer to respawn it after login.");
            }

            if (proximity.MapCacheCount > 0 && !proximity.PositionKnown)
            {
                hints.Add("After login, move until position is known; bags respawn when you walk back within ~200m.");
            }

            string noDropVerdict = proximity.MapCacheCount > 0 && proximity.NearbyCacheCount > 0
                ? "PERSISTED_NEARBY"
                : "INCONCLUSIVE";

            return new SupplyCacheHealthReport { Verdict = noDropVerdict, Checks = checks, Hints = hints };
        }

        if (LastDrop.SkipReason != null)
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Drop routed to supply cache",
                Passed = false,
                Detail = LastDrop.SkipReason,
            });
            return new SupplyCacheHealthReport { Verdict = "NOT_APPLICABLE", Checks = checks, Hints = hints };
        }

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "PopDrop setId matches client wire id",
            Passed = LastDrop.PopSetId == LastDrop.WireSetId,
            Detail = $"wireSetId={LastDrop.WireSetId} popSetId={LastDrop.PopSetId} cacheSetId=0x{SupplyCache.MakeSetId(LastDrop.CacheId):X8}",
        });

        if (LastDrop.PopSetId != LastDrop.WireSetId)
        {
            hints.Add("PopDrop SetId must equal the client's wire set id (1, 2, 3...), not the cache database id.");
        }

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "PopDrop notification built",
            Passed = LastDrop.PopDropBuilt,
            Detail = LastDrop.PopDropBuilt
                ? $"popSetId=0x{LastDrop.PopSetId:X8} wireSetId={LastDrop.WireSetId} cache={LastDrop.CacheId}"
                : "HandleDrop returned null",
        });

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "PopDrop sent to client",
            Passed = LastDrop.PopDropSent,
            Detail = LastDrop.PopDropSent ? "S2C_INSTANCE_POP_DROP_ITEM_NTC sent after discard" : "PopDrop was not sent",
        });

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Layout has area group",
            Passed = LastDrop.ResolvedLayout.GroupId != 0 || LastDrop.CharacterStage.Id is 135 or 2,
            Detail = $"stage={LastDrop.CharacterStage} instance={LastDrop.InstanceLayout} resolved={LastDrop.ResolvedLayout}",
        });

        if (LastDrop.ResolvedLayout.GroupId == 0 && LastDrop.CharacterStage.Id == 1)
        {
            hints.Add("Open-world drops need a non-zero GroupId. Walk until enemies load, then discard again.");
        }

        if (LastDrop.CharacterStage.Id == 135)
        {
            hints.Add("Arisen's Room (map 135) often fails to show bags. Test in Gran Soren field (map 1).");
        }

        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Player position known",
            Passed = LastDrop.PosX != 0 || LastDrop.PosY != 0 || LastDrop.PosZ != 0,
            Detail = $"pos=({LastDrop.PosX:F1}, {LastDrop.PosY:F1}, {LastDrop.PosZ:F1})",
        });

        List<SupplyCacheDropListRecord> relatedLists = _recentDropLists
            .Where(x => x.UtcTime >= LastDrop.UtcTime)
            .Where(x => x.SetId == LastDrop.PopSetId
                || x.SetId == LastDrop.WireSetId
                || x.SetId == SupplyCache.MakeSetId(LastDrop.CacheId)
                || x.CacheId == LastDrop.CacheId)
            .ToList();

        if (relatedLists.Count == 0)
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Client polled drop list",
                Passed = false,
                Detail = "No GetDropItemList for this bag yet (client may still be initializing)",
            });
        }
        else
        {
            SupplyCacheDropListRecord latest = relatedLists[^1];
            bool goodList = latest.Outcome == SupplyCacheDropListOutcome.SupplyCacheList && latest.ItemCount > 0;
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Client polled drop list",
                Passed = true,
                Detail = $"{relatedLists.Count} request(s); latest={latest.Outcome} items={latest.ItemCount}",
            });

            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Drop list exposes cache items",
                Passed = goodList,
                Detail = goodList
                    ? "GetDropItemList returned supply cache contents"
                    : $"Latest outcome={latest.Outcome}, items={latest.ItemCount}",
            });

            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Not auto-looted on list fetch",
                Passed = !latest.AutoLooted && latest.Outcome != SupplyCacheDropListOutcome.EnemyAutoLoot,
                Detail = latest.AutoLooted ? "Vanilla auto-loot path fired (bag will vanish)" : "Supply cache path used",
            });

            if (latest.Outcome is SupplyCacheDropListOutcome.EmptyConsumed or SupplyCacheDropListOutcome.EmptyStaleCache)
            {
                hints.Add("Server returned an empty drop list — client likely removed the bag icon.");
            }

            if (latest.Outcome == SupplyCacheDropListOutcome.EnemyAutoLoot)
            {
                hints.Add("Drop fell through to enemy auto-loot. Check setId/layout mapping in server logs.");
            }
        }

        int mapCaches = server.SupplyCacheManager.GetCacheCountForMap(LastDrop.ResolvedLayout.Id);
        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Caches on map",
            Passed = mapCaches > 0,
            Detail = $"{mapCaches} cache(s) on map {LastDrop.ResolvedLayout.Id}",
        });

        bool allCriticalPass = checks
            .Where(x => x.Name is
                "PopDrop setId matches client wire id" or
                "PopDrop notification built" or
                "PopDrop sent to client" or
                "Drop list exposes cache items" or
                "Not auto-looted on list fetch")
            .All(x => x.Passed);

        bool anyHardFail = checks.Any(x => !x.Passed && x.Name is
            "PopDrop setId matches client wire id" or
            "PopDrop notification built" or
            "PopDrop sent to client" or
            "Drop list exposes cache items" or
            "Not auto-looted on list fetch");

        bool clientPolled = relatedLists.Count > 0;
        string verdict = allCriticalPass && clientPolled ? "WORKING"
            : anyHardFail ? "BROKEN"
            : !clientPolled && LastDrop.PopDropSent ? "CLIENT_PENDING"
            : "INCONCLUSIVE";

        return new SupplyCacheHealthReport { Verdict = verdict, Checks = checks, Hints = hints };
    }

    public void LogDropEvent(DdonGameServer server, GameClient client, uint characterId, string eventName)
    {
        if (!server.GameSettings.GameServerSettings.SupplyCacheDiagnosticsEnabled || LastDrop == null)
        {
            return;
        }

        SupplyCacheHealthReport report = BuildReport(server, client);
        Logger.Info(
            $"[SUPPLY_CACHE_DIAG] event={eventName} verdict={report.Verdict} " +
            $"cha={characterId} cache={LastDrop.CacheId} popSetId=0x{LastDrop.PopSetId:X8} wireSetId={LastDrop.WireSetId} " +
            $"layout={LastDrop.ResolvedLayout} stage={LastDrop.CharacterStage} instance={LastDrop.InstanceLayout} " +
            $"popBuilt={LastDrop.PopDropBuilt} popSent={LastDrop.PopDropSent} skip={LastDrop.SkipReason ?? "-"} " +
            $"pos=({LastDrop.PosX:F1},{LastDrop.PosY:F1},{LastDrop.PosZ:F1})");
    }

    public void LogGetDropItemList(DdonGameServer server, GameClient client, SupplyCacheDropListRecord record)
    {
        if (!server.GameSettings.GameServerSettings.SupplyCacheDiagnosticsEnabled)
        {
            return;
        }

        SupplyCacheHealthReport report = BuildReport(server, client);
        Logger.Info(
            $"[SUPPLY_CACHE_DIAG] event=get_drop_list verdict={report.Verdict} " +
            $"setId=0x{record.SetId:X8} layout={record.RequestLayout} outcome={record.Outcome} " +
            $"items={record.ItemCount} cache={record.CacheId?.ToString() ?? "-"} autoLoot={record.AutoLooted} " +
            $"path={record.ResponsePath} needsReg={record.NeededRegistration} mappedCache={record.MappedCacheId?.ToString() ?? "-"}");
    }

    public const int MaxChatReportLength = 480;

    public static string FormatReportForChat(SupplyCacheHealthReport report, SupplyCacheDropRecord? lastDrop)
    {
        StringBuilder sb = new();
        sb.Append($"Supply cache diagnosis: {report.Verdict}");

        foreach (SupplyCacheHealthCheck check in report.Checks)
        {
            if (sb.Length >= MaxChatReportLength)
            {
                break;
            }

            sb.Append('\n');
            sb.Append(check.Passed ? "[OK] " : "[!!] ");
            sb.Append(check.Name);
            sb.Append(": ");
            AppendTruncatedDetail(sb, check.Detail, MaxChatReportLength);
        }

        foreach (string hint in report.Hints)
        {
            if (sb.Length >= MaxChatReportLength)
            {
                break;
            }

            sb.Append("\n> ");
            AppendTruncatedDetail(sb, hint, MaxChatReportLength);
        }

        if (lastDrop?.SkipReason == null && lastDrop != null && sb.Length < MaxChatReportLength)
        {
            sb.Append('\n');
            AppendTruncatedDetail(
                sb,
                $"Last drop: cache={lastDrop.CacheId} wire={lastDrop.WireSetId} at {lastDrop.UtcTime:HH:mm:ss} UTC",
                MaxChatReportLength);
        }

        if (sb.Length > MaxChatReportLength)
        {
            return sb.ToString(0, MaxChatReportLength - 3) + "...";
        }

        return sb.ToString();
    }

    private static void AppendTruncatedDetail(StringBuilder sb, string detail, int maxLength)
    {
        int remaining = maxLength - sb.Length;
        if (remaining <= 0)
        {
            return;
        }

        if (detail.Length <= remaining)
        {
            sb.Append(detail);
            return;
        }

        sb.Append(detail.AsSpan(0, Math.Max(0, remaining - 3)));
        sb.Append("...");
    }

    private void AppendWireDiagnostics(
        DdonGameServer server,
        GameClient client,
        List<SupplyCacheHealthCheck> checks,
        List<string> hints)
    {
        int wireCount = client.SupplyCacheDropTracker.WireSetMappings.Count();
        checks.Add(new SupplyCacheHealthCheck
        {
            Name = "Session wire mappings",
            Passed = wireCount > 0,
            Detail = wireCount > 0
                ? $"{wireCount} mapped: {client.SupplyCacheDropTracker.FormatWireSnapshot(maxEntries: 6, maxLength: 180)}"
                : "no wire mappings",
        });

        uint mapId = client.InstanceLayoutId.Id != 0 ? client.InstanceLayoutId.Id : client.Character.Stage.Id;
        List<string> legacyCollisions = server.SupplyCacheManager.DetectLegacyLowByteCollisions(mapId);
        if (legacyCollisions.Count > 0)
        {
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Legacy low-byte id collisions on map",
                Passed = false,
                Detail = string.Join("; ", legacyCollisions),
            });
            hints.Add(
                "Multiple persisted caches would share the same old low-byte wire id. Rebuild assigns unique session wires per cache.");
        }

        SupplyCacheDropListRecord? latest = _recentDropLists.LastOrDefault();
        if (latest != null)
        {
            bool ok = latest.Outcome == SupplyCacheDropListOutcome.SupplyCacheList && latest.ItemCount > 0;
            checks.Add(new SupplyCacheHealthCheck
            {
                Name = "Latest bag interact",
                Passed = ok,
                Detail =
                    $"setId={latest.SetId} path={latest.ResponsePath} needsReg={latest.NeededRegistration} " +
                    $"outcome={latest.Outcome} items={latest.ItemCount} cache={latest.CacheId?.ToString() ?? "-"}",
            });

            if (latest.ResponsePath == "direct" && latest.Outcome == SupplyCacheDropListOutcome.SupplyCacheList && latest.ItemCount > 0)
            {
                hints.Add("Latest interact used direct loot list (registration already completed).");
            }

            if (latest.ResponsePath == "register-then-followup")
            {
                hints.Add(
                    "Latest interact used register-then-followup (empty list first). Bag flicker on first click is expected.");
            }
        }
    }
}

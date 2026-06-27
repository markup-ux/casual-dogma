using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Logging;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Characters;

/// <summary>
/// Pure logic checks that run at server startup and in dotnet test. A failure here means the
/// supply-cache registration contract is broken before any player logs in.
/// </summary>
public static class SupplyCacheSelfTest
{
    private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupplyCacheSelfTest));

    public static bool LastRunPassed { get; private set; }
    public static IReadOnlyList<string> LastFailures { get; private set; } = [];

    public static (bool Passed, IReadOnlyList<string> Failures) RunCoreChecks()
    {
        List<string> failures = [];

        if (!GuardDetectsPartialRegistrationTable())
        {
            failures.Add("Registration guard must reject partial drop-set tables that omit active player wires.");
        }

        if (!GuardAcceptsFullRegistrationTable())
        {
            failures.Add("Registration guard must accept a complete drop-set table.");
        }

        if (!TrackerMarksPlayerDropTracked())
        {
            failures.Add("Player drop tracker must expose tracked item slots on wire id 1.");
        }

        if (!ClientWireIdNormalizationPreservesPlayerIds())
        {
            failures.Add("Client wire id normalization must preserve player ids 1..127.");
        }

        LastRunPassed = failures.Count == 0;
        LastFailures = failures;
        return (LastRunPassed, failures);
    }

    public static void RunCoreChecksAndLog()
    {
        (bool passed, IReadOnlyList<string> failures) = RunCoreChecks();
        if (passed)
        {
            Logger.Info("[SUPPLY_CACHE_VERIFY] startup self-test PASS (4/4 core checks)");
            SupplyCacheEventLog.Record(null, "self_test", "result=PASS checks=4");
            return;
        }

        Logger.Error($"[SUPPLY_CACHE_VERIFY] startup self-test FAIL: {string.Join(" | ", failures)}");
        SupplyCacheEventLog.Record(null, "self_test", $"result=FAIL detail={string.Join(';', failures)}");
    }

    private static bool GuardDetectsPartialRegistrationTable()
    {
        SupplyCacheDropTracker tracker = new();
        tracker.RegisterDrop(1, 71, "player-drop");
        tracker.TrackPlayerDrop(1, 71, itemSlot: 0);
        tracker.RegisterDrop(128, 47, "proximity-sync");

        SupplyCacheRegistrationValidation validation = SupplyCacheRegistrationGuard.ValidateTrackerCoverage(
            tracker,
            [128]);

        return !validation.Passed
            && validation.MissingWireIds.Count == 1
            && validation.MissingWireIds[0] == 1;
    }

    private static bool GuardAcceptsFullRegistrationTable()
    {
        SupplyCacheDropTracker tracker = new();
        tracker.RegisterDrop(1, 71, "player-drop");
        tracker.TrackPlayerDrop(1, 71, itemSlot: 0);
        tracker.RegisterDrop(128, 47, "proximity-sync");

        SupplyCacheRegistrationValidation validation = SupplyCacheRegistrationGuard.ValidateTrackerCoverage(
            tracker,
            [1, 128]);

        return validation.Passed;
    }

    private static bool TrackerMarksPlayerDropTracked()
    {
        SupplyCacheDropTracker tracker = new();
        tracker.TrackPlayerDrop(1, 10, itemSlot: 3);
        return tracker.HasTrackedItems(1)
            && tracker.HasActivePlayerDrop(10)
            && SupplyCacheDropTracker.IsPlayerWireSetId(1);
    }

    private static bool ClientWireIdNormalizationPreservesPlayerIds()
    {
        return SupplyCacheDropTracker.ToClientWireSetId(1) == 1
            && SupplyCacheDropTracker.ToClientWireSetId(127) == 127
            && SupplyCacheDropTracker.IsPlayerWireSetId(1)
            && SupplyCacheDropTracker.IsPersistedWireSetId(128);
    }

    public static bool CompareWireSetsDetectsMissingEntry()
    {
        List<CDataDropItemSetInfo> expected =
        [
            new() { Id = 1 },
            new() { Id = 128 },
        ];
        List<CDataDropItemSetInfo> partial = [new() { Id = 128 }];

        SupplyCacheRegistrationValidation validation =
            SupplyCacheRegistrationGuard.CompareWireSets(expected, partial);

        return !validation.Passed && validation.MissingWireIds.Contains(1u);
    }
}

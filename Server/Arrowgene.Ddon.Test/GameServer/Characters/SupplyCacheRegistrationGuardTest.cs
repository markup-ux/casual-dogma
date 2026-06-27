using System.Collections.Generic;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Xunit;

namespace Arrowgene.Ddon.Test.GameServer.Characters;

public class SupplyCacheRegistrationGuardTest
{
    [Fact]
    public void HasAnyActivePlayerDrop_BlocksWhileDiscardBagOpen()
    {
        SupplyCacheDropTracker tracker = new();
        Assert.False(tracker.HasAnyActivePlayerDrop());

        tracker.RegisterDrop(1, 71, "player-drop");
        tracker.TrackPlayerDrop(1, 71, itemSlot: 0);
        Assert.True(tracker.HasAnyActivePlayerDrop());
    }

    [Fact]
    public void ValidateTrackerCoverage_RejectsPartialTableMissingPlayerWire()
    {
        SupplyCacheDropTracker tracker = new();
        tracker.RegisterDrop(1, 71, "player-drop");
        tracker.TrackPlayerDrop(1, 71, itemSlot: 0);
        tracker.RegisterDrop(128, 47, "proximity-sync");

        SupplyCacheRegistrationValidation validation = SupplyCacheRegistrationGuard.ValidateTrackerCoverage(
            tracker,
            [128]);

        Assert.False(validation.Passed);
        Assert.Contains(1u, validation.MissingWireIds);
    }

    [Fact]
    public void ValidateTrackerCoverage_AcceptsCompleteTable()
    {
        SupplyCacheDropTracker tracker = new();
        tracker.RegisterDrop(1, 71, "player-drop");
        tracker.TrackPlayerDrop(1, 71, itemSlot: 0);
        tracker.RegisterDrop(128, 47, "proximity-sync");

        SupplyCacheRegistrationValidation validation = SupplyCacheRegistrationGuard.ValidateTrackerCoverage(
            tracker,
            [1, 128]);

        Assert.True(validation.Passed);
        Assert.Empty(validation.MissingWireIds);
    }

    [Fact]
    public void CompareWireSets_DetectsMissingDropSetEntry()
    {
        Assert.True(SupplyCacheSelfTest.CompareWireSetsDetectsMissingEntry());
    }

    [Fact]
    public void SelfTest_CoreChecksPass()
    {
        (bool passed, IReadOnlyList<string> failures) = SupplyCacheSelfTest.RunCoreChecks();
        Assert.True(passed, string.Join("; ", failures));
    }
}

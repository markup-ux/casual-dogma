using System;
using System.Collections.Generic;
using System.Linq;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.GameServer.Characters;

public sealed class SupplyCacheRegistrationValidation
{
    public bool Passed { get; init; }
    public IReadOnlyList<uint> ExpectedWireIds { get; init; } = [];
    public IReadOnlyList<uint> PayloadWireIds { get; init; } = [];
    public IReadOnlyList<uint> MissingWireIds { get; init; } = [];

    public string Summary =>
        Passed
            ? $"wires=[{string.Join(',', PayloadWireIds)}] complete"
            : $"missing=[{string.Join(',', MissingWireIds)}] payload=[{string.Join(',', PayloadWireIds)}] expected=[{string.Join(',', ExpectedWireIds)}]";
}

/// <summary>
/// Ensures GetDropItemSetListRes payloads include every active wire. The client replaces its
/// entire drop-set table on each response; omitting a wire unregisters that bag.
/// </summary>
public static class SupplyCacheRegistrationGuard
{
    public static SupplyCacheRegistrationValidation ValidateRegistrationList(
        SupplyCacheManager manager,
        GameClient client,
        StageLayoutId layout,
        IEnumerable<CDataDropItemSetInfo> dropSets)
    {
        List<CDataDropItemSetInfo> expected = manager.GetDropSetList(client, layout);
        return CompareWireSets(expected, dropSets);
    }

    public static SupplyCacheRegistrationValidation ValidateTrackerCoverage(
        SupplyCacheDropTracker tracker,
        IEnumerable<uint> payloadWireIds)
    {
        HashSet<uint> expected = tracker.WireSetMappings
            .Where(mapping => !tracker.IsConsumed(mapping.Key))
            .Select(mapping => SupplyCacheDropTracker.ToClientWireSetId(mapping.Key))
            .ToHashSet();

        HashSet<uint> payload = payloadWireIds
            .Select(SupplyCacheDropTracker.ToClientWireSetId)
            .ToHashSet();

        return BuildValidation(expected, payload);
    }

    /// <summary>
    /// Validates discard registration payloads that intentionally omit persisted proximity wires.
    /// </summary>
    public static SupplyCacheRegistrationValidation ValidatePlayerWireRegistration(
        SupplyCacheDropTracker tracker,
        IEnumerable<uint> payloadWireIds)
    {
        HashSet<uint> expected = tracker.WireSetMappings
            .Where(mapping =>
                SupplyCacheDropTracker.IsPlayerWireSetId(mapping.Key) && !tracker.IsConsumed(mapping.Key))
            .Select(mapping => SupplyCacheDropTracker.ToClientWireSetId(mapping.Key))
            .ToHashSet();

        HashSet<uint> payload = payloadWireIds
            .Select(SupplyCacheDropTracker.ToClientWireSetId)
            .ToHashSet();

        return BuildValidation(expected, payload);
    }

    internal static SupplyCacheRegistrationValidation CompareWireSets(
        IEnumerable<CDataDropItemSetInfo> expected,
        IEnumerable<CDataDropItemSetInfo> actual)
    {
        HashSet<uint> expectedIds = expected
            .Select(dropSet => (uint)dropSet.Id)
            .ToHashSet();
        HashSet<uint> actualIds = actual
            .Select(dropSet => (uint)dropSet.Id)
            .ToHashSet();
        return BuildValidation(expectedIds, actualIds);
    }

    private static SupplyCacheRegistrationValidation BuildValidation(
        HashSet<uint> expectedIds,
        HashSet<uint> payloadIds)
    {
        List<uint> missing = expectedIds
            .Where(id => !payloadIds.Contains(id))
            .OrderBy(id => id)
            .ToList();

        return new SupplyCacheRegistrationValidation
        {
            Passed = missing.Count == 0,
            ExpectedWireIds = expectedIds.OrderBy(id => id).ToList(),
            PayloadWireIds = payloadIds.OrderBy(id => id).ToList(),
            MissingWireIds = missing,
        };
    }
}

public sealed class SupplyCacheRegistrationAudit
{
    public sealed record Entry(
        DateTime UtcTime,
        string Path,
        StageLayoutId Layout,
        IReadOnlyList<uint> PayloadWireIds,
        IReadOnlyList<uint> ExpectedWireIds,
        IReadOnlyList<uint> MissingWireIds,
        bool Passed);

    private const int MaxEntries = 24;
    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public Entry? LastEntry => _entries.Count == 0 ? null : _entries[^1];

    public int FailCount => _entries.Count(entry => !entry.Passed);

    public void Record(
        string path,
        StageLayoutId layout,
        SupplyCacheRegistrationValidation validation)
    {
        _entries.Add(new Entry(
            DateTime.UtcNow,
            path,
            layout,
            validation.PayloadWireIds,
            validation.ExpectedWireIds,
            validation.MissingWireIds,
            validation.Passed));

        if (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(0);
        }
    }

    public void Clear() => _entries.Clear();
}

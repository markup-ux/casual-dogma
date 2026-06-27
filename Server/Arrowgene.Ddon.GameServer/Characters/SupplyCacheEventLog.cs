using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Characters;

/// <summary>
/// Structured supply-cache timeline for operators, /scdiag, and log grep ([SUPPLY_CACHE_EVENT]).
/// Also appends to Logs/supply_cache_events.log for post-session review.
/// </summary>
public static class SupplyCacheEventLog
{
    private const int MaxRingEntries = 128;
    private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupplyCacheEventLog));
    private static readonly ConcurrentQueue<string> RingBuffer = new();
    private static readonly object FileLock = new();
    private static string? _eventLogPath;

    public static string BuildStamp { get; } =
        typeof(SupplyCacheEventLog).Assembly.GetName().Version?.ToString() ?? "unknown";

    public static void ConfigureLogDirectory(string? logDirectory)
    {
        string directory = string.IsNullOrWhiteSpace(logDirectory)
            ? Path.Combine(Directory.GetCurrentDirectory(), "Logs")
            : logDirectory;
        _eventLogPath = Path.Combine(directory, "supply_cache_events.log");
    }

    public static void RecordServerStart(int cacheCount)
    {
        Record(null, "server_start", $"build={BuildStamp} caches={cacheCount}");
    }

    public static void Record(
        GameClient? client,
        string eventType,
        string details)
    {
        uint characterId = client?.Character?.CharacterId ?? 0;
        string line =
            $"[SUPPLY_CACHE_EVENT] ts={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z evt={eventType} cha={characterId} {details}";
        Logger.Info(line);
        AppendRing(line);
        AppendFile(line);
    }

    public static IReadOnlyList<string> GetRecentEvents(uint characterId, int maxEntries)
    {
        string prefix = $" cha={characterId} ";
        return RingBuffer
            .Where(line => characterId == 0 || line.Contains(prefix, StringComparison.Ordinal))
            .Reverse()
            .Take(maxEntries)
            .Reverse()
            .ToList();
    }

    public static IReadOnlyList<string> GetRecentEvents(int maxEntries) =>
        RingBuffer.Reverse().Take(maxEntries).Reverse().ToList();

    public static string WriteSessionSnapshot(
        DdonGameServer server,
        GameClient client,
        SupplyCacheHealthReport report,
        SupplyCacheDropRecord? lastDrop,
        int maxEvents = 24)
    {
        ConfigureLogDirectory(null);
        string directory = Path.GetDirectoryName(_eventLogPath!) ?? Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        Directory.CreateDirectory(directory);

        string path = Path.Combine(
            directory,
            $"supply_cache_snapshot_cha{client.Character.CharacterId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");

        StringBuilder sb = new();
        sb.AppendLine($"build={BuildStamp}");
        sb.AppendLine($"verdict={report.Verdict}");
        sb.AppendLine($"layout={client.InstanceLayoutId} stage={client.Character.Stage}");
        sb.AppendLine(
            $"pos=({client.Character.X:F1},{client.Character.Y:F1},{client.Character.Z:F1}) " +
            $"wires={client.SupplyCacheDropTracker.WireSetMappings.Count()}");
        sb.AppendLine($"wire_snapshot={client.SupplyCacheDropTracker.FormatWireSnapshot(maxEntries: 12, maxLength: 400)}");

        if (lastDrop != null)
        {
            sb.AppendLine(
                $"last_drop cache={lastDrop.CacheId} wire={lastDrop.WireSetId} pop={lastDrop.PopSetId} " +
                $"layout={lastDrop.ResolvedLayout} popSent={lastDrop.PopDropSent}");
        }

        sb.AppendLine("--- events ---");
        foreach (string line in GetRecentEvents(client.Character.CharacterId, maxEvents))
        {
            sb.AppendLine(line);
        }

        sb.AppendLine("--- checks ---");
        foreach (SupplyCacheHealthCheck check in report.Checks)
        {
            sb.AppendLine($"{(check.Passed ? "OK" : "!!")} {check.Name}: {check.Detail}");
        }

        File.WriteAllText(path, sb.ToString());
        Record(client, "snapshot", $"path={path.Replace('\\', '/')}");
        return path;
    }

    private static void AppendRing(string line)
    {
        RingBuffer.Enqueue(line);
        while (RingBuffer.Count > MaxRingEntries && RingBuffer.TryDequeue(out _))
        {
        }
    }

    private static void AppendFile(string line)
    {
        try
        {
            ConfigureLogDirectory(null);
            lock (FileLock)
            {
                File.AppendAllText(_eventLogPath!, line + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"SupplyCacheEventLog file append failed: {ex.Message}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.Cli
{
    /// <summary>
    /// Centralized error logging and detection.
    ///
    /// Every log that flows through <see cref="LogProvider"/> is offered to <see cref="Inspect"/>.
    /// Entries at <see cref="LogLevel.Error"/> are:
    ///   - persisted to a dedicated daily error log file (errors-YYYY-MM-DD.log.txt), and
    ///   - aggregated in-memory by a normalized signature so recurring problems are easy to spot.
    ///
    /// The aggregated data is surfaced through the `errors` CLI command and the session summary
    /// printed on shutdown.
    /// </summary>
    public class ErrorMonitor
    {
        public class ErrorGroup
        {
            public string Signature { get; init; }
            public long Count { get; set; }
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public string Sample { get; set; }
        }

        // Collapses per-occurrence tokens (object/generation ids, instance numbers, ports,
        // ip addresses, hex values and bare numbers) so the same logical error groups together
        // regardless of the specific connection/values involved.
        private static readonly Regex NoiseRegex = new(
            @"\[Id:\d+\]|\[Gen:\d+\]|#\d+|\b\d{1,3}(\.\d{1,3}){3}\b|0x[0-9a-fA-F]+|:\d+\b|\b\d+\b",
            RegexOptions.Compiled);

        private const int MaxRecent = 50;
        private const int MaxSampleLength = 2000;

        private readonly object _lock = new();
        private readonly Dictionary<string, ErrorGroup> _groups = new();
        private readonly LinkedList<string> _recent = new();

        private long _totalErrors;
        private DateTime _startedUtc = DateTime.UtcNow;
        private DirectoryInfo _logDir;

        public long TotalErrors
        {
            get
            {
                lock (_lock)
                {
                    return _totalErrors;
                }
            }
        }

        public void SetLogDirectory(DirectoryInfo logDir)
        {
            lock (_lock)
            {
                _logDir = logDir;
            }
        }

        /// <summary>
        /// Offer a log entry to the monitor. Only entries at <see cref="LogLevel.Error"/> are
        /// recorded; everything else is ignored. Safe to call from any thread.
        /// </summary>
        public void Inspect(Log log)
        {
            if (log.LogLevel != LogLevel.Error)
            {
                return;
            }

            string text = log.ToString();
            if (string.IsNullOrEmpty(text))
            {
                text = log.LoggerIdentity ?? "Unknown error";
            }

            string signature = Normalize(text);

            lock (_lock)
            {
                _totalErrors++;

                if (!_groups.TryGetValue(signature, out ErrorGroup group))
                {
                    group = new ErrorGroup
                    {
                        Signature = signature,
                        FirstSeen = log.DateTime,
                        Sample = Truncate(text, MaxSampleLength)
                    };
                    _groups.Add(signature, group);
                }

                group.Count++;
                group.LastSeen = log.DateTime;

                _recent.AddLast($"[{log.DateTime:yyyy-MM-dd HH:mm:ss}] {FirstLine(text)}");
                while (_recent.Count > MaxRecent)
                {
                    _recent.RemoveFirst();
                }

                WriteToFile(log, text);
            }
        }

        private void WriteToFile(Log log, string text)
        {
            if (_logDir == null)
            {
                return;
            }

            try
            {
                string filePath = Path.Combine(_logDir.FullName, $"errors-{log.DateTime:yyyy-MM-dd}.log.txt");
                using StreamWriter sw = new(filePath, append: true);
                sw.WriteLine(text);
            }
            catch
            {
                // Never let error logging throw and cascade into yet more errors.
            }
        }

        /// <summary>
        /// Aggregated report of all errors detected since monitoring started (or last reset).
        /// </summary>
        public string BuildReport()
        {
            lock (_lock)
            {
                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine("==================== Error Report ====================");
                sb.AppendLine($"Monitoring since (UTC): {_startedUtc:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Total errors:           {_totalErrors}");
                sb.AppendLine($"Unique error types:     {_groups.Count}");

                if (_groups.Count == 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("No errors detected.");
                    sb.AppendLine("======================================================");
                    return sb.ToString();
                }

                sb.AppendLine();
                sb.AppendLine("Top errors by occurrence:");
                int rank = 1;
                foreach (ErrorGroup g in _groups.Values.OrderByDescending(g => g.Count).Take(20))
                {
                    sb.AppendLine();
                    sb.AppendLine($"  #{rank++}  x{g.Count}  (first {g.FirstSeen:HH:mm:ss}, last {g.LastSeen:HH:mm:ss})");
                    sb.AppendLine($"      {FirstLine(g.Sample)}");
                }

                sb.AppendLine();
                sb.AppendLine("======================================================");
                return sb.ToString();
            }
        }

        /// <summary>
        /// The most recent individual errors, newest last.
        /// </summary>
        public string BuildRecent()
        {
            lock (_lock)
            {
                if (_recent.Count == 0)
                {
                    return "No errors detected this session.";
                }

                StringBuilder sb = new();
                sb.AppendLine();
                sb.AppendLine($"Last {_recent.Count} error(s):");
                foreach (string entry in _recent)
                {
                    sb.AppendLine($"  {entry}");
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// A one-paragraph summary suitable for printing on shutdown.
        /// </summary>
        public string BuildSessionSummary()
        {
            lock (_lock)
            {
                if (_totalErrors == 0)
                {
                    return "Session error summary: 0 errors detected.";
                }

                ErrorGroup top = _groups.Values.OrderByDescending(g => g.Count).First();
                return $"Session error summary: {_totalErrors} error(s) across {_groups.Count} type(s). " +
                       $"Most frequent (x{top.Count}): {FirstLine(top.Sample)}. " +
                       "See the errors-*.log.txt file or run the `errors` command for details.";
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _groups.Clear();
                _recent.Clear();
                _totalErrors = 0;
                _startedUtc = DateTime.UtcNow;
            }
        }

        private static string Normalize(string text)
        {
            string firstLine = FirstLine(text);
            return NoiseRegex.Replace(firstLine, "#").Trim();
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int idx = text.IndexOfAny(new[] { '\r', '\n' });
            return idx >= 0 ? text.Substring(0, idx) : text;
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
            {
                return text;
            }

            return text.Substring(0, max) + "...";
        }
    }
}

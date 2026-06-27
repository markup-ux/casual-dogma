using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher;

/// <summary>
/// One entry in the remote patch manifest.
/// </summary>
public sealed class PatchFile
{
    /// <summary>Path relative to the install directory, e.g. "DDO.exe" or "nativePC/foo.arc".</summary>
    public string Path { get; set; } = "";

    /// <summary>Expected size in bytes (used for a cheap first-pass check and progress totals).</summary>
    public long Size { get; set; }

    /// <summary>Lowercase hex SHA-256 of the file. Optional but strongly recommended.</summary>
    public string Sha256 { get; set; } = "";

    /// <summary>Optional absolute URL override. If empty, BaseUrl + Path is used.</summary>
    public string Url { get; set; } = "";
}

/// <summary>
/// Remote manifest describing the full set of files required to play.
/// </summary>
public sealed class PatchManifest
{
    public string Version { get; set; } = "";

    /// <summary>Base URL that relative file paths are appended to (should end with '/').</summary>
    public string BaseUrl { get; set; } = "";

    public List<PatchFile> Files { get; set; } = new();
}

/// <summary>The set of files that need to be (re)downloaded plus useful totals.</summary>
public sealed class PatchPlan
{
    public PatchManifest Manifest { get; } 
    public List<PatchFile> ToDownload { get; } = new();
    public long TotalBytes { get; private set; }

    public bool UpToDate => ToDownload.Count == 0;
    public string Version => Manifest.Version;

    public PatchPlan(PatchManifest manifest) => Manifest = manifest;

    internal void Add(PatchFile file)
    {
        ToDownload.Add(file);
        TotalBytes += Math.Max(0, file.Size);
    }
}

/// <summary>Progress snapshot reported during a download.</summary>
public readonly record struct PatchProgress(
    int FileIndex,
    int FileCount,
    string CurrentFile,
    long DownloadedBytes,
    long TotalBytes)
{
    public double Fraction => TotalBytes > 0 ? Math.Clamp((double)DownloadedBytes / TotalBytes, 0, 1) : 0;
    public int Percent => (int)Math.Round(Fraction * 100);
}

public sealed class Patcher
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly HttpClient _http;

    public Patcher(HttpClient http) => _http = http;

    public async Task<PatchManifest> FetchManifestAsync(string manifestUrl, CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync(manifestUrl, ct);
        var manifest = JsonSerializer.Deserialize<PatchManifest>(json, JsonOpts)
                       ?? throw new InvalidDataException("Manifest was empty or invalid.");
        return manifest;
    }

    /// <summary>
    /// Compares the manifest against the install directory and returns which files
    /// need downloading. A file is flagged when it is missing, the wrong size, or
    /// (when a hash is provided) fails SHA-256 verification.
    /// </summary>
    public async Task<PatchPlan> CheckAsync(PatchManifest manifest, string installDir,
        bool verifyHashes = true, IProgress<string>? scanStatus = null, CancellationToken ct = default)
    {
        var plan = new PatchPlan(manifest);

        foreach (var file in manifest.Files)
        {
            ct.ThrowIfCancellationRequested();
            string full = System.IO.Path.Combine(installDir, NormalizeRelative(file.Path));

            if (!File.Exists(full))
            {
                plan.Add(file);
                continue;
            }

            var info = new FileInfo(full);
            if (file.Size > 0 && info.Length != file.Size)
            {
                plan.Add(file);
                continue;
            }

            if (verifyHashes && !string.IsNullOrWhiteSpace(file.Sha256))
            {
                scanStatus?.Report($"Verifying {file.Path}");
                string actual = await ComputeSha256Async(full, ct);
                if (!actual.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    plan.Add(file);
            }
        }

        return plan;
    }

    /// <summary>
    /// Downloads every file in the plan to the install directory, verifying hashes,
    /// writing to a temporary file first and moving into place atomically.
    /// </summary>
    public async Task DownloadAsync(PatchPlan plan, string installDir, string baseUrl,
        IProgress<PatchProgress>? progress = null, CancellationToken ct = default)
    {
        long total = plan.TotalBytes;
        long doneOverall = 0;
        int index = 0;

        foreach (var file in plan.ToDownload)
        {
            ct.ThrowIfCancellationRequested();
            index++;

            string url = !string.IsNullOrWhiteSpace(file.Url)
                ? file.Url
                : CombineUrl(baseUrl, file.Path);

            string dest = System.IO.Path.Combine(installDir, NormalizeRelative(file.Path));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!);
            string tmp = dest + ".part";

            using (var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                resp.EnsureSuccessStatusCode();

                await using var src = await resp.Content.ReadAsStreamAsync(ct);
                await using var dst = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, true);

                var buffer = new byte[1 << 20];
                int read;
                long doneFile = 0;
                while ((read = await src.ReadAsync(buffer, ct)) > 0)
                {
                    await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                    doneFile += read;
                    doneOverall += read;
                    progress?.Report(new PatchProgress(index, plan.ToDownload.Count, file.Path, doneOverall, total));
                }
            }

            if (!string.IsNullOrWhiteSpace(file.Sha256))
            {
                string actual = await ComputeSha256Async(tmp, ct);
                if (!actual.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    TryDelete(tmp);
                    throw new InvalidDataException($"Checksum mismatch for {file.Path}.");
                }
            }

            if (File.Exists(dest)) File.Delete(dest);
            File.Move(tmp, dest);
        }
    }

    public static async Task<string> ComputeSha256Async(string path, CancellationToken ct = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, true);
        using var sha = SHA256.Create();
        byte[] hash = await sha.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeRelative(string path) =>
        path.Replace('/', System.IO.Path.DirectorySeparatorChar).TrimStart(System.IO.Path.DirectorySeparatorChar);

    private static string CombineUrl(string baseUrl, string relative)
    {
        if (string.IsNullOrEmpty(baseUrl)) return relative;
        return baseUrl.TrimEnd('/') + "/" + relative.Replace('\\', '/').TrimStart('/');
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }
}

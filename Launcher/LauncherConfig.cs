using System.IO;
using System.Text.Json;

namespace Launcher;

public sealed class LauncherConfig
{
    public string ServerName { get; set; } = "Casual Dogma";
    public string ClientPath { get; set; } = @"D:\DDON\Client\Dragon's Dogma Online\DDO.exe";
    public string StatusUrl { get; set; } = "http://127.0.0.1:52099/rpc/status";
    public string AccountApiUrl { get; set; } = "http://127.0.0.1:52099/api/account";
    public string LoginAddress { get; set; } = "127.0.0.1";
    public int LoginPort { get; set; } = 52100;
    public string DownloadUrl { get; set; } = "http://127.0.0.1:52099/win/";
    public string Token { get; set; } = "00000000000000000000";
    public string ClientVersion { get; set; } = "03.04.003.20181115.0";
    public string RemoteVersion { get; set; } = "3040008";
    public int RefreshSeconds { get; set; } = 10;

    // Display / window settings written into the client's config.ini before launch.
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 720;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;

    // Remembered session (login token is valid for 7 days server-side).
    public string AccountName { get; set; } = "";
    public string LoginToken { get; set; } = "";

    // When true, the launcher refuses to start the game unless the level-sync applier
    // scheduled task is installed locally. Keeps the client patch in place for fairness.
    public bool RequireLevelSyncApplier { get; set; } = true;

    // Optional explicit path to the level-sync applier's setup.cmd. Leave empty to let the
    // launcher auto-discover it next to the launcher (e.g. .\applier\setup.cmd).
    public string LevelSyncSetupPath { get; set; } = "";

    // "Remember me": when enabled, the password is stored DPAPI-encrypted so the
    // launcher can re-authenticate (e.g. after the login token expires).
    public bool RememberLogin { get; set; } = false;
    public string SavedPassword { get; set; } = "";

    // --- Patcher / installer ---
    // Leave PatchManifestUrl empty to disable updating entirely (the launcher
    // then assumes the client is already installed locally). Set it to a hosted
    // manifest.json when the server goes public.
    public string PatchManifestUrl { get; set; } = "";
    // Where game files are installed. Empty => use the folder of ClientPath.
    public string InstallPath { get; set; } = "";
    // The manifest version currently installed locally.
    public string InstalledVersion { get; set; } = "";

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static string ConfigPath =>
        Path.Combine(AppContext.BaseDirectory, "launcher.config.json");

    public static LauncherConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<LauncherConfig>(json, Opts) ?? new LauncherConfig();
            }
        }
        catch
        {
            // Fall back to defaults on any parse error.
        }

        // First run (or unreadable): seed a config file from defaults.
        var fresh = new LauncherConfig();
        fresh.Save();
        return fresh;
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, Opts));
        }
        catch
        {
            // Non-fatal: launcher still works with in-memory config.
        }
    }
}

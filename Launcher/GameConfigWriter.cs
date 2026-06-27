using System.IO;

namespace Launcher;

/// <summary>
/// Writes display settings into the MT Framework client's config.ini.
/// The game can rewrite config.ini on exit, so the launcher re-applies the
/// chosen values right before each launch.
/// </summary>
public static class GameConfigWriter
{
    public static string ApplyDisplaySettings(string clientExePath, int width, int height, bool fullscreen, bool vsync)
    {
        string? dir = Path.GetDirectoryName(clientExePath);
        if (string.IsNullOrEmpty(dir)) return "";

        string iniPath = Path.Combine(dir, "config.ini");

        // If the file was marked read-only (a common DDON tip), clear it so we can write.
        if (File.Exists(iniPath))
        {
            var attr = File.GetAttributes(iniPath);
            if (attr.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(iniPath, attr & ~FileAttributes.ReadOnly);
        }

        var ini = new IniFile(iniPath);
        ini.Set("DISPLAY", "VSYNC", vsync ? "ON" : "OFF");
        ini.Set("DISPLAY", "FullScreen", fullscreen ? "ON" : "OFF");
        ini.Set("DISPLAY", "Width", width.ToString());
        ini.Set("DISPLAY", "Height", height.ToString());
        ini.Save();
        return iniPath;
    }
}

/// <summary>Minimal INI editor that preserves existing lines, comments and ordering.</summary>
public sealed class IniFile
{
    private readonly string _path;
    private readonly List<string> _lines;

    public IniFile(string path)
    {
        _path = path;
        _lines = File.Exists(path) ? new List<string>(File.ReadAllLines(path)) : new List<string>();
    }

    public void Set(string section, string key, string value)
    {
        int secStart = -1;
        int secEnd = _lines.Count;

        for (int i = 0; i < _lines.Count; i++)
        {
            string t = _lines[i].Trim();
            if (t.StartsWith("[") && t.EndsWith("]"))
            {
                string name = t.Substring(1, t.Length - 2).Trim();
                if (secStart == -1 && string.Equals(name, section, StringComparison.OrdinalIgnoreCase))
                {
                    secStart = i;
                }
                else if (secStart != -1)
                {
                    secEnd = i;
                    break;
                }
            }
        }

        if (secStart == -1)
        {
            if (_lines.Count > 0 && _lines[^1].Trim().Length > 0) _lines.Add("");
            _lines.Add($"[{section}]");
            _lines.Add($"{key}={value}");
            return;
        }

        for (int i = secStart + 1; i < secEnd; i++)
        {
            string line = _lines[i];
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith(";") || trimmed.StartsWith("#")) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string k = line.Substring(0, eq).Trim();
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
            {
                _lines[i] = $"{key}={value}";
                return;
            }
        }

        int insert = secEnd;
        while (insert - 1 > secStart && _lines[insert - 1].Trim().Length == 0) insert--;
        _lines.Insert(insert, $"{key}={value}");
    }

    public void Save() => File.WriteAllLines(_path, _lines);
}

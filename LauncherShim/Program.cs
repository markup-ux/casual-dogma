using System.Diagnostics;
using System.Threading;

// This stand-in replaces the client's bundled DDO_Launcher.exe (which pointed at
// a different private server). The game runs it on "exit to launcher".
//
// Behaviour:
//   1. If the Casual Dogma launcher is already running, signal it to come to the
//      foreground and exit (no duplicate window).
//   2. Otherwise, start the Casual Dogma launcher.

const string ShowEventName = @"Local\CasualDogmaLauncher.Show";

try
{
    if (EventWaitHandle.TryOpenExisting(ShowEventName, out var ev))
    {
        ev.Set();
        ev.Dispose();
        return;
    }
}
catch
{
    // Fall through to a cold start.
}

string exe = ResolveLauncherPath();
if (File.Exists(exe))
{
    Process.Start(new ProcessStartInfo
    {
        FileName = exe,
        UseShellExecute = true,
        WorkingDirectory = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory
    });
}

static string ResolveLauncherPath()
{
    // Allow overriding the launcher location via a sibling pointer file.
    string dir = AppContext.BaseDirectory;
    string ptr = Path.Combine(dir, "casualdogma.launcher.txt");
    if (File.Exists(ptr))
    {
        string p = File.ReadAllText(ptr).Trim();
        if (!string.IsNullOrEmpty(p)) return p;
    }
    return @"D:\DDON\Launcher\bin\Release\net10.0-windows\DDON Launcher.exe";
}

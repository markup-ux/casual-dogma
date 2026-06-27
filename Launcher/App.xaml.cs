using System.Threading;
using System.Windows;

namespace Launcher;

/// <summary>
/// Interaction logic for App.xaml.
///
/// Enforces a single running instance. When a second copy starts (for example
/// the game's "exit to launcher", which relaunches the bundled launcher), it
/// signals the already-running launcher to come to the foreground and then
/// exits, instead of opening a duplicate window.
/// </summary>
public partial class App : Application
{
    private const string MutexName = @"Local\CasualDogmaLauncher.Instance";
    private const string ShowEventName = @"Local\CasualDogmaLauncher.Show";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _showEvent;
    private Thread? _showListener;
    private bool _running;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Another launcher is already running: tell it to show, then quit.
            try
            {
                if (EventWaitHandle.TryOpenExisting(ShowEventName, out var existing))
                {
                    existing.Set();
                    existing.Dispose();
                }
            }
            catch { /* best effort */ }

            Shutdown(0);
            return;
        }

        _running = true;
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        _showListener = new Thread(ShowListenerLoop) { IsBackground = true, Name = "ShowListener" };
        _showListener.Start();

        base.OnStartup(e);
    }

    private void ShowListenerLoop()
    {
        while (_running)
        {
            try
            {
                if (_showEvent is null) return;
                if (!_showEvent.WaitOne(1000)) continue;
                if (!_running) return;

                Dispatcher.Invoke(() =>
                {
                    if (MainWindow is null) return;
                    if (MainWindow.WindowState == WindowState.Minimized)
                        MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Show();
                    MainWindow.Activate();
                    MainWindow.Topmost = true;
                    MainWindow.Topmost = false;
                });
            }
            catch
            {
                return;
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _running = false;
        try { _showEvent?.Set(); } catch { }
        try { _showEvent?.Dispose(); } catch { }
        try { _instanceMutex?.ReleaseMutex(); } catch { }
        try { _instanceMutex?.Dispose(); } catch { }
        base.OnExit(e);
    }
}

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Launcher;

public partial class MainWindow : Window
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    // Separate client for downloads: no overall timeout (cancellation handles aborts).
    private static readonly HttpClient PatchHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly DispatcherTimer _timer = new();
    private LauncherConfig _config = new();
    private bool _polling;

    private bool _createMode;
    private bool _signedIn;
    private string _token = "";

    // Server status, tracked so the Play button can be recomputed centrally.
    private bool _serverOnline;
    private bool _preventLogin;

    // Patcher state.
    private enum PatchState { Disabled, Checking, UpdateAvailable, Downloading, Ready, Error }
    private readonly Patcher _patcher = new(PatchHttp);
    private PatchState _patchState = PatchState.Disabled;
    private PatchPlan? _plan;
    private CancellationTokenSource? _patchCts;

    private string InstallDir => string.IsNullOrWhiteSpace(_config.InstallPath)
        ? (Path.GetDirectoryName(_config.ClientPath) ?? AppContext.BaseDirectory)
        : _config.InstallPath;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _config = LauncherConfig.Load();
        ServerNameText.Text = _config.ServerName;

        // Restore a remembered session (server tokens last 7 days).
        if (_config.RememberLogin && !string.IsNullOrWhiteSpace(_config.LoginToken) && !string.IsNullOrWhiteSpace(_config.AccountName))
            SetSignedIn(_config.AccountName, _config.LoginToken);
        else
            SetSignedOut();

        AccountInput.Text = _config.AccountName;
        RememberToggle.IsChecked = _config.RememberLogin;

        PasswordInput.KeyDown += (_, ev) => { if (ev.Key == Key.Enter) AuthSubmit_Click(this, new RoutedEventArgs()); };
        AccountInput.KeyDown += (_, ev) => { if (ev.Key == Key.Enter) AuthSubmit_Click(this, new RoutedEventArgs()); };

        _timer.Interval = TimeSpan.FromSeconds(Math.Max(3, _config.RefreshSeconds));
        _timer.Tick += async (_, _) => await PollStatusAsync();
        _timer.Start();

        await PollStatusAsync();
        await TrySilentLoginAsync();
        await CheckForUpdatesAsync();
    }

    /// <summary>
    /// When "Remember me" is set but there's no usable session token (e.g. it
    /// expired), re-authenticate silently with the saved password.
    /// </summary>
    private async Task TrySilentLoginAsync()
    {
        if (_signedIn || !_config.RememberLogin) return;
        if (string.IsNullOrWhiteSpace(_config.AccountName)) return;

        string pw = CredentialProtector.Unprotect(_config.SavedPassword);
        if (string.IsNullOrEmpty(pw)) return;

        try
        {
            var login = await PostAccountAsync("login", _config.AccountName, pw);
            if (login is { Token: not null } && !string.IsNullOrEmpty(login.Token))
            {
                SetSignedIn(_config.AccountName, login.Token);
                _config.LoginToken = login.Token;
                _config.Save();
            }
        }
        catch
        {
            // Server unreachable: stay signed out, user can retry.
        }
    }

    // ===================== Server status =====================

    private async Task PollStatusAsync()
    {
        if (_polling) return;
        _polling = true;
        try
        {
            List<ServerStatus>? servers;
            try
            {
                var json = await Http.GetStringAsync(_config.StatusUrl);
                servers = JsonSerializer.Deserialize<List<ServerStatus>>(json, JsonOpts);
            }
            catch
            {
                SetOffline("Server unreachable");
                return;
            }

            var server = servers?.FirstOrDefault();
            if (server is null) { SetOffline("No server registered"); return; }

            // The status endpoint (RPC web) can answer before the login/game server is
            // actually accepting connections: the server binds its login port only after
            // it finishes loading assets/scripts. Only report ONLINE once that port is
            // truly listening, otherwise the Play button would let users connect too early.
            if (!await IsLoginPortReadyAsync())
            {
                SetStarting(server);
                return;
            }

            SetOnline(server);
        }
        finally
        {
            _polling = false;
            // While the server is still coming up (or unreachable), re-check
            // frequently so the UI converges to ONLINE quickly once the login
            // port binds. Relax to the configured interval once it's online.
            var next = _serverOnline
                ? TimeSpan.FromSeconds(Math.Max(3, _config.RefreshSeconds))
                : TimeSpan.FromSeconds(2);
            if (_timer.Interval != next) _timer.Interval = next;
        }
    }

    /// <summary>
    /// Returns true only if the login server port is accepting TCP connections,
    /// i.e. the server has finished starting up and is ready for clients.
    /// </summary>
    private async Task<bool> IsLoginPortReadyAsync()
    {
        try
        {
            using var tcp = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            // The token overload handles dual-stack hosts (e.g. "localhost"
            // resolving to ::1, which the server does not bind) and cancels the
            // attempt cleanly instead of leaking a connect past the timeout.
            await tcp.ConnectAsync(_config.LoginAddress, _config.LoginPort, cts.Token);
            return tcp.Connected;
        }
        catch
        {
            return false;
        }
    }

    private void SetOnline(ServerStatus server)
    {
        var green = (Brush)FindResource("OnlineGreen");
        StatusText.Text = server.PreventLogin ? "MAINTENANCE" : "ONLINE";
        StatusText.Foreground = (Brush)FindResource("TextPrimary");
        StatusDot.Fill = server.PreventLogin ? (Brush)FindResource("GoldBrush") : green;
        StatusGlow.Fill = StatusDot.Fill;
        ServerNameText.Text = string.IsNullOrWhiteSpace(server.Name) ? _config.ServerName : server.Name;
        PlayersText.Text = $"{server.LoginNum}";
        TrafficText.Text = server.TrafficName;
        _serverOnline = true;
        _preventLogin = server.PreventLogin;
        RefreshPlayButton();
        StartPulse();
    }

    private void SetOffline(string reason)
    {
        var red = (Brush)FindResource("OfflineRed");
        StatusText.Text = "OFFLINE";
        StatusText.Foreground = red;
        StatusDot.Fill = red;
        StatusGlow.Fill = red;
        PlayersText.Text = "—";
        TrafficText.Text = reason;
        _serverOnline = false;
        RefreshPlayButton();
        StopPulse();
    }

    // Server process is up (status endpoint answers) but not yet accepting logins.
    private void SetStarting(ServerStatus server)
    {
        var gold = (Brush)FindResource("GoldBrush");
        StatusText.Text = "STARTING";
        StatusText.Foreground = (Brush)FindResource("TextPrimary");
        StatusDot.Fill = gold;
        StatusGlow.Fill = gold;
        ServerNameText.Text = string.IsNullOrWhiteSpace(server.Name) ? _config.ServerName : server.Name;
        PlayersText.Text = "—";
        TrafficText.Text = "Server starting…";
        _serverOnline = false;
        _preventLogin = server.PreventLogin;
        RefreshPlayButton();
        StartPulse();
    }

    private void StartPulse()
    {
        var pulse = new DoubleAnimation(0.15, 0.6, TimeSpan.FromSeconds(1.1))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase()
        };
        StatusGlow.BeginAnimation(OpacityProperty, pulse);
    }

    private void StopPulse()
    {
        StatusGlow.BeginAnimation(OpacityProperty, null);
        StatusGlow.Opacity = 0.35;
    }

    // ===================== Account / auth =====================

    private void SignInLink_Click(object sender, RoutedEventArgs e) => ShowAuth(createMode: false);

    private void SignOutLink_Click(object sender, RoutedEventArgs e)
    {
        SetSignedOut();
        // Explicit sign-out stops remembering and clears the saved session.
        _config.LoginToken = "";
        _config.SavedPassword = "";
        _config.RememberLogin = false;
        _config.Save();
        RememberToggle.IsChecked = false;
        PasswordInput.Password = "";
        Toast("Signed out.");
    }

    private void ShowAuth(bool createMode)
    {
        SetAuthMode(createMode);
        AuthMessage.Visibility = Visibility.Collapsed;

        if (string.IsNullOrEmpty(AccountInput.Text)) AccountInput.Text = _config.AccountName;
        RememberToggle.IsChecked = _config.RememberLogin;
        // Pre-fill the saved password (decrypted) when remembering.
        if (_config.RememberLogin && string.IsNullOrEmpty(PasswordInput.Password))
        {
            string saved = CredentialProtector.Unprotect(_config.SavedPassword);
            if (!string.IsNullOrEmpty(saved)) PasswordInput.Password = saved;
        }

        AuthOverlay.Visibility = Visibility.Visible;
        if (string.IsNullOrEmpty(AccountInput.Text)) AccountInput.Focus(); else PasswordInput.Focus();
    }

    private void AuthClose_Click(object sender, RoutedEventArgs e) => AuthOverlay.Visibility = Visibility.Collapsed;

    private void AuthSwitch_Click(object sender, RoutedEventArgs e) => SetAuthMode(!_createMode);

    private void SetAuthMode(bool createMode)
    {
        _createMode = createMode;
        AuthTitle.Text = createMode ? "CREATE ACCOUNT" : "SIGN IN";
        AuthSubtitle.Text = createMode
            ? "Create an account to play Casual Dogma."
            : "Enter your credentials to play Casual Dogma.";
        AuthSubmit.Content = createMode ? "CREATE ACCOUNT" : "SIGN IN";
        AuthSwitchPrompt.Text = createMode ? "Already have an account?" : "No account yet?";
        AuthSwitch.Content = createMode ? "Sign in" : "Create one";
        AuthMessage.Visibility = Visibility.Collapsed;
    }

    private async void AuthSubmit_Click(object sender, RoutedEventArgs e)
    {
        if (AuthOverlay.Visibility != Visibility.Visible) return;

        string account = AccountInput.Text.Trim();
        string password = PasswordInput.Password;

        if (string.IsNullOrWhiteSpace(account)) { AuthError("Please enter an account name."); return; }
        if (string.IsNullOrWhiteSpace(password)) { AuthError("Please enter a password."); return; }

        AuthSubmit.IsEnabled = false;
        try
        {
            if (_createMode)
            {
                // The server requires an e-mail format but mail verification is off,
                // so we generate a synthetic address (no e-mail field in the UI).
                string email = $"{account}@casualdogma.local";

                var create = await PostAccountAsync("create", account, password, email);
                if (create is null) { AuthError("No response from server."); return; }
                if (!string.IsNullOrEmpty(create.Error)) { AuthError(create.Error); return; }

                // Auto sign-in right after a successful creation.
                var login = await PostAccountAsync("login", account, password);
                if (login is { Token: not null } && !string.IsNullOrEmpty(login.Token))
                {
                    SetSignedIn(account, login.Token);
                    PersistSession(account, login.Token, password);
                    AuthOverlay.Visibility = Visibility.Collapsed;
                    Toast($"Account created. Signed in as {account}.");
                }
                else
                {
                    SetAuthMode(createMode: false);
                    AuthInfo("Account created — please sign in.");
                }
            }
            else
            {
                var login = await PostAccountAsync("login", account, password);
                if (login is null) { AuthError("No response from server."); return; }
                if (!string.IsNullOrEmpty(login.Error) || string.IsNullOrEmpty(login.Token))
                {
                    AuthError(string.IsNullOrEmpty(login.Error) ? "Login failed." : login.Error);
                    return;
                }

                SetSignedIn(account, login.Token!);
                PersistSession(account, login.Token!, password);
                AuthOverlay.Visibility = Visibility.Collapsed;
                Toast($"Signed in as {account}.");
            }
        }
        catch (Exception ex)
        {
            AuthError($"Connection error: {ex.Message}");
        }
        finally
        {
            AuthSubmit.IsEnabled = true;
        }
    }

    private async Task<AccountResponse?> PostAccountAsync(string action, string account, string password, string? email = null)
    {
        var payload = new AccountRequest
        {
            Action = action,
            Account = account,
            Password = password,
            Email = email ?? "",
            PatchVersion = PatchAuth.Version,
            PatchToken = PatchAuth.Compute(account)
        };
        var content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");
        var resp = await Http.PostAsync(_config.AccountApiUrl, content);
        var body = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AccountResponse>(body, JsonOpts);
    }

    private void SetSignedIn(string account, string token)
    {
        _signedIn = true;
        _token = token;
        AccountStatusText.Text = account;
        AccountStatusText.Foreground = (Brush)FindResource("TextPrimary");
        SignInLink.Visibility = Visibility.Collapsed;
        SignOutLink.Visibility = Visibility.Visible;
    }

    private void SetSignedOut()
    {
        _signedIn = false;
        _token = "";
        AccountStatusText.Text = "Not signed in";
        AccountStatusText.Foreground = (Brush)FindResource("TextMuted");
        SignInLink.Visibility = Visibility.Visible;
        SignOutLink.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Persists the session. The username is always remembered for convenience;
    /// the token and the DPAPI-encrypted password are only stored when
    /// "Remember me" is checked.
    /// </summary>
    private void PersistSession(string account, string token, string password)
    {
        bool remember = RememberToggle.IsChecked == true;
        _config.AccountName = account;
        _config.RememberLogin = remember;
        _config.LoginToken = remember ? token : "";
        _config.SavedPassword = remember ? CredentialProtector.Protect(password) : "";
        _config.Save();
    }

    private void AuthError(string msg)
    {
        AuthMessage.Text = msg;
        AuthMessage.Foreground = (Brush)FindResource("OfflineRed");
        AuthMessage.Visibility = Visibility.Visible;
    }

    private void AuthInfo(string msg)
    {
        AuthMessage.Text = msg;
        AuthMessage.Foreground = (Brush)FindResource("OnlineGreen");
        AuthMessage.Visibility = Visibility.Visible;
    }

    // ===================== Display settings =====================

    private void DisplayButton_Click(object sender, RoutedEventArgs e)
    {
        WidthInput.Text = _config.WindowWidth.ToString();
        HeightInput.Text = _config.WindowHeight.ToString();
        FullscreenRadio.IsChecked = _config.Fullscreen;
        WindowedRadio.IsChecked = !_config.Fullscreen;
        VSyncToggle.IsChecked = _config.VSync;
        DisplayMessage.Visibility = Visibility.Collapsed;
        DisplayOverlay.Visibility = Visibility.Visible;
    }

    private void DisplayClose_Click(object sender, RoutedEventArgs e) => DisplayOverlay.Visibility = Visibility.Collapsed;

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button b && b.Tag is string tag)
        {
            var parts = tag.Split('x');
            if (parts.Length == 2)
            {
                WidthInput.Text = parts[0];
                HeightInput.Text = parts[1];
            }
        }
    }

    private void DisplayApply_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthInput.Text.Trim(), out int w) || w < 640 || w > 7680)
        {
            DisplayMessage.Text = "Width must be a number between 640 and 7680.";
            DisplayMessage.Visibility = Visibility.Visible;
            return;
        }
        if (!int.TryParse(HeightInput.Text.Trim(), out int h) || h < 480 || h > 4320)
        {
            DisplayMessage.Text = "Height must be a number between 480 and 4320.";
            DisplayMessage.Visibility = Visibility.Visible;
            return;
        }

        _config.WindowWidth = w;
        _config.WindowHeight = h;
        _config.Fullscreen = FullscreenRadio.IsChecked == true;
        _config.VSync = VSyncToggle.IsChecked == true;
        _config.Save();

        // Apply immediately too, if the client is present.
        if (File.Exists(_config.ClientPath))
        {
            try { GameConfigWriter.ApplyDisplaySettings(_config.ClientPath, w, h, _config.Fullscreen, _config.VSync); }
            catch { /* non-fatal; will retry on Play */ }
        }

        DisplayOverlay.Visibility = Visibility.Collapsed;
        string mode = _config.Fullscreen ? "fullscreen" : $"{w}×{h} windowed";
        Toast($"Display set to {mode}.");
    }

    // ===================== Server connection =====================

    private void ServerButton_Click(object sender, RoutedEventArgs e)
    {
        LobbyIpInput.Text = _config.LoginAddress;
        LobbyPortInput.Text = _config.LoginPort.ToString();
        var (dlHost, dlPort) = ParseHostPort(_config.DownloadUrl, 52099);
        DlIpInput.Text = dlHost;
        DlPortInput.Text = dlPort.ToString();
        ServerMessage.Visibility = Visibility.Collapsed;
        ServerOverlay.Visibility = Visibility.Visible;
        LobbyIpInput.Focus();
    }

    private void ServerClose_Click(object sender, RoutedEventArgs e) => ServerOverlay.Visibility = Visibility.Collapsed;

    private void ServerReset_Click(object sender, RoutedEventArgs e)
    {
        LobbyIpInput.Text = "localhost";
        LobbyPortInput.Text = "52100";
        DlIpInput.Text = "127.0.0.1";
        DlPortInput.Text = "52099";
        ServerMessage.Visibility = Visibility.Collapsed;
    }

    private async void ServerApply_Click(object sender, RoutedEventArgs e)
    {
        string lobbyIp = LobbyIpInput.Text.Trim();
        string dlIp = DlIpInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(lobbyIp)) { ServerError("Lobby IP cannot be empty."); return; }
        if (!int.TryParse(LobbyPortInput.Text.Trim(), out int lobbyPort) || lobbyPort < 1 || lobbyPort > 65535)
        { ServerError("Lobby port must be 1–65535."); return; }
        if (string.IsNullOrWhiteSpace(dlIp)) { ServerError("Download IP cannot be empty."); return; }
        if (!int.TryParse(DlPortInput.Text.Trim(), out int dlPort) || dlPort < 1 || dlPort > 65535)
        { ServerError("Download port must be 1–65535."); return; }

        var (oldDlHost, oldDlPort) = ParseHostPort(_config.DownloadUrl, 52099);
        bool changed = lobbyIp != _config.LoginAddress || lobbyPort != _config.LoginPort
                       || dlIp != oldDlHost || dlPort != oldDlPort;

        _config.LoginAddress = lobbyIp;
        _config.LoginPort = lobbyPort;
        _config.DownloadUrl = $"http://{dlIp}:{dlPort}/win/";
        _config.StatusUrl = $"http://{dlIp}:{dlPort}/rpc/status";
        _config.AccountApiUrl = $"http://{dlIp}:{dlPort}/api/account";

        if (changed)
        {
            // A login token / saved password is only valid on the server that issued it.
            SetSignedOut();
            _config.AccountName = "";
            _config.LoginToken = "";
            _config.SavedPassword = "";
            _config.RememberLogin = false;
            RememberToggle.IsChecked = false;
        }
        _config.Save();

        ServerOverlay.Visibility = Visibility.Collapsed;
        Toast(changed ? $"Connecting to {dlIp}…" : "Server settings saved.");

        await PollStatusAsync();
        await CheckForUpdatesAsync();
    }

    private void ServerError(string msg)
    {
        ServerMessage.Text = msg;
        ServerMessage.Visibility = Visibility.Visible;
    }

    private static (string host, int port) ParseHostPort(string url, int fallbackPort)
    {
        try
        {
            var u = new Uri(url);
            return (u.Host, u.Port > 0 ? u.Port : fallbackPort);
        }
        catch
        {
            return ("127.0.0.1", fallbackPort);
        }
    }

    // ===================== Patcher / installer =====================

    private async Task CheckForUpdatesAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.PatchManifestUrl))
        {
            // Updating not configured yet: behave as a plain launcher.
            SetPatchState(PatchState.Disabled);
            return;
        }

        SetPatchState(PatchState.Checking);
        try
        {
            var manifest = await _patcher.FetchManifestAsync(_config.PatchManifestUrl);
            // Routine checks use size/existence for speed; full hashing happens on download
            // (and via an explicit repair, if added later).
            var plan = await _patcher.CheckAsync(manifest, InstallDir, verifyHashes: false);
            _plan = plan;

            if (plan.UpToDate)
            {
                _config.InstalledVersion = manifest.Version;
                _config.Save();
                SetPatchState(PatchState.Ready);
            }
            else
            {
                SetPatchState(PatchState.UpdateAvailable);
            }
        }
        catch (Exception ex)
        {
            Toast($"Update check failed: {ex.Message}");
            SetPatchState(PatchState.Error);
        }
    }

    private async Task RunUpdateAsync()
    {
        if (_plan is null || _plan.UpToDate)
        {
            SetPatchState(PatchState.Ready);
            return;
        }

        _patchCts = new CancellationTokenSource();
        SetPatchState(PatchState.Downloading);
        PatchPanel.Visibility = Visibility.Visible;
        PatchProgressBar.Value = 0;

        var progress = new Progress<PatchProgress>(p =>
        {
            PatchProgressBar.Value = p.Percent;
            PatchPercentText.Text = $"{p.Percent}%";
            PatchStatusText.Text = $"Downloading {p.FileIndex} / {p.FileCount}";
            PatchDetailText.Text = $"{p.CurrentFile}   {HumanBytes(p.DownloadedBytes)} / {HumanBytes(p.TotalBytes)}";
        });

        try
        {
            Directory.CreateDirectory(InstallDir);
            await _patcher.DownloadAsync(_plan, InstallDir, _plan.Manifest.BaseUrl, progress, _patchCts.Token);

            _config.InstalledVersion = _plan.Manifest.Version;
            // If we just installed into a fresh folder, point ClientPath at the new DDO.exe.
            if (!File.Exists(_config.ClientPath))
            {
                var candidate = Path.Combine(InstallDir, "DDO.exe");
                if (File.Exists(candidate)) _config.ClientPath = candidate;
            }
            _config.Save();

            _plan = new PatchPlan(_plan.Manifest);
            PatchPanel.Visibility = Visibility.Collapsed;
            SetPatchState(PatchState.Ready);
            Toast($"Up to date (v{_config.InstalledVersion}).");
        }
        catch (OperationCanceledException)
        {
            PatchPanel.Visibility = Visibility.Collapsed;
            SetPatchState(PatchState.UpdateAvailable);
            Toast("Download canceled.");
        }
        catch (Exception ex)
        {
            PatchPanel.Visibility = Visibility.Collapsed;
            SetPatchState(PatchState.Error);
            Toast($"Download failed: {ex.Message}");
        }
        finally
        {
            _patchCts?.Dispose();
            _patchCts = null;
        }
    }

    private void SetPatchState(PatchState state)
    {
        _patchState = state;
        RefreshPlayButton();
    }

    /// <summary>Single source of truth for the main button's label, icon and enabled state.</summary>
    private void RefreshPlayButton()
    {
        switch (_patchState)
        {
            case PatchState.Checking:
                PlayButtonText.Text = "CHECKING";
                PlayButtonIcon.Text = "\uE72C";
                PlayButton.IsEnabled = false;
                break;
            case PatchState.Downloading:
                PlayButtonText.Text = "CANCEL";
                PlayButtonIcon.Text = "\uE711";
                PlayButton.IsEnabled = true;
                break;
            case PatchState.UpdateAvailable:
                bool fresh = string.IsNullOrEmpty(_config.InstalledVersion);
                PlayButtonText.Text = fresh ? "INSTALL" : "UPDATE";
                PlayButtonIcon.Text = "\uE896";
                PlayButton.IsEnabled = true;
                break;
            case PatchState.Error:
                PlayButtonText.Text = "RETRY";
                PlayButtonIcon.Text = "\uE72C";
                PlayButton.IsEnabled = true;
                break;
            default: // Disabled or Ready -> normal play
                PlayButtonText.Text = "PLAY";
                PlayButtonIcon.Text = "\uE768";
                PlayButton.IsEnabled = _serverOnline && !_preventLogin;
                break;
        }
    }

    private static string HumanBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u == 0 ? $"{bytes} B" : $"{v:0.0} {units[u]}";
    }

    // ===================== Launch =====================

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        // The main button is contextual: it installs/updates/cancels/retries when a
        // patch manifest is configured, otherwise it launches the game.
        switch (_patchState)
        {
            case PatchState.Checking:
                return;
            case PatchState.Downloading:
                _patchCts?.Cancel();
                return;
            case PatchState.Error:
                await CheckForUpdatesAsync();
                return;
            case PatchState.UpdateAvailable:
                await RunUpdateAsync();
                return;
        }

        LaunchGame();
    }

    private void LaunchGame()
    {
        if (!_signedIn)
        {
            Toast("Please sign in to play.");
            ShowAuth(createMode: false);
            return;
        }

        if (!File.Exists(_config.ClientPath))
        {
            Toast("Game client not found. Use \"Client\" to locate DDO.exe.");
            PickClient();
            return;
        }

        if (_config.RequireLevelSyncApplier && !IsLevelSyncApplierInstalled())
        {
            // First-run: try to install the patch automatically, then launch if it succeeds.
            _ = InstallApplierThenLaunchAsync();
            return;
        }

        try
        {
            try
            {
                GameConfigWriter.ApplyDisplaySettings(_config.ClientPath, _config.WindowWidth,
                    _config.WindowHeight, _config.Fullscreen, _config.VSync);
            }
            catch (Exception cfgEx)
            {
                Toast($"Couldn't write display config: {cfgEx.Message}");
            }

            string token = string.IsNullOrEmpty(_token) ? _config.Token : _token;
            var args = $"addr={_config.LoginAddress} port={_config.LoginPort} " +
                       $"token={token} DL={_config.DownloadUrl} " +
                       $"LVer={_config.ClientVersion} RVer={_config.RemoteVersion}";

            var psi = new ProcessStartInfo
            {
                FileName = _config.ClientPath,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(_config.ClientPath) ?? Environment.CurrentDirectory,
                UseShellExecute = true
            };

            var game = Process.Start(psi);
            if (game is not null)
            {
                // "Logout to launcher" in-game simply exits DDO.exe; since the launcher
                // is its parent, we stay alive and pop back to the foreground on exit.
                game.EnableRaisingEvents = true;
                game.Exited += (_, _) => Dispatcher.Invoke(ReturnFromGame);
            }

            // Best-effort: make sure the zone level-sync applier is running. It normally auto-starts at
            // logon, but kicking the scheduled task here covers the case where it was installed after
            // logon. No elevation needed to start your own task; failures are harmless and ignored.
            EnsureLevelSyncApplierRunning();

            Toast("Launching Casual Dogma…");
            WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            Toast($"Failed to launch: {ex.Message}");
        }
    }

    private const string LevelSyncTaskName = "DDONLevelSyncApplier";

    /// <summary>
    /// Runs the level-sync applier installer (setup.cmd) when the patch isn't present yet, then
    /// launches the game once the scheduled task appears. setup.cmd prompts for server/character
    /// details and self-elevates to register the task, so a UAC prompt is expected.
    /// </summary>
    private async Task InstallApplierThenLaunchAsync()
    {
        string? setup = FindLevelSyncSetup();
        if (setup is null)
        {
            Toast("Level sync patch not installed and setup.cmd could not be found. Please install it manually.");
            return;
        }

        try
        {
            Toast("Installing the level sync patch…");

            var psi = new ProcessStartInfo
            {
                FileName = setup,
                WorkingDirectory = Path.GetDirectoryName(setup) ?? Environment.CurrentDirectory,
                UseShellExecute = true
            };

            var proc = Process.Start(psi);
            if (proc is not null)
            {
                await Task.Run(() => proc.WaitForExit());
            }

            // The elevated registration may finish slightly after setup.cmd closes, so poll briefly.
            bool installed = false;
            for (int i = 0; i < 10; i++)
            {
                if (IsLevelSyncApplierInstalled()) { installed = true; break; }
                await Task.Delay(2000);
            }

            if (installed)
            {
                Toast("Level sync patch installed.");
                LaunchGame();
            }
            else
            {
                Toast("Setup didn't complete. Please finish the installer, then press Play again.");
            }
        }
        catch (Exception ex)
        {
            Toast($"Couldn't run the level sync installer: {ex.Message}");
        }
    }

    /// <summary>
    /// Locates the level-sync applier's setup.cmd: an explicit config path if set, otherwise common
    /// locations next to the launcher (and the dev source layout as a fallback).
    /// </summary>
    private string? FindLevelSyncSetup()
    {
        if (!string.IsNullOrWhiteSpace(_config.LevelSyncSetupPath) && File.Exists(_config.LevelSyncSetupPath))
        {
            return _config.LevelSyncSetupPath;
        }

        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDir, "applier", "setup.cmd"),
            Path.Combine(baseDir, "LevelSync", "setup.cmd"),
            Path.Combine(baseDir, "setup.cmd"),
            Path.Combine(baseDir, "..", "client-mod", "applier", "setup.cmd"),
            @"D:\DDON\client-mod\applier\setup.cmd"
        };

        foreach (string candidate in candidates)
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            catch
            {
                // Ignore malformed candidate paths.
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true if the level-sync applier scheduled task is registered on this machine, i.e. the
    /// player has installed the client patch (via setup.cmd / install_applier_task.ps1).
    /// </summary>
    private static bool IsLevelSyncApplierInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/query /tn \"{LevelSyncTaskName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                return false;
            }

            // Drain output so the child can't block on a full pipe, then bound the wait.
            proc.StandardOutput.ReadToEnd();
            proc.StandardError.ReadToEnd();
            if (!proc.WaitForExit(5000))
            {
                try { proc.Kill(); } catch { /* ignore */ }
                return false;
            }

            return proc.ExitCode == 0;
        }
        catch
        {
            // If we can't determine it, fail closed: treat as not installed.
            return false;
        }
    }

    /// <summary>
    /// Best-effort start of the zone level-sync applier scheduled task. Silent no-op if the task isn't
    /// installed (the player hasn't opted in) or can't be started.
    /// </summary>
    private static void EnsureLevelSyncApplierRunning()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/run /tn \"{LevelSyncTaskName}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }
        catch
        {
            // Applier not installed or couldn't start; level sync is optional, so ignore.
        }
    }

    /// <summary>Bring the launcher back to the foreground after the game closes / logs out.</summary>
    private async void ReturnFromGame()
    {
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Show();
        Activate();
        // Briefly assert top-most to force the window in front, then release it.
        Topmost = true;
        Topmost = false;
        Toast("Welcome back to Casual Dogma.");

        await PollStatusAsync();
        await CheckForUpdatesAsync();
    }

    private void FolderButton_Click(object sender, RoutedEventArgs e) => PickClient();

    private void PickClient()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select the game client (DDO.exe)",
            Filter = "DDO client (DDO.exe)|DDO.exe|Executable (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (File.Exists(_config.ClientPath))
            dlg.InitialDirectory = Path.GetDirectoryName(_config.ClientPath);

        if (dlg.ShowDialog(this) == true)
        {
            _config.ClientPath = dlg.FileName;
            _config.Save();
            Toast("Client path saved.");
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await PollStatusAsync();

    private DispatcherTimer? _toastTimer;
    private void Toast(string message)
    {
        ToastText.Text = message;
        ToastText.Visibility = Visibility.Visible;
        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _toastTimer.Tick += (_, _) => { ToastText.Visibility = Visibility.Collapsed; _toastTimer!.Stop(); };
        _toastTimer.Start();
    }

    // ===================== Window chrome =====================

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    // ===================== DTOs =====================

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private sealed class ServerStatus
    {
        public string Name { get; set; } = "";
        public string TrafficName { get; set; } = "";
        public uint TrafficLevel { get; set; }
        public uint MaxLoginNum { get; set; }
        public uint LoginNum { get; set; }
        public bool PreventLogin { get; set; }
    }

    private sealed class AccountRequest
    {
        public string Action { get; set; } = "";
        public string Account { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public int PatchVersion { get; set; }
        public string PatchToken { get; set; } = "";
    }

    private sealed class AccountResponse
    {
        public string? Error { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
    }
}

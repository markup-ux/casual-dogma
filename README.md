# Casual Dogma

A community snapshot of the **Casual Dogma** private server for [Dragon's Dogma Online](https://en.wikipedia.org/wiki/Dragon%27s_Dogma_Online) — forked and customized from the [Arrowgene DDON](https://github.com/sebastian-heinz/Arrowgene.DragonsDogmaOnline) emulator.

This repository is a **frozen release** for others to run, play, and modify on their own. It is not a live upstream — treat it as a starting point and make it yours.

## What's included

| Component | Purpose |
|-----------|---------|
| `Server/` | DDON game/login/web server with Casual Dogma scripts and settings |
| `Launcher/` | WPF launcher (account login, server status, client config) |
| `LauncherShim/` | Small exe shim that replaces the stock launcher entry point |
| `client-mod/` | 32-bit client DLL, injector, and level-sync memory applier |
| `level-sync/` | Zone level-sync integration docs and reference source |
| `tools/SkillPaletteHotkeys/` | Optional AutoHotkey helper for skill palette |
| Batch scripts | `start-server.bat`, `stop-server.bat`, `restart-server.bat` |
| Discord setup | Optional `setup_discord.py` + `server_blueprint.py` for community Discord |

## What you must obtain separately

The **game client is not included** (copyright). You need a legal copy of Dragon's Dogma Online for PC (version `03.04.003.20181115.0` / remote version `3040008`). Community guides for obtaining and patching the client are available from the broader DDON community.

Install the client somewhere on disk and point the launcher at `DDO.exe`.

## Requirements

- **Windows 10/11** (client is 32-bit; server and launcher are .NET)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- [Rust toolchain](https://rustup.rs/) with the `i686-pc-windows-msvc` target (only if rebuilding client-mod from source)
- [Python 3](https://www.python.org/) (optional — for level-sync applier fallback and Discord setup)

## Quick start

### 1. Clone and build the server

```powershell
git clone https://github.com/markup-ux/casual-dogma.git
cd casual-dogma

dotnet build Server\Arrowgene.Ddon.Cli\Arrowgene.Ddon.Cli.csproj -c Release
```

Copy the server config template into the build output (paths are relative to the exe):

```powershell
copy Server\config\Arrowgene.Ddon.config.template.json `
     Server\Arrowgene.Ddon.Cli\bin\Release\net10.0\Files\Arrowgene.Ddon.config.json
```

Start the server:

```powershell
.\start-server.bat
```

The server listens on:

| Service | Port |
|---------|------|
| Web / RPC / downloads | 52099 |
| Login | 52100 |
| Game | 52000 |

### 2. Build the launcher

```powershell
dotnet publish Launcher\Launcher.csproj -c Release -r win-x64 --self-contained false
```

The published launcher is at `Launcher\bin\Release\net10.0-windows\win-x64\publish\`.

On first run the launcher creates `launcher.config.json` beside the exe. Use `launcher.config.example.json` in the repo root as a reference — set `ClientPath` to your `DDO.exe`.

### 3. Client mod (custom skills + level sync)

Prebuilt 32-bit binaries are shipped in `client-mod/dist/`:

- `ddon_mod.dll` — in-process client mod (custom skill binding)
- `inject.exe` — DLL injector

To inject after starting the game:

```powershell
cd client-mod
.\inject.ps1
```

Rebuild from source if needed:

```powershell
.\build.ps1
```

### 4. Level-sync applier (recommended)

Casual Dogma uses per-zone level sync so over-leveled characters are fair in low-level dungeons. Install the memory applier once:

```powershell
cd client-mod\applier
.\setup.cmd
```

See `client-mod/applier/README.md` for details. The launcher can require this applier before allowing play (`RequireLevelSyncApplier` in config).

### 5. Create an account and play

1. Start the server (`start-server.bat`).
2. Run the launcher, create an account, and sign in.
3. Inject the client mod (`client-mod\inject.ps1`) after the game loads.
4. Play.

## Casual Dogma server features

Gameplay tuning lives in `Server/Arrowgene.Ddon.Scripts/scripts/settings/GameServerSettings.csx` (hot-reloadable). Highlights:

- Auto-loot, story unlocks, optional quests, max jewelry slots
- Exploration gear drops with pity system
- Dungeon mob repop for farming
- Zone level sync (server-side + client applier)
- Recoverable HP pinning during leveling
- Supply caches, revival recharge timers, and more

Edit the `.csx` file and restart the server (or use hot-reload where supported) to customize.

## Optional: Discord community server

```powershell
pip install -r requirements.txt
copy .env.example .env
# Edit .env with your bot token and guild ID
python setup_discord.py
```

## Project layout

```
casual-dogma/
├── Server/                 # Arrowgene DDON server (modified)
├── Launcher/               # Game launcher (WPF)
├── LauncherShim/           # Launcher entry-point shim
├── client-mod/             # Client DLL, injector, level-sync applier
├── level-sync/             # Level-sync reference / integration notes
├── tools/                  # Optional helpers (SkillPaletteHotkeys)
├── start-server.bat        # Start server in a visible console
├── stop-server.bat         # Stop running server
├── restart-server.bat      # Rebuild + restart
└── launcher.config.example.json
```

## License

MIT — see [LICENSE](LICENSE). Dragon's Dogma Online is property of Capcom. This project is for educational and preservation purposes.

## Credits

- [Arrowgene Dragons Dogma Online](https://github.com/sebastian-heinz/Arrowgene.DragonsDogmaOnline) — base server emulator
- [ddon-level-sync](https://github.com/markup-ux/ddon-level-sync) — zone level sync

## Contributing

This repo is a **community snapshot**, not an actively maintained upstream. Fork it, change it, run your own server — that's the point.

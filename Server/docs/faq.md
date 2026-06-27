# Frequently Asked Questions

## When I launch the server, it says "Please update the database..."
As features are developed, we must often change the structure of the database. To stay up to date with these changes, you'll need to migrate your database. If you built the server using `publish.cmd` or `publish.sh`, you can use the provided `MigrateDatabase.cmd` to do this. If you built the server using VS or another tool, you can navigate to `Arrowgene.Ddon.Cli` and run `dotnet run dbmigration` from the command line or PowerShell.

## How do I update the translation?
* Option 1: Use the new and improved [DDON Launcher](https://github.com/D00MK1D/DDON-Launcher/releases). Place this in your DDON directory (near `ddon.exe`), run the launcher, and click the `大A` button. By default, this downloads and installs the most recent *English* translation, or you can provide the github URL for another translation project.
* Option 2: Use the legacy local patcher. Place a translation file (`gmd.csv`) in `[Your DDON Directory]/nativePC/Server/Files/Client`, then drag the folder `[Your DDON Directory]/nativePC/rom` onto `pack_gmd_english.cmd` in the same directory.

## How do I use admin commands?
You'll need to modify your account status in the server's database. If you're running locally and using the default SQLite DB, [DB4S](https://sqlitebrowser.org/) is a convenient tool for this. Make sure you've logged in at least once, open up the DB (`Server\Files\Database\db.sqlite`) in DB4S, navigate to the `account` table, and set the `state` column for your account to `100`.

Alternatively, you can disable the account status check, by editing the server setting scripts. See below for details.

## How do I place monsters and loot?
The most convenient tool for this is [DDONTools](https://github.com/alborrajo/DDOn-Tools).

## How do I change settings like EXP multipliers?
See the [README](https://github.com/sebastian-heinz/Arrowgene.DragonsDogmaOnline/blob/develop/Arrowgene.Ddon.Scripts/scripts/settings/README.md).

## How do I open my game up for multiplayer?
* Ensure that the IP addresses and ports in `Server/Files/Arrowgene.Ddon.config.json` are publicly accessible.
* Update `Server/Files/Assets/GameServerList.csv` with your publicly facing IP address/ports.
* Players can add your server to their launcher by clicking the gear icon in the bottom left.

## How do I run multiple channels at once?
Each channel is a separately running server process. One server acts as the leader, managing login and some periodic tasks, as well as its own GameServer instance, while also directing players to the subordinate channels. In general, to prevent issues, all channels should run from the same asset folder and same build of the server.
* Make a copy of your config file, then change the `Port` and `Id` values so that they do not overlap with any other channel's ports and IDs. Web, login, and game servers must all have unique ids. The copy can be in the same folder as the original.
* Add the new channel in a new row of `GameServerList.csv`. The values here should reflect the config files.
  * `IsHide` will prevent players from seeing this channel in the channel list.
  * `PreventLogin` will prevent automatic load balancing from placing new characters on this channel.
* Launch each separate instance with `dotnet run server start --config=[PATH TO CONFIG FILE]`.
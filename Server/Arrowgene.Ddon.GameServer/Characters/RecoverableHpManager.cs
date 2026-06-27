#nullable enable
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.RpcPacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Arrowgene.Ddon.GameServer.Characters
{
    /// <summary>
    /// Keeps recoverable HP (gray / WhiteHP) from dropping while a character is below the job level cap.
    ///
    /// Combat HP is computed client-side; the server tracks a per-session white-HP ceiling on
    /// <see cref="GameClient"/>, patches periodic RPC payloads, relays the correction to party/hub
    /// peers, and echoes it back to the sender so vanilla clients stay in sync (no injection).
    /// A legacy JSON signal file is still written for optional local tooling.
    /// </summary>
    public class RecoverableHpManager
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(RecoverableHpManager));

        private readonly DdonGameServer _server;

        // Signal entries keyed by in-game character name ("First Last").
        private readonly ConcurrentDictionary<string, RecoverableHpSignalEntry> _signal = new();

        private readonly object _fileLock = new();
        private readonly string _signalPath;

        public RecoverableHpManager(DdonGameServer server)
        {
            _server = server;
            _signalPath = Environment.GetEnvironmentVariable("DDON_RECOVERABLE_HP_FILE")
                          ?? Path.Combine("D:", "DDON", "client-mod", "sync", "recoverable_hp_state.json");
        }

        private class RecoverableHpSignalEntry
        {
            public bool PinRecoverableHp { get; set; }
            public uint JobLevel { get; set; }
            public uint JobLevelMax { get; set; }
        }

        public class RecoverableHpSignalView
        {
            public bool PinRecoverableHp { get; set; }
            public uint JobLevel { get; set; }
            public uint JobLevelMax { get; set; }
        }

        public bool ShouldPin(CharacterCommon? character)
        {
            if (!_server.GameSettings.GameServerSettings.DisableRecoverableHpLossBelowMaxLevel)
            {
                return false;
            }

            CDataCharacterJobData? jobData = character?.ActiveCharacterJobData;
            if (jobData == null)
            {
                return false;
            }

            uint levelCap = _server.GameSettings.GameServerSettings.JobLevelMax;
            return jobData.Lv < levelCap;
        }

        public bool ShouldPin(GameClient? client)
        {
            return client?.Character != null && ShouldPin(client.Character);
        }

        public RecoverableHpSignalView GetSignalView(string name)
        {
            if (!string.IsNullOrEmpty(name) && _signal.TryGetValue(name, out RecoverableHpSignalEntry? entry))
            {
                return new RecoverableHpSignalView
                {
                    PinRecoverableHp = entry.PinRecoverableHp,
                    JobLevel = entry.JobLevel,
                    JobLevelMax = entry.JobLevelMax
                };
            }

            return new RecoverableHpSignalView();
        }

        public void ClearProtectionState(GameClient client)
        {
            client.ProtectedRecoverableHpCeiling = 0;
        }

        /// <summary>
        /// Session ceiling: highest green or white HP seen while protection is active.
        /// </summary>
        public static ushort GetProtectedWhiteHp(GameClient client, ushort greenHp, ushort reportedWhiteHp)
        {
            ushort ceiling = client.ProtectedRecoverableHpCeiling;
            ushort protectedWhite = GetProtectedWhiteHp(ref ceiling, greenHp, reportedWhiteHp);
            client.ProtectedRecoverableHpCeiling = ceiling;
            return protectedWhite;
        }

        public static ushort GetProtectedWhiteHp(ref ushort ceiling, ushort greenHp, ushort reportedWhiteHp)
        {
            if (greenHp > ceiling)
            {
                ceiling = greenHp;
            }

            if (reportedWhiteHp > ceiling)
            {
                ceiling = reportedWhiteHp;
            }

            return ceiling;
        }

        /// <summary>
        /// Refresh the applier signal and seed the session ceiling when a character enters the world or changes job/level.
        /// </summary>
        public void EvaluateCharacter(GameClient client)
        {
            if (client?.Character == null)
            {
                return;
            }

            EvaluateCharacter(client, client.Character);
        }

        public void EvaluateCharacter(Character? character)
        {
            // Legacy overload for callers without a session; only updates the signal file.
            if (character == null)
            {
                return;
            }

            SyncCombatHpFromStatus(character);

            string name = CharacterName(character);
            uint levelCap = _server.GameSettings.GameServerSettings.JobLevelMax;
            uint jobLevel = character.ActiveCharacterJobData?.Lv ?? 0;
            bool pin = ShouldPin(character);

            if (pin)
            {
                _signal[name] = new RecoverableHpSignalEntry
                {
                    PinRecoverableHp = true,
                    JobLevel = jobLevel,
                    JobLevelMax = levelCap
                };
                Logger.Info(
                    $"[RECOVERABLE_HP] {name} PIN active jobLv={jobLevel} cap={levelCap}");
            }
            else if (_signal.TryRemove(name, out _))
            {
                Logger.Info($"[RECOVERABLE_HP] {name} PIN cleared jobLv={jobLevel} cap={levelCap}");
            }

            WriteSignalFile();
        }

        private void EvaluateCharacter(GameClient client, Character character)
        {
            SyncCombatHpFromStatus(character);

            string name = CharacterName(character);
            uint levelCap = _server.GameSettings.GameServerSettings.JobLevelMax;
            uint jobLevel = character.ActiveCharacterJobData?.Lv ?? 0;
            bool pin = ShouldPin(character);

            if (pin)
            {
                SeedSessionCeiling(client, character);
                _signal[name] = new RecoverableHpSignalEntry
                {
                    PinRecoverableHp = true,
                    JobLevel = jobLevel,
                    JobLevelMax = levelCap
                };
                Logger.Info(
                    $"[RECOVERABLE_HP] {name} PIN active jobLv={jobLevel} cap={levelCap} ceiling={client.ProtectedRecoverableHpCeiling}");
            }
            else
            {
                ClearProtectionState(client);
                if (_signal.TryRemove(name, out _))
                {
                    Logger.Info($"[RECOVERABLE_HP] {name} PIN cleared jobLv={jobLevel} cap={levelCap}");
                }
            }

            WriteSignalFile();
        }

        /// <summary>
        /// After RpcCtrlPeriodicTop copies client HP into the character model, raise recoverable HP to the session ceiling.
        /// </summary>
        public void ClampPeriodicUpdate(GameClient client)
        {
            if (client?.Character == null)
            {
                return;
            }

            ClampPeriodicUpdate(client, client.Character);
        }

        public void ClampPeriodicUpdate(Character? character)
        {
            // RpcHandler always passes the owning client; this overload exists for safety only.
            if (character == null)
            {
                return;
            }
        }

        private void ClampPeriodicUpdate(GameClient client, Character character)
        {
            ushort ceiling = client.ProtectedRecoverableHpCeiling;
            ClampPeriodicUpdate(character, ref ceiling);
            client.ProtectedRecoverableHpCeiling = ceiling;
        }

        internal void ClampPeriodicUpdate(Character character, ref ushort ceiling)
        {
            uint green = character.GreenHp;
            uint white = character.WhiteHp;

            // Death: accept client values and drop the session ceiling so respawn can rebuild it.
            if (green == 0 || white == 0)
            {
                ceiling = 0;
                return;
            }

            if (!ShouldPin(character))
            {
                return;
            }

            ushort protectedWhite = GetProtectedWhiteHp(ref ceiling, ToPacketHp(green), ToPacketHp(white));

            // Only raise the recoverable cap (loss-gauge ceiling), not current green HP.
            character.WhiteHp = protectedWhite;
            character.StatusInfo.WhiteHP = protectedWhite;
        }

        /// <summary>
        /// Rewrite the periodic RPC white HP from the session ceiling and return whether the packet was modified.
        /// </summary>
        public bool TryPatchPeriodicRpc(GameClient client, byte[] rpcData)
        {
            if (client?.Character == null || rpcData == null || !ShouldPin(client.Character))
            {
                return false;
            }

            ushort ceiling = client.ProtectedRecoverableHpCeiling;
            bool patched = TryPatchPeriodicRpc(client.Character, ref ceiling, rpcData);
            client.ProtectedRecoverableHpCeiling = ceiling;
            return patched;
        }

        internal bool TryPatchPeriodicRpc(Character character, ref ushort ceiling, byte[] rpcData)
        {
            if (!RpcCtrlPeriodicTop.TryReadPeriodicTopHp(rpcData, out ushort greenHp, out ushort whiteHp))
            {
                return false;
            }

            ushort protectedWhite = GetProtectedWhiteHp(ref ceiling, greenHp, whiteHp);
            if (protectedWhite <= whiteHp)
            {
                return false;
            }

            if (!RpcCtrlPeriodicTop.TryWriteWhiteHp(rpcData, protectedWhite))
            {
                return false;
            }

            Logger.Info(
                $"[RECOVERABLE_HP] {CharacterName(character)} patched periodic WhiteHP {whiteHp} -> {protectedWhite} ceiling={ceiling}");
            return true;
        }

        /// <summary>
        /// Normalize recoverable HP before persisting logout state for sub-cap characters.
        /// </summary>
        public void NormalizeForSave(Character character)
        {
            if (!ShouldPin(character) || character.GreenHp == 0 || character.WhiteHp == 0)
            {
                return;
            }

            uint normalized = Math.Max(character.WhiteHp, character.GreenHp);
            character.WhiteHp = normalized;
            character.GreenHp = normalized;
            character.StatusInfo.WhiteHP = normalized;
            character.StatusInfo.HP = normalized;
        }

        public void OnCharacterDisconnected(GameClient client)
        {
            if (client?.Character == null)
            {
                return;
            }

            ClearProtectionState(client);
            _signal.TryRemove(CharacterName(client.Character), out _);
            WriteSignalFile();
        }

        private void SeedSessionCeiling(GameClient client, Character character)
        {
            ushort ceiling = client.ProtectedRecoverableHpCeiling;
            SeedSessionCeiling(character, ref ceiling);
            client.ProtectedRecoverableHpCeiling = ceiling;
        }

        private static void SeedSessionCeiling(Character character, ref ushort ceiling)
        {
            uint seed = Math.Max(SanitizeHp(character.GreenHp), SanitizeHp(character.WhiteHp));
            seed = Math.Max(seed, SanitizeHp(character.StatusInfo.HP));
            seed = Math.Max(seed, SanitizeHp(character.StatusInfo.WhiteHP));
            if (seed == 0)
            {
                return;
            }

            ushort seedHp = ToPacketHp(seed);
            GetProtectedWhiteHp(ref ceiling, seedHp, seedHp);
        }

        /// <summary>
        /// Combat mirrors start at 0 until the first periodic RPC; status info is loaded from DB on login.
        /// </summary>
        private static void SyncCombatHpFromStatus(Character character)
        {
            uint statusHp = SanitizeHp(character.StatusInfo.HP);
            if (character.GreenHp == 0 && statusHp > 0)
            {
                character.GreenHp = statusHp;
            }

            uint statusWhite = SanitizeHp(character.StatusInfo.WhiteHP);
            if (character.WhiteHp == 0 && statusWhite > 0)
            {
                character.WhiteHp = statusWhite;
            }
        }

        private static uint SanitizeHp(uint hp)
        {
            // RestoreFullVitals uses uint.MaxValue; never treat that as a real combat value.
            return hp >= 100_000 ? 0 : hp;
        }

        private static ushort ToPacketHp(uint hp)
        {
            return (ushort)Math.Min(hp, ushort.MaxValue);
        }

        private static string CharacterName(Character character)
        {
            return $"{character.FirstName} {character.LastName}";
        }

        private void WriteSignalFile()
        {
            try
            {
                Dictionary<string, RecoverableHpSignalEntry> snapshot = new(_signal);
                string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

                lock (_fileLock)
                {
                    string? dir = Path.GetDirectoryName(_signalPath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllText(_signalPath, json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[RECOVERABLE_HP] failed to write signal file '{_signalPath}': {ex.Message}");
            }
        }
    }
}

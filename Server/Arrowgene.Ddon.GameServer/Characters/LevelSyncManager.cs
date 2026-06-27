#nullable enable
using Arrowgene.Ddon.GameServer;
using Arrowgene.Ddon.GameServer.GatheringItems;
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
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
    /// Per-zone level sync.
    ///
    /// Background: DDON's combat power is computed and stored CLIENT-SIDE and the client ignores any
    /// mid-session server attempt to lower it (job-level-up notices, character-status reloads, etc.).
    /// Reverse engineering confirmed the only thing that actually reduces a player's damage is writing
    /// the player's live combat-attack value in the game's memory directly. So combat down-scaling is
    /// performed by a small client-side "sync applier" running on the player's machine, and the server's
    /// job here is simply to TELL that applier how much to scale, per character.
    ///
    /// Combat stats and the real level/EXP are never mutated on the server: the stored level, EXP total,
    /// gear/equip requirements and database writes always use the true values. The server only:
    ///   1. Decides whether the stage a player entered has a recommended level below their real level.
    ///   2. Computes attack scale factors (ratio of level-scaled base stats at the recommended vs. real
    ///      level) and writes them to a signal file the applier polls (this performs the combat reduction).
        ///   3. Optionally REPAINTS the HUD job-level number to the recommended level for immersion (so an
        ///      over-leveled player doesn't see "Lv.60" while doing scaled damage to low-level mobs), anchoring
        ///      the EXP bar to their real progress mapped into the displayed band, and restores the real level
        ///      number when they leave. This is display-only and is toggled by
        ///      <see cref="Arrowgene.Ddon.Server.Settings.GameServerSettings.LevelSyncDisplayRecommendedLevel"/>.
        ///   4. When <see cref="Arrowgene.Ddon.Server.Settings.GameServerSettings.LevelSyncBroadcastDisplayToOthers"/>
        ///      is enabled, broadcasts the same display level and scaled offensive stats to party members and
        ///      serves scaled values on profile/inspect requests while the character is online in a synced zone.
        ///   5. When <see cref="Arrowgene.Ddon.Server.Settings.GameServerSettings.LevelSyncUseDisplayLevelForExp"/>
        ///      is enabled, party EXP penalties and per-kill EXP level comparisons treat over-leveled members
        ///      who are in the synced stage as the zone's recommended level (so mixed-level parties can farm
        ///      low sync dungeons together without a spread penalty).
    ///
    /// The signal file is JSON keyed by the in-game character name ("First Last"), which the applier
    /// resolves by reading the name from the game's memory. Path is overridable via the DDON_SYNC_FILE
    /// environment variable (default <c>D:\DDON\client-mod\sync\sync_state.json</c>).
    /// </summary>
    public class LevelSyncManager
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(LevelSyncManager));

        private readonly DdonGameServer _server;

        /// <summary>Think tables written by earlier level-sync builds that broke world-spawn AI.</summary>
        private static bool IsLegacySyncThinkTbl(byte thinkTbl) => thinkTbl is 1 or 8;

        // Currently-synced characters, keyed by CharacterCommon.CommonId (used for IsSynced()).
        private readonly ConcurrentDictionary<uint, byte> _syncedCharacters = new();

        // Active sync parameters keyed by CommonId (for party/profile display without a name lookup).
        private readonly ConcurrentDictionary<uint, SyncSignalEntry> _syncByCommonId = new();

        // Current signal entries keyed by in-game character name ("First Last").
        private readonly ConcurrentDictionary<string, SyncSignalEntry> _signal = new();

        private readonly object _fileLock = new();
        private readonly string _signalPath;

        public LevelSyncManager(DdonGameServer server)
        {
            _server = server;
            _signalPath = Environment.GetEnvironmentVariable("DDON_SYNC_FILE")
                          ?? Path.Combine("D:", "DDON", "client-mod", "sync", "sync_state.json");
        }

        private class SyncSignalEntry
        {
            public bool Synced { get; set; }
            public double PhysFactor { get; set; }
            public double MagFactor { get; set; }
            public uint RecLevel { get; set; }
            public uint TrueLevel { get; set; }
            public uint GearTier { get; set; }
            public uint EffectiveTier { get; set; }
            public string Job { get; set; } = string.Empty;
        }

        /// <summary>Read-only view of an online character's active level-sync state.</summary>
        public readonly struct ActiveSyncState
        {
            public ActiveSyncState(double physFactor, double magFactor, uint recLevel, uint trueLevel)
            {
                PhysFactor = physFactor;
                MagFactor = magFactor;
                RecLevel = recLevel;
                TrueLevel = trueLevel;
            }

            public double PhysFactor { get; }
            public double MagFactor { get; }
            public uint RecLevel { get; }
            public uint TrueLevel { get; }
        }

        public readonly struct SafeUpdateToken
        {
            public readonly bool WasSynced;
            public readonly uint RealLevelBefore;

            public SafeUpdateToken(bool wasSynced, uint realLevelBefore)
            {
                WasSynced = wasSynced;
                RealLevelBefore = realLevelBefore;
            }
        }

        /// <summary>
        /// Returns the recommended level for a stage, or 0 if the stage has no recommended level (so no sync).
        ///
        /// Resolution order:
        ///   1. An explicit per-stage entry in <see cref="Arrowgene.Ddon.Server.Settings.GameServerSettings.StageRecommendedLevels"/>
        ///      always wins (keyed by StageId). This lets an admin override or disable (value 0) sync for a stage.
        ///   2. Otherwise the value baked from the client's stage list (<see cref="StageRecommendedLevelTable"/>,
        ///      keyed by StageNo), but ONLY for dungeon/recommended-level content. Town hubs and open-world
        ///      areas are excluded so power-leveling in the field (and normal life in towns) is never synced.
        /// </summary>
        public uint GetRecommendedLevel(uint stageId)
        {
            var map = _server.GameSettings.GameServerSettings.StageRecommendedLevels;
            if (map != null && map.TryGetValue(stageId, out uint overrideLevel))
            {
                return overrideLevel;
            }

            if (!StageManager.IsDungeon(stageId))
            {
                return 0;
            }

            uint stageNo = StageManager.ConvertIdToStageNo(stageId);
            if (StageRecommendedLevelTable.ByStageNo.TryGetValue(stageNo, out byte baked))
            {
                return baked;
            }

            return 0;
        }

        public bool IsSynced(uint commonId)
        {
            return _syncedCharacters.ContainsKey(commonId);
        }

        /// <summary>
        /// Job level used for EXP spread penalties and automatic per-kill EXP level comparisons.
        /// When <see cref="Arrowgene.Ddon.Server.Settings.GameServerSettings.LevelSyncUseDisplayLevelForExp"/>
        /// is enabled, a member in a recommended-level stage who is above that cap counts as the recommended
        /// level. Members outside the kill stage keep their real level so hub players do not skew dungeon EXP.
        /// </summary>
        public uint GetEffectiveJobLevelForExp(CharacterCommon character, uint stageId)
        {
            if (character?.ActiveCharacterJobData == null)
            {
                return 0;
            }

            uint realLevel = character.ActiveCharacterJobData.Lv;
            if (!_server.GameSettings.GameServerSettings.LevelSyncUseDisplayLevelForExp)
            {
                return realLevel;
            }

            uint recommendedLevel = GetRecommendedLevel(stageId);
            if (recommendedLevel == 0 || realLevel <= recommendedLevel)
            {
                return realLevel;
            }

            if (character.Stage.Id != stageId)
            {
                return realLevel;
            }

            return recommendedLevel;
        }

        /// <summary>
        /// Returns true when <paramref name="character"/> is online and actively combat-synced
        /// (real level above the current zone's recommended level).
        /// </summary>
        public bool TryGetActiveSync(CharacterCommon character, out ActiveSyncState state)
        {
            state = default;
            if (character == null || !_syncByCommonId.TryGetValue(character.CommonId, out SyncSignalEntry? entry) || !entry.Synced)
            {
                return false;
            }

            state = new ActiveSyncState(entry.PhysFactor, entry.MagFactor, entry.RecLevel, entry.TrueLevel);
            return true;
        }

        /// <summary>
        /// Builds a display-only copy of job data for party list, inspect, and profile packets.
        /// Real persisted job data on the server is never modified.
        /// </summary>
        public CDataCharacterJobData CreateDisplayJobData(CDataCharacterJobData source, ActiveSyncState sync, GameMode gameMode)
        {
            uint displayExp = MapDisplayExp(source, sync.RecLevel, sync.TrueLevel, gameMode);
            return new CDataCharacterJobData
            {
                Job = source.Job,
                Exp = displayExp,
                JobPoint = source.JobPoint,
                Lv = sync.RecLevel,
                Atk = ScaleU16(source.Atk, sync.PhysFactor),
                Def = source.Def,
                MAtk = ScaleU16(source.MAtk, sync.MagFactor),
                MDef = source.MDef,
                Strength = source.Strength,
                DownPower = ScaleU16(source.DownPower, sync.PhysFactor),
                ShakePower = ScaleU16(source.ShakePower, sync.PhysFactor),
                StunPower = ScaleU16(source.StunPower, sync.PhysFactor),
                Constitution = source.Constitution,
                Guts = source.Guts,
                FireResist = source.FireResist,
                IceResist = source.IceResist,
                ThunderResist = source.ThunderResist,
                HolyResist = source.HolyResist,
                DarkResist = source.DarkResist,
                SpreadResist = source.SpreadResist,
                FreezeResist = source.FreezeResist,
                ShockResist = source.ShockResist,
                AbsorbResist = source.AbsorbResist,
                DarkElmResist = source.DarkElmResist,
                PoisonResist = source.PoisonResist,
                SlowResist = source.SlowResist,
                SleepResist = source.SleepResist,
                StunResist = source.StunResist,
                WetResist = source.WetResist,
                OilResist = source.OilResist,
                SealResist = source.SealResist,
                CurseResist = source.CurseResist,
                SoftResist = source.SoftResist,
                StoneResist = source.StoneResist,
                GoldResist = source.GoldResist,
                FireReduceResist = source.FireReduceResist,
                IceReduceResist = source.IceReduceResist,
                ThunderReduceResist = source.ThunderReduceResist,
                HolyReduceResist = source.HolyReduceResist,
                DarkReduceResist = source.DarkReduceResist,
                AtkDownResist = source.AtkDownResist,
                DefDownResist = source.DefDownResist,
                MAtkDownResist = source.MAtkDownResist,
                MDefDownResist = source.MDefDownResist,
            };
        }

        /// <summary>Display level param for party level notices while synced.</summary>
        public CDataCharacterLevelParam CreateDisplayLevelParam(CharacterCommon character, ActiveSyncState sync)
        {
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return character.CDataCharacterLevelParam;
            }

            return new CDataCharacterLevelParam
            {
                Attack = ScaleU16(jobData.Atk, sync.PhysFactor),
                MagAttack = ScaleU16(jobData.MAtk, sync.MagFactor),
                Defence = jobData.Def,
                MagDefence = jobData.MDef,
                Strength = jobData.Strength,
                DownPower = ScaleU16(jobData.DownPower, sync.PhysFactor),
                ShakePower = ScaleU16(jobData.ShakePower, sync.PhysFactor),
                StunPower = ScaleU16(jobData.StunPower, sync.PhysFactor),
                Constitution = jobData.Constitution,
                Guts = jobData.Guts,
            };
        }

        /// <summary>Overlays synced level/stats onto a party context player info block.</summary>
        public void ApplyDisplayToContextPlayerInfo(CDataContextPlayerInfo info, Character character, ActiveSyncState sync, GameMode gameMode)
        {
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return;
            }

            info.Lv = (ushort)sync.RecLevel;
            info.Exp = MapDisplayExp(jobData, sync.RecLevel, sync.TrueLevel, gameMode);
            info.Atk = ScaleU32(info.Atk, sync.PhysFactor);
            info.MAtk = ScaleU32(info.MAtk, sync.MagFactor);
            info.DownPower = ScaleU32(info.DownPower, sync.PhysFactor);
            info.ShakePower = ScaleU32(info.ShakePower, sync.PhysFactor);
            info.StunPower = ScaleU32(info.StunPower, sync.PhysFactor);
            info.GainAttack = ScaleU32(info.GainAttack, sync.PhysFactor);
            info.GainMagicAttack = ScaleU32(info.GainMagicAttack, sync.MagFactor);

            foreach (CDataContextJobData job in info.JobList)
            {
                if (job.Job == character.Job)
                {
                    job.Lv = (ushort)sync.RecLevel;
                    job.Exp = info.Exp;
                }
            }
        }

        /// <summary>Overlays synced active-job level onto a party list element.</summary>
        public CDataCharacterListElement ApplyDisplayToListElement(CDataCharacterListElement element, Character character, ActiveSyncState sync)
        {
            element.CurrentJobBaseInfo = new CDataJobBaseInfo
            {
                Job = character.Job,
                Level = (byte)sync.RecLevel
            };
            element.EntryJobBaseInfo = new CDataJobBaseInfo
            {
                Job = character.Job,
                Level = (byte)sync.RecLevel
            };
            return element;
        }

        /// <summary>
        /// When the stage has a recommended level, caps spawned enemy level to that value.
        /// Does not set <see cref="InstancedEnemy.IsManualSet"/>: on this client, manual set on world layout
        /// spawns produces ornamental mobs with no AI or collision even when a think table is supplied.
        /// The client auto-scale path (IsManualSet=false, StartThinkTblNo=0) must stay active.
        /// Over-leveled parties may still see the client upscale mobs toward the leader's level; player combat
        /// sync via the applier handles the other half of fairness.
        /// </summary>
        public bool TryApplyEnemyLevelSync(GameClient client, StageLayoutId stageLayoutId, InstancedEnemy enemy)
        {
            if (!_server.GameSettings.GameServerSettings.LevelSyncEnemyLevels)
            {
                return false;
            }

            if (enemy.QuestScheduleId != 0 || enemy.IsAreaBoss)
            {
                return false;
            }

            if (enemy.IsBossGauge && !_server.GameSettings.GameServerSettings.LevelSyncBossLevels)
            {
                return false;
            }

            uint recommendedLevel = GetRecommendedLevel(stageLayoutId.Id);
            if (recommendedLevel == 0)
            {
                return false;
            }

            ushort cappedLevel = enemy.Lv > 0
                ? (ushort)Math.Min(enemy.Lv, recommendedLevel)
                : (ushort)recommendedLevel;

            bool needsLevelCap = enemy.Lv != cappedLevel;
            bool needsAutoPath = enemy.IsManualSet || IsLegacySyncThinkTbl(enemy.StartThinkTblNo);
            if (!needsLevelCap && !needsAutoPath)
            {
                return false;
            }

            Character leaderCharacter = client.Party?.Leader?.Client?.Character ?? client.Character;
            uint leaderLevel = leaderCharacter?.ActiveCharacterJobData?.Lv ?? 0;

            ushort previousLevel = enemy.Lv;
            bool previousManualSet = enemy.IsManualSet;
            byte previousThinkTbl = enemy.StartThinkTblNo;

            if (needsLevelCap)
            {
                enemy.Lv = cappedLevel;
            }

            if (enemy.IsManualSet)
            {
                enemy.IsManualSet = false;
            }

            if (IsLegacySyncThinkTbl(enemy.StartThinkTblNo))
            {
                enemy.StartThinkTblNo = 0;
            }

            Logger.Info(
                $"[LEVELSYNC] enemy cap stage={stageLayoutId.Id} idx={enemy.Index} " +
                $"enemyId={enemy.EnemyId} lv {previousLevel}->{enemy.Lv} " +
                $"manualSet={previousManualSet}->false thinkTbl={previousThinkTbl}->{enemy.StartThinkTblNo} " +
                $"leaderLv={leaderLevel} recLv={recommendedLevel}");
            return true;
        }

        /// <summary>
        /// Clears cached instance enemies for a stage so re-entry picks up fresh spawn data
        /// (including level-sync caps) instead of reusing mobs scaled before sync was applied.
        /// </summary>
        public void ResetSyncedStageEnemies(GameClient client, uint stageId)
        {
            if (GetRecommendedLevel(stageId) == 0)
            {
                return;
            }

            client.Party.InstanceEnemyManager.ResetAllLayoutsForStage(stageId);
            Logger.Info($"[LEVELSYNC] cleared enemy cache for stage={stageId} recLv={GetRecommendedLevel(stageId)}");
        }

        /// <summary>
        /// Public view of a character's current sync signal, for the network endpoint the remote applier polls.
        /// Returns a not-synced default when the character has no active sync.
        /// </summary>
        public class SyncSignalView
        {
            public bool Synced { get; set; }
            public double PhysFactor { get; set; } = 1.0;
            public double MagFactor { get; set; } = 1.0;
            public uint RecLevel { get; set; }
            public uint TrueLevel { get; set; }
            public uint GearTier { get; set; }
            public uint EffectiveTier { get; set; }
            public string Job { get; set; } = string.Empty;
            public bool PinRecoverableHp { get; set; }
            public uint RecoverableHpJobLevel { get; set; }
            public uint RecoverableHpJobLevelMax { get; set; }
        }

        public SyncSignalView GetSignalView(string name)
        {
            if (!string.IsNullOrEmpty(name) && _signal.TryGetValue(name, out SyncSignalEntry? e))
            {
                return new SyncSignalView
                {
                    Synced = e.Synced,
                    PhysFactor = e.PhysFactor,
                    MagFactor = e.MagFactor,
                    RecLevel = e.RecLevel,
                    TrueLevel = e.TrueLevel,
                    GearTier = e.GearTier,
                    EffectiveTier = e.EffectiveTier,
                    Job = e.Job
                };
            }
            return new SyncSignalView();
        }

        /// <summary>
        /// Highest combat-relevant tier among performance equipment (max of item level and IR).
        /// Items like Hidden Seiðr (LV 1, IR 55) resolve to 55, not 1.
        /// </summary>
        private uint GetMaxEquippedCombatTier(Character character)
        {
            uint maxTier = 0;

            foreach (Item? item in character.Equipment.GetItems(EquipType.Performance))
            {
                if (item == null)
                {
                    continue;
                }

                if (!_server.AssetRepository.ClientItemInfos.TryGetValue((ItemId)item.ItemId, out ClientItemInfo? info))
                {
                    continue;
                }

                if (!ExplorationProgressionCatalog.IsPerformanceCombatGear(info))
                {
                    continue;
                }

                uint tier = ExplorationProgressionCatalog.GetEquipLevel(info);
                if (tier > maxTier)
                {
                    maxTier = tier;
                }
            }

            return maxTier;
        }

        /// <summary>
        /// Evaluates and (re)applies or clears level sync for the player based on the stage they just entered,
        /// then refreshes the signal file. Returns an empty queue (no client packets are needed: the client-side
        /// applier performs the actual combat down-scaling).
        /// </summary>
        public PacketQueue HandleStageChange(GameClient client, uint stageId)
        {
            PacketQueue queue = new();

            if (client?.Character == null)
            {
                return queue;
            }

            uint recommendedLevel = GetRecommendedLevel(stageId);
            ResetSyncedStageEnemies(client, stageId);
            EvaluatePlayer(client, recommendedLevel, queue);

            return queue;
        }

        private void EvaluatePlayer(GameClient client, uint recommendedLevel, PacketQueue queue)
        {
            Character character = client.Character;
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return;
            }

            uint realLevel = jobData.Lv;
            bool shouldSync = recommendedLevel > 0 && realLevel > recommendedLevel;
            string name = $"{character.FirstName} {character.LastName}";
            bool wasSynced = _syncedCharacters.ContainsKey(character.CommonId);

            if (shouldSync)
            {
                var baseTrue = ExpManager.CalculateBaseStats(jobData.Job, realLevel);
                var baseRec = ExpManager.CalculateBaseStats(jobData.Job, recommendedLevel);
                bool gearAware = _server.GameSettings.GameServerSettings.LevelSyncGearAwareScaling;
                uint gearTier = GetMaxEquippedCombatTier(character);
                uint effectiveTier = Math.Max(realLevel, gearTier);
                if (effectiveTier < recommendedLevel)
                {
                    effectiveTier = recommendedLevel;
                }

                double tierRatio = recommendedLevel / (double)effectiveTier;
                if (tierRatio > 1.0)
                {
                    tierRatio = 1.0;
                }

                double physBaseRatio = baseTrue.Atk > 0 ? (double)baseRec.Atk / baseTrue.Atk : 1.0;
                double magBaseRatio = baseTrue.MAtk > 0 ? (double)baseRec.MAtk / baseTrue.MAtk : 1.0;
                bool physTierLimited = gearAware && tierRatio < physBaseRatio;
                bool magTierLimited = gearAware && tierRatio < magBaseRatio;
                double physFactor = physTierLimited ? ClampFactor(tierRatio) : ShapeFactor(physBaseRatio);
                double magFactor = magTierLimited ? ClampFactor(tierRatio) : ShapeFactor(magBaseRatio);

                _syncedCharacters[character.CommonId] = 1;
                _syncByCommonId[character.CommonId] = new SyncSignalEntry
                {
                    Synced = true,
                    PhysFactor = physFactor,
                    MagFactor = magFactor,
                    RecLevel = recommendedLevel,
                    TrueLevel = realLevel,
                    GearTier = gearTier,
                    EffectiveTier = effectiveTier,
                    Job = jobData.Job.ToString()
                };
                _signal[name] = _syncByCommonId[character.CommonId];

                Logger.Info(
                    $"[LEVELSYNC] {name} SYNC realLv={realLevel} recLv={recommendedLevel} gearTier={gearTier} " +
                    $"effectiveTier={effectiveTier} tierRatio={tierRatio:0.000} gearAware={gearAware} " +
                    $"physFactor={physFactor:0.000} magFactor={magFactor:0.000}");

                // Repaint the HUD job level to the recommended level (display-only).
                EnqueueSyncPresentation(client, character, jobData, recommendedLevel, queue);
            }
            else if (recommendedLevel > 0)
            {
                _syncedCharacters.TryRemove(character.CommonId, out _);
                _syncByCommonId.TryRemove(character.CommonId, out _);
                _signal[name] = new SyncSignalEntry
                {
                    Synced = false,
                    PhysFactor = 1.0,
                    MagFactor = 1.0,
                    RecLevel = recommendedLevel,
                    TrueLevel = realLevel,
                    Job = jobData.Job.ToString()
                };

                Logger.Info($"[LEVELSYNC] {name} zone recLv={recommendedLevel} realLv={realLevel} (enemy cap active, no player combat sync)");

                if (wasSynced)
                {
                    EnqueueSyncPresentation(client, character, jobData, realLevel, queue);
                }
            }
            else
            {
                _syncedCharacters.TryRemove(character.CommonId, out _);
                _syncByCommonId.TryRemove(character.CommonId, out _);
                if (_signal.TryRemove(name, out _))
                {
                    Logger.Info($"[LEVELSYNC] {name} UNSYNC (recLv={recommendedLevel} realLv={realLevel})");
                }

                // If we previously repainted the HUD to a lower level, restore the real level number now.
                if (wasSynced)
                {
                    EnqueueSyncPresentation(client, character, jobData, realLevel, queue);
                }
            }

            WriteSignalFile();
        }

        /// <summary>
        /// Repaints the synced player's local HUD and, when enabled, broadcasts display level/stats
        /// to party members and pushes a fresh party context snapshot.
        /// </summary>
        private void EnqueueSyncPresentation(GameClient client, Character character, CDataCharacterJobData jobData, uint displayLevel, PacketQueue queue)
        {
            EnqueueDisplayLevel(client, jobData, displayLevel, queue);

            if (!_server.GameSettings.GameServerSettings.LevelSyncBroadcastDisplayToOthers)
            {
                return;
            }

            if (client.Party == null)
            {
                return;
            }

            client.Party.EnqueueToAllExcept(new S2CJobCharacterJobLevelUpMemberNtc
            {
                CharacterId = character.CharacterId,
                Job = jobData.Job,
                Level = displayLevel,
                CharacterLevelParam = TryGetActiveSync(character, out ActiveSyncState s)
                    ? CreateDisplayLevelParam(character, s)
                    : character.CDataCharacterLevelParam
            }, queue, client);

            if (client.Party.GetPartyMemberByCharacter(character) is PlayerPartyMember playerMember)
            {
                client.Party.EnqueueToAll(playerMember.GetPartyContext(), queue);
            }

            Logger.Info($"[LEVELSYNC] broadcast display lv={displayLevel} to party for {character.FirstName} {character.LastName}");
        }

        private uint MapDisplayExp(CDataCharacterJobData jobData, uint displayLevel, uint trueLevel, GameMode gameMode)
        {
            double pct = 0.0;
            uint realBase = ExpManager.TotalExpToLevelUpTo(trueLevel, gameMode);
            uint realNext = ExpManager.TotalExpToLevelUpTo(trueLevel + 1, gameMode);
            if (realNext > realBase)
            {
                double into = (double)jobData.Exp - realBase;
                pct = into / (realNext - realBase);
                if (pct < 0.0) pct = 0.0;
                if (pct > 1.0) pct = 1.0;
            }

            uint dispBase = ExpManager.TotalExpToLevelUpTo(displayLevel, gameMode);
            uint dispNext = ExpManager.TotalExpToLevelUpTo(displayLevel + 1, gameMode);
            return (dispNext > dispBase)
                ? dispBase + (uint)Math.Round(pct * (dispNext - dispBase))
                : dispBase;
        }

        private static ushort ScaleU16(ushort value, double factor)
        {
            if (value == 0)
            {
                return 0;
            }

            return (ushort)Math.Max(1, Math.Min(ushort.MaxValue, Math.Round(value * factor)));
        }

        private static uint ScaleU32(uint value, double factor)
        {
            if (value == 0)
            {
                return 0;
            }

            return (uint)Math.Max(1, Math.Min(uint.MaxValue, Math.Round(value * factor)));
        }

        /// <summary>
        /// Repaints the client's HUD job-level number to <paramref name="displayLevel"/> and anchors the EXP bar so it
        /// reflects the player's REAL progress toward their real next level (mapped into the displayed level's band).
        ///
        /// This is purely cosmetic. The server's stored level/EXP (<paramref name="jobData"/>) are never changed, so
        /// progression, gear requirements, and database writes always use the true values. Combat down-scaling is still
        /// done by the client-side applier; this notice does not (and cannot reliably) change client combat power.
        /// </summary>
        private void EnqueueDisplayLevel(GameClient client, CDataCharacterJobData jobData, uint displayLevel, PacketQueue queue)
        {
            if (!_server.GameSettings.GameServerSettings.LevelSyncDisplayRecommendedLevel)
            {
                return;
            }

            var gameMode = client.GameMode;
            uint realLevel = jobData.Lv;
            uint dispExp = MapDisplayExp(jobData, displayLevel, realLevel, gameMode);

            // Repaint the HUD level number. AddJobPoint = 0 so no JP popup; TotalJobPoint stays real.
            client.Enqueue(new S2CJobCharacterJobLevelUpNtc
            {
                Job = jobData.Job,
                Level = displayLevel,
                AddJobPoint = 0,
                TotalJobPoint = jobData.JobPoint,
                CharacterLevelParam = client.Character.CDataCharacterLevelParam
            }, queue);

            // Anchor the EXP bar within the displayed band (AddExp = 0 => no gain animation).
            client.Enqueue(new S2CJobCharacterJobExpUpNtc
            {
                JobId = jobData.Job,
                AddExp = 0,
                ExtraBonusExp = 0,
                TotalExp = dispExp,
                Type = 0
            }, queue);
        }

        /// <summary>
        /// Clamps a raw ratio to [MinFactor, 1.0] without applying the exponent. Used when equipped gear
        /// tier (not job level alone) is the limiting factor, so IR 55 weapons in a Lv 4 zone scale linearly.
        /// </summary>
        private double ClampFactor(double ratio)
        {
            var settings = _server.GameSettings.GameServerSettings;
            double minFactor = settings.LevelSyncMinAttackFactor;

            if (ratio < minFactor) return minFactor;
            if (ratio > 1.0) return 1.0;
            return ratio;
        }

        /// <summary>
        /// Applies the configured exponent to the raw base-stat ratio and clamps it to [MinFactor, 1.0].
        /// </summary>
        private double ShapeFactor(double ratio)
        {
            var settings = _server.GameSettings.GameServerSettings;
            double minFactor = settings.LevelSyncMinAttackFactor;
            double exponent = settings.LevelSyncAttackFactorExponent;

            if (ratio < 0.0) ratio = 0.0;
            if (ratio > 1.0) ratio = 1.0;

            double f = (exponent == 1.0) ? ratio : Math.Pow(ratio, exponent);

            if (f < minFactor) return minFactor;
            if (f > 1.0) return 1.0;
            return f;
        }

        private void WriteSignalFile()
        {
            try
            {
                // Snapshot the current synced entries.
                Dictionary<string, SyncSignalEntry> snapshot = new(_signal);
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
                Logger.Error($"[LEVELSYNC] failed to write sync signal file '{_signalPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Retained for API compatibility with <see cref="ExpManager"/>. The server no longer mutates combat
        /// stats for level sync (the client-side applier does the scaling), so the live job data always holds
        /// the real values and EXP/JP math and database writes are already correct. These are now no-ops.
        /// </summary>
        public SafeUpdateToken BeginPersistenceSafeUpdate(CharacterCommon character)
        {
            return new SafeUpdateToken(false, 0);
        }

        /// <summary>
        /// Retained for API compatibility with <see cref="ExpManager"/>. See <see cref="BeginPersistenceSafeUpdate"/>.
        /// </summary>
        public void EndPersistenceSafeUpdate(GameClient client, CharacterCommon character, SafeUpdateToken token, PacketQueue queue)
        {
            // No-op: real stats are always live now.
        }
    }
}

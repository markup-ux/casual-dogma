#nullable enable
using Arrowgene.Ddon.GameServer.Party;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Collections.Concurrent;

namespace Arrowgene.Ddon.GameServer.Characters
{
    /// <summary>
    /// Implements per-zone level sync. When a player (and their pawns) enters a stage that has a recommended level
    /// (auto-detected from client data, see <see cref="GetRecommendedLevel"/>) and their current job level is HIGHER
    /// than that recommended level, their combat is balanced down to match the zone: their base combat stats
    /// (physical/magick attack and defense) are temporarily reduced to the recommended level's values so fights feel
    /// fair. Stats are restored when they leave for a zone with no (or a high enough) recommended level.
    ///
        /// Nothing else is faked on the server. The real job level, EXP total, gear requirements, and database
        /// writes always use the true values. The client only applies combat-stat changes from JobLevelUp when
        /// the notice level changes, so sync notices use the zone recommended level (with reduced stats), then
        /// a real EXP refresh, then the recommended level again to re-apply stats after the EXP notice resets
        /// them. A party-context refresh carries the real Lv/Exp for the HUD without undoing combat sync.
    ///
    /// Only the combat stats are altered in memory (so shared-world context stays consistent), and the database is
    /// protected: the two combat paths that persist job data while adventuring (<see cref="ExpManager.AddExp"/> and
    /// <see cref="ExpManager.AddJp"/>) cooperate via <see cref="BeginPersistenceSafeUpdate"/> and
    /// <see cref="EndPersistenceSafeUpdate"/> so the real stats are what gets written.
    /// </summary>
    public class LevelSyncManager
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(LevelSyncManager));

        private readonly DdonGameServer _server;

        // Keyed by CharacterCommon.CommonId (works for both players and pawns).
        private readonly ConcurrentDictionary<uint, SyncState> _syncedCharacters = new();

        public LevelSyncManager(DdonGameServer server)
        {
            _server = server;
        }

        private class SyncState
        {
            public JobId Job;
            public uint RecommendedLevel;
            // The real (un-reduced) base combat stats, used to restore on exit and to persist correctly.
            public ushort RealAtk;
            public ushort RealDef;
            public ushort RealMAtk;
            public ushort RealMDef;
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
        ///      keyed by StageNo) is used, but ONLY for dungeon/recommended-level content. Town hubs and open-world
        ///      areas are excluded so power-leveling out in the field (and normal life in towns) is never synced,
        ///      even though some of those stages technically carry a recommended level in client data.
        /// </summary>
        public uint GetRecommendedLevel(uint stageId)
        {
            // 1. Explicit settings override (also allows disabling a specific stage by mapping it to 0).
            var map = _server.GameSettings.GameServerSettings.StageRecommendedLevels;
            if (map != null && map.TryGetValue(stageId, out uint overrideLevel))
            {
                return overrideLevel;
            }

            // 2. Auto-detected recommended level from client data, gated to dungeons only.
            //    IsDungeon excludes safe areas (towns/hubs) and the open-world field maps.
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
        /// Evaluates and (re)applies or removes level sync for the player and their pawns based on the stage
        /// they just entered. Returns the notices to inform clients of any level/stat changes.
        /// </summary>
        public PacketQueue HandleStageChange(GameClient client, uint stageId)
        {
            PacketQueue queue = new();

            if (client?.Character == null)
            {
                return queue;
            }

            uint recommendedLevel = GetRecommendedLevel(stageId);

            EvaluateCharacter(client, client.Character, recommendedLevel, queue);
            foreach (var pawn in client.Character.Pawns)
            {
                EvaluateCharacter(client, pawn, recommendedLevel, queue);
            }

            return queue;
        }

        private void EvaluateCharacter(GameClient client, CharacterCommon character, uint recommendedLevel, PacketQueue queue)
        {
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return;
            }

            uint commonId = character.CommonId;
            bool currentlySynced = _syncedCharacters.TryGetValue(commonId, out SyncState? state);

            // The actual level is never altered, so the live job data always holds the real level.
            uint realLevel = jobData.Lv;

            bool shouldSync = recommendedLevel > 0 && realLevel > recommendedLevel;

            if (shouldSync)
            {
                if (currentlySynced && state!.RecommendedLevel == recommendedLevel && state.Job == jobData.Job)
                {
                    // Already synced; re-push client notices in case the prior batch was missed.
                    ApplySyncedStats(jobData, recommendedLevel);
                    EnqueueStatSyncNotices(client, character, queue, restoringFromSync: false);
                    return;
                }

                // Capture the real stats. If already synced, the live job data holds reduced stats, so the real
                // values come from the stored state.
                SyncState newState = new SyncState
                {
                    Job = jobData.Job,
                    RecommendedLevel = recommendedLevel,
                    RealAtk = currentlySynced ? state!.RealAtk : jobData.Atk,
                    RealDef = currentlySynced ? state!.RealDef : jobData.Def,
                    RealMAtk = currentlySynced ? state!.RealMAtk : jobData.MAtk,
                    RealMDef = currentlySynced ? state!.RealMDef : jobData.MDef
                };
                _syncedCharacters[commonId] = newState;

                ApplySyncedStats(jobData, recommendedLevel);
                Logger.Info($"[LEVELSYNC] {character.CommonId} SYNC realLv={realLevel} recLv={recommendedLevel} -> reduced Atk={jobData.Atk} Def={jobData.Def} MAtk={jobData.MAtk} MDef={jobData.MDef} (real Atk={newState.RealAtk} MAtk={newState.RealMAtk})");
                // Push the reduced combat stats to the client. The level number and EXP gauge stay real.
                EnqueueStatSyncNotices(client, character, queue, restoringFromSync: false);
            }
            else if (currentlySynced)
            {
                // Restore the real combat stats.
                RestoreRealStats(jobData, state!);
                _syncedCharacters.TryRemove(commonId, out _);
                Logger.Info($"[LEVELSYNC] {character.CommonId} RESTORE -> Atk={jobData.Atk} MAtk={jobData.MAtk}");
                EnqueueStatSyncNotices(client, character, queue, restoringFromSync: true);
            }
        }

        /// <summary>
        /// Called at the start of a job-data update that may persist to the database (e.g. EXP/JP gain).
        /// If the character is currently synced, the real combat stats are restored onto the live job data so the
        /// update logic and database write operate on the true values. Pair every call with
        /// <see cref="EndPersistenceSafeUpdate"/>.
        /// </summary>
        public SafeUpdateToken BeginPersistenceSafeUpdate(CharacterCommon character)
        {
            var jobData = character?.ActiveCharacterJobData;
            if (jobData == null || !_syncedCharacters.TryGetValue(character!.CommonId, out SyncState? state))
            {
                return new SafeUpdateToken(false, 0);
            }

            uint realLevelBefore = jobData.Lv;
            RestoreRealStats(jobData, state!);
            return new SafeUpdateToken(true, realLevelBefore);
        }

        /// <summary>
        /// Called at the end of a persistence-safe update. Re-applies the reduced stats onto the live job data
        /// (after capturing any legitimate stat changes as the new real values) and, if a real level-up happened
        /// in between, re-pushes the reduced stats for the new level (the level-up notice the update already sent
        /// carried full stats). The real level number and EXP value the update sent are intentionally kept.
        /// </summary>
        public void EndPersistenceSafeUpdate(GameClient client, CharacterCommon character, SafeUpdateToken token, PacketQueue queue)
        {
            if (!token.WasSynced)
            {
                return;
            }

            var jobData = character?.ActiveCharacterJobData;
            if (jobData == null || !_syncedCharacters.TryGetValue(character!.CommonId, out SyncState? state))
            {
                return;
            }

            // Capture the (possibly updated) real stats produced by the update (e.g. recomputed on a real level-up).
            state!.RealAtk = jobData.Atk;
            state.RealDef = jobData.Def;
            state.RealMAtk = jobData.MAtk;
            state.RealMDef = jobData.MDef;
            state.Job = jobData.Job;

            // The real level only ever increases via EXP, and sync only applies while the real level exceeds the
            // recommended level, so the sync always still applies here.
            ApplySyncedStats(jobData, state.RecommendedLevel);

            // A real level-up during the update sent the client full (un-synced) stats via the level-up notice.
            // Re-push the synced (reduced) stats for the new level so combat stays balanced. The level number and
            // EXP gauge keep the real values the update already sent.
            if (jobData.Lv != token.RealLevelBefore)
            {
                EnqueueStatSyncNotices(client, character, queue, restoringFromSync: false);
            }
        }

        private static void RestoreRealStats(CDataCharacterJobData jobData, SyncState state)
        {
            jobData.Atk = state.RealAtk;
            jobData.Def = state.RealDef;
            jobData.MAtk = state.RealMAtk;
            jobData.MDef = state.RealMDef;
        }

        private static void ApplySyncedStats(CDataCharacterJobData jobData, uint level)
        {
            // Only the level-scaled base combat stats are reduced. The level itself is intentionally left untouched
            // so gear, equip requirements and progression keep using the real level.
            var baseStats = ExpManager.CalculateBaseStats(jobData.Job, level);
            jobData.Atk = baseStats.Atk;
            jobData.Def = baseStats.Def;
            jobData.MAtk = baseStats.MAtk;
            jobData.MDef = baseStats.MDef;
        }

        /// <summary>
        /// Pushes synced/restored stats to the client. For the local player we re-send the authoritative
        /// character status packet (S2CCharacterGetCharacterStatusNtc) with the synced job data (level,
        /// EXP and reduced combat stats), which is the packet the client honors at load. On restore we
        /// re-send it with the real values. Pawns keep the older JobLevelUp approach for now.
        /// </summary>
        private void EnqueueStatSyncNotices(
            GameClient client,
            CharacterCommon character,
            PacketQueue queue,
            bool restoringFromSync)
        {
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return;
            }

            bool isSynced = _syncedCharacters.TryGetValue(character.CommonId, out SyncState? state);

            if (character is Character playerCharacter)
            {
                if (!restoringFromSync && isSynced)
                {
                    uint syncLevel = state!.RecommendedLevel;
                    var reduced = ExpManager.CalculateBaseStats(jobData.Job, syncLevel);
                    uint syncExp = ExpManager.TotalExpToLevelUpTo(syncLevel, client.GameMode);
                    EnqueueCharacterStatusReload(client, playerCharacter, syncLevel, syncExp,
                        reduced.Atk, reduced.Def, reduced.MAtk, reduced.MDef, queue, "SYNC");
                }
                else
                {
                    // Restoring: jobData already holds the real level/EXP/stats.
                    EnqueueCharacterStatusReload(client, playerCharacter, jobData.Lv, jobData.Exp,
                        jobData.Atk, jobData.Def, jobData.MAtk, jobData.MDef, queue, "RESTORE");
                }
                return;
            }

            // Pawn path (unchanged for now).
            uint realLevel = jobData.Lv;
            CDataCharacterLevelParam levelParam = character.CDataCharacterLevelParam;
            EnqueueJobLevelUpNotices(client, character, queue, realLevel, levelParam);
        }

        /// <summary>
        /// Re-sends the authoritative character status to the client with the given level/EXP/combat stats.
        /// Always clones the live job data so server state is never mutated.
        /// </summary>
        private void EnqueueCharacterStatusReload(
            GameClient client,
            Character character,
            uint level,
            uint exp,
            ushort atk,
            ushort def,
            ushort matk,
            ushort mdef,
            PacketQueue queue,
            string label)
        {
            var real = character.ActiveCharacterJobData;

            CDataCharacterJobData jobClone = new()
            {
                Job = real.Job,
                Exp = exp,
                JobPoint = real.JobPoint,
                Lv = level,
                Atk = atk,
                Def = def,
                MAtk = matk,
                MDef = mdef,
                Strength = real.Strength,
                DownPower = real.DownPower,
                ShakePower = real.ShakePower,
                StunPower = real.StunPower,
                Constitution = real.Constitution,
                Guts = real.Guts,
                FireResist = real.FireResist,
                IceResist = real.IceResist,
                ThunderResist = real.ThunderResist,
                HolyResist = real.HolyResist,
                DarkResist = real.DarkResist,
                SpreadResist = real.SpreadResist,
                FreezeResist = real.FreezeResist,
                ShockResist = real.ShockResist,
                AbsorbResist = real.AbsorbResist,
                DarkElmResist = real.DarkElmResist,
                PoisonResist = real.PoisonResist,
                SlowResist = real.SlowResist,
                SleepResist = real.SleepResist,
                StunResist = real.StunResist,
                WetResist = real.WetResist,
                OilResist = real.OilResist,
                SealResist = real.SealResist,
                CurseResist = real.CurseResist,
                SoftResist = real.SoftResist,
                StoneResist = real.StoneResist,
                GoldResist = real.GoldResist,
                FireReduceResist = real.FireReduceResist,
                IceReduceResist = real.IceReduceResist,
                ThunderReduceResist = real.ThunderReduceResist,
                HolyReduceResist = real.HolyReduceResist,
                DarkReduceResist = real.DarkReduceResist,
                AtkDownResist = real.AtkDownResist,
                DefDownResist = real.DefDownResist,
                MAtkDownResist = real.MAtkDownResist,
                MDefDownResist = real.MDefDownResist
            };

            CDataCharacterLevelParam param = new()
            {
                Attack = atk,
                Defence = def,
                MagAttack = matk,
                MagDefence = mdef,
                Strength = real.Strength,
                DownPower = real.DownPower,
                ShakePower = real.ShakePower,
                StunPower = real.StunPower,
                Constitution = real.Constitution,
                Guts = real.Guts
            };

            S2CCharacterGetCharacterStatusNtc ntc = new()
            {
                CharacterId = character.CharacterId,
                StatusInfo = character.StatusInfo,
                JobParam = jobClone,
                CharacterParam = param,
                EditInfo = character.EditInfo,
                EquipDataList = character.Equipment.AsCDataEquipItemInfo(EquipType.Performance),
                VisualEquipDataList = character.Equipment.AsCDataEquipItemInfo(EquipType.Visual),
                EquipJobItemList = character.EquipmentTemplate.JobItemsAsCDataEquipJobItem(character.Job),
                HideHead = character.HideEquipHead,
                HideLantern = character.HideEquipLantern,
                JewelryNum = character.ExtendedParams.JewelrySlot
            };
            client.Enqueue(ntc, queue);

            Logger.Info($"[LEVELSYNC] STATUS_RELOAD {label} char={character.CharacterId} Lv={level} Exp={exp} Atk={atk} Def={def} MAtk={matk} MDef={mdef}");
        }

        private static void EnqueueHudContextRefresh(GameClient client, CharacterCommon character, PacketQueue queue)
        {
            if (character is Character playerCharacter)
            {
                if (client.Party?.GetPlayerPartyMember(client) is PlayerPartyMember playerMember)
                {
                    client.Enqueue(playerMember.GetPartyContext(), queue);
                    return;
                }

                client.Enqueue(playerCharacter.S2CContextGetLobbyPlayerContextNtc, queue);
                return;
            }

            if (character is Pawn pawn && client.Party?.GetPartyMemberByCharacter(pawn) is PawnPartyMember pawnMember)
            {
                if (pawn.CharacterId != client.Character.CharacterId)
                {
                    client.Enqueue(pawnMember.GetS2CContextGetPartyRentedPawn_ContextNtc(), queue);
                }
                else
                {
                    client.Enqueue(pawnMember.GetPartyContext(), queue);
                }
            }
        }

        private static void EnqueueRealJobExpRefresh(GameClient client, CharacterCommon character, PacketQueue queue)
        {
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return;
            }

            if (character is Character)
            {
                S2CJobCharacterJobExpUpNtc expNtc = new()
                {
                    JobId = jobData.Job,
                    AddExp = 0,
                    ExtraBonusExp = 0,
                    TotalExp = jobData.Exp
                };
                client.Enqueue(expNtc, queue);
            }
            else if (character is Pawn pawn)
            {
                S2CJobPawnJobExpUpNtc expNtc = new()
                {
                    JobId = jobData.Job,
                    AddExp = 0,
                    ExtraBonusExp = 0,
                    TotalExp = jobData.Exp
                };
                client.Enqueue(expNtc, queue);
            }
        }

        private static void EnqueueJobLevelUpNotices(
            GameClient client,
            CharacterCommon character,
            PacketQueue queue,
            uint noticeLevel,
            CDataCharacterLevelParam levelParam)
        {
            var jobData = character.ActiveCharacterJobData;
            if (jobData == null)
            {
                return;
            }

            if (character is Character playerCharacter)
            {
                S2CJobCharacterJobLevelUpNtc selfNtc = new()
                {
                    Job = jobData.Job,
                    Level = noticeLevel,
                    AddJobPoint = 0,
                    TotalJobPoint = jobData.JobPoint,
                    CharacterLevelParam = levelParam
                };
                client.Enqueue(selfNtc, queue);

                if (client.Party != null)
                {
                    S2CJobCharacterJobLevelUpMemberNtc memberNtc = new()
                    {
                        CharacterId = playerCharacter.CharacterId,
                        Job = jobData.Job,
                        Level = noticeLevel,
                        CharacterLevelParam = levelParam
                    };
                    client.Party.EnqueueToAllExcept(memberNtc, queue, client);
                }
            }
            else if (character is Pawn pawn)
            {
                S2CJobPawnJobLevelUpNtc selfNtc = new()
                {
                    PawnId = pawn.PawnId,
                    Job = jobData.Job,
                    Level = noticeLevel,
                    AddJobPoint = 0,
                    TotalJobPoint = jobData.JobPoint,
                    CharacterLevelParam = levelParam
                };
                client.Enqueue(selfNtc, queue);

                if (client.Party != null)
                {
                    S2CJobPawnJobLevelUpMemberNtc memberNtc = new()
                    {
                        CharacterId = pawn.CharacterId,
                        PawnId = pawn.PawnId,
                        Job = jobData.Job,
                        Level = noticeLevel,
                        CharacterLevelParam = levelParam
                    };
                    client.Party.EnqueueToAllExcept(memberNtc, queue, client);
                }
            }
        }
    }
}

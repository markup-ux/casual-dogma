using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.GameServer.Tasks;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Ddon.Shared.Model.Scheduler;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer
{
    public class WorldQuestManager
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(WorldQuestManager));

        private readonly DdonGameServer _server;
        private Dictionary<QuestAreaId, HashSet<uint>> _serverPool;
        private readonly object _lock = new object();

        public WorldQuestManager(DdonGameServer server)
        {
            _server = server;
            _serverPool = new Dictionary<QuestAreaId, HashSet<uint>>();
            foreach (var areaId in Enum.GetValues<QuestAreaId>())
                _serverPool[areaId] = new HashSet<uint>();
        }

        /// <summary>
        /// Called on server startup (after QuestManager.LoadQuests). Computes the seed for the
        /// current period and populates the server pool so that newly-created parties get the
        /// correct quests without waiting for the next weekly reset broadcast.
        /// Only active when WorldQuestSystem = ServerReset.
        /// </summary>
        public void Initialize()
        {
            if (_server.GameSettings.GameServerSettings.WorldQuestSystem != WorldQuestSystemMode.ServerReset)
                return;

            var settings = _server.GameSettings.GameServerSettings;
            var resetTask = _server.ScriptManager.SchedulerTaskModule.GetTask<WeeklyTask>(TaskType.WorldQuestRotation);
            if (resetTask == null)
            {
                Logger.Error("WorldQuestManager: no WorldQuestRotation task found in scripts; skipping pool initialization.");
                return;
            }
            long seed = ComputeCurrentPeriodSeed(resetTask.Day, resetTask.Hour, resetTask.Minute, settings.GetEffectiveUtcOffset());
            var periodStart = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(seed), settings.ServerTimeZone);
            Logger.Info($"WorldQuestManager initializing with seed {seed} (period starting {periodStart:yyyy-MM-dd HH:mm:ss zzz})");
            RollPool(seed);
        }

        /// <summary>
        /// Returns a copy of the current server-wide quest pool.
        /// Each new party calls this via EnforceInitialPoolEligibility so it starts with the correct quests.
        /// </summary>
        public Dictionary<QuestAreaId, HashSet<uint>> GetCurrentPool()
        {
            lock (_lock)
            {
                var copy = new Dictionary<QuestAreaId, HashSet<uint>>();
                foreach (var (areaId, ids) in _serverPool)
                    copy[areaId] = new HashSet<uint>(ids);
                return copy;
            }
        }

        /// <summary>
        /// Performs a world quest reset triggered by the weekly task.
        /// In ServerReset mode: rotates the server-wide pool, notifies all online parties, and purges stale DB progress.
        /// In InstanceReset mode (when WorldQuestFirstClearRewards is enabled): skips pool rotation but still resets
        /// per-character first-clear records so the new period's rewards are available.
        /// </summary>
        public void PerformReset(long seed)
        {
            var settings = _server.GameSettings.GameServerSettings;
            bool isServerReset = settings.WorldQuestSystem == WorldQuestSystemMode.ServerReset;

            if (isServerReset)
            {
                Logger.Info($"WorldQuestManager performing pool reset with seed {seed}");
                RollPool(seed);

                foreach (var party in _server.PartyManager.GetAllParties())
                {
                    if (party.QuestState is SharedQuestStateManager shared)
                    {
                        shared.OnServerWorldQuestReset(GetCurrentPool());
                    }
                }

                // Purge all persisted world quest progress so offline players don't reload
                // stale quests on next login. Online players already had their in-memory
                // state cleared above before this runs.
                _server.Database.RemoveAllQuestProgressByType(QuestType.World);

                _server.ChatManager.BroadcastMessage(
                    LobbyChatMsgType.ManagementAlertN,
                    "World quest pool has been refreshed for the new period.");
            }

            if (settings.WorldQuestFirstClearRewards)
            {
                Logger.Info($"WorldQuestManager clearing period first-clear reward records");
                _server.Database.DeleteQuestPeriodFirstClears(QuestType.World);

                foreach (var party in _server.PartyManager.GetAllParties())
                    foreach (var client in party.Clients)
                        client.Character.GetQuestPeriodFirstClears(QuestType.World).Clear();

                if (!isServerReset)
                {
                    _server.ChatManager.BroadcastMessage(
                        LobbyChatMsgType.ManagementAlertN,
                        "World quest rewards have been reset for the new period.");
                }
            }
        }

        /// <summary>
        /// Computes the Unix timestamp of the most recent past occurrence of the configured
        /// reset day and hour. All server instances independently arrive at the same seed as long
        /// as they are configured with the same utcOffset, making the pool deterministic
        /// without a DB round-trip.
        /// </summary>
        public static long ComputeCurrentPeriodSeed(DayOfWeek resetDay, uint resetHour, uint resetMinute = 0, TimeSpan? utcOffset = null)
        {
            var offset = utcOffset ?? TimeSpan.Zero;
            var now = DateTimeOffset.UtcNow.ToOffset(offset);
            int dayDiff = ((int)now.DayOfWeek - (int)resetDay + 7) % 7;
            var resetDate = now.AddDays(-dayDiff);
            var resetTime = new DateTimeOffset(
                resetDate.Year, resetDate.Month, resetDate.Day,
                (int)resetHour, (int)resetMinute, 0, offset);

            // If the reset time for today hasn't happened yet, go back one full week
            if (resetTime > now)
                resetTime = resetTime.AddDays(-7);

            return resetTime.ToUnixTimeSeconds();
        }

        private void RollPool(long seed)
        {
            var rng = new Random((int)(seed & 0x7FFFFFFF));
            var newPool = new Dictionary<QuestAreaId, HashSet<uint>>();

            foreach (var areaId in Enum.GetValues<QuestAreaId>())
            {
                newPool[areaId] = new HashSet<uint>();
                foreach (var questId in QuestManager.GetWorldQuestIdsByAreaId(areaId))
                {
                    var quest = RollQuestWithRng(questId, rng);
                    if (quest != null)
                        newPool[areaId].Add(quest.QuestScheduleId);
                }
            }

            lock (_lock)
            {
                _serverPool = newPool;
            }
        }

        private static Quest RollQuestWithRng(QuestId questId, Random rng)
        {
            var scheduleIds = QuestManager.GetQuestScheduleIdsForQuestId(questId);
            if (scheduleIds.Count == 0) return null;
            var idx = rng.Next(0, scheduleIds.Count);
            var scheduleId = scheduleIds.ElementAt(idx);
            return QuestManager.GetQuestByScheduleId(scheduleId);
        }
    }
}

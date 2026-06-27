using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Server.Settings;
using System;

namespace Arrowgene.Ddon.GameServer.Enemies
{
    /// <summary>
    /// Configures faster per-mob repop for Casual Dogma exploration farming.
    /// Most world/dungeon spawns ship with RepopCount=0 (no respawn); this enables single respawns
    /// in normal-mode dungeons and scales wait time from sync level or mob level.
    /// </summary>
    public static class ExplorationRepopHelper
    {
        public static void ConfigureSpawn(DdonGameServer server, GameClient client, StageLayoutId stageLayoutId, InstancedEnemy enemy)
        {
            GameServerSettings settings = server.GameSettings.GameServerSettings;
            if (!settings.EnableDungeonMobRepop || client.GameMode != GameMode.Normal)
            {
                return;
            }

            if (enemy.QuestScheduleId != 0 || enemy.IsAreaBoss || enemy.IsBossGauge)
            {
                return;
            }

            if (!StageManager.IsDungeon(stageLayoutId.Id))
            {
                return;
            }

            uint baseWait = ResolveBaseWaitSeconds(server, stageLayoutId, enemy, settings);
            enemy.RepopWaitSecond = ScaleWaitSeconds(baseWait, settings);

            if (enemy.RepopCount == 0)
            {
                enemy.RepopCount = 1;
                enemy.RepopNum = 0;
            }
        }

        public static uint ResolveRepopWaitSeconds(DdonGameServer server, InstancedEnemy enemy)
        {
            GameServerSettings settings = server.GameSettings.GameServerSettings;
            uint baseWait = enemy.RepopWaitSecond;
            if (baseWait == 0)
            {
                baseWait = ResolveBaseWaitSeconds(server, enemy.StageLayoutId, enemy, settings);
            }

            return ScaleWaitSeconds(baseWait, settings);
        }

        /// <summary>
        /// Base wait before multiplier:
        /// fixed override, else syncLevel × perSyncSeconds, else mobLevel ÷ 2 (minimum minWait).
        /// </summary>
        private static uint ResolveBaseWaitSeconds(
            DdonGameServer server,
            StageLayoutId stageLayoutId,
            InstancedEnemy enemy,
            GameServerSettings settings)
        {
            if (settings.ExplorationMobRepopBaseSeconds > 0)
            {
                return settings.ExplorationMobRepopBaseSeconds;
            }

            uint syncLevel = server.LevelSyncManager.GetRecommendedLevel(stageLayoutId.Id);
            if (syncLevel > 0)
            {
                uint syncWait = syncLevel * settings.ExplorationMobRepopSecondsPerSyncLevel;
                return Math.Max(settings.ExplorationMobRepopMinWaitSeconds, syncWait);
            }

            uint levelWait = (uint)Math.Ceiling(enemy.Lv / 2.0);
            return Math.Max(settings.ExplorationMobRepopMinWaitSeconds, levelWait);
        }

        private static uint ScaleWaitSeconds(uint baseWait, GameServerSettings settings)
        {
            double scaled = baseWait * settings.ExplorationMobRepopWaitMultiplier;
            uint wait = (uint)Math.Ceiling(scaled);
            return Math.Clamp(
                wait,
                settings.ExplorationMobRepopMinWaitSeconds,
                settings.ExplorationMobRepopMaxWaitSeconds);
        }
    }
}

using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Context;
using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Enemies.Generators
{
    public class QuestEnemySetGenerator : IEnemySetGenerator
    {
        public bool GetEnemySet(DdonGameServer server, GameClient client, StageLayoutId stageLayoutId, byte subGroupId, List<InstancedEnemy> instancedEnemySet, out QuestId questId)
        {
            questId = QuestId.None;

            Quest quest = FindQuestBasedOnPriority(client, stageLayoutId, subGroupId);
            if (quest == null)
            {
                return false;
            }

            questId = quest.QuestId;

            var questStateManager = GetEffectiveQuestStateManager(client, quest);
            instancedEnemySet.AddRange(questStateManager.GetInstancedEnemies(quest, stageLayoutId, subGroupId));

            // Main quests with OverrideEnemySpawn can match a stage layout even when no
            // quest enemies are active for the current step; fall through to world spawns.
            return instancedEnemySet.Count > 0;
        }

        // For personal quests (substory, tutorial, main), a non-leader party member
        // may have the leader's quest schedule ID in their collected set but not in
        // their own QuestState. Fall back to the leader's state so enemies are visible.
        private static QuestStateManager GetEffectiveQuestStateManager(GameClient client, Quest quest)
        {
            if (!quest.IsPersonal)
                return client.Party.QuestState;

            if (client.QuestState.IsQuestActive(quest.QuestScheduleId))
                return client.QuestState;

            var leader = client.Party.Leader;
            if (leader != null && !ReferenceEquals(leader.Client, client) && leader.Client.QuestState.IsQuestActive(quest.QuestScheduleId))
                return leader.Client.QuestState;

            return client.QuestState;
        }

        private Quest FindQuestBasedOnPriority(GameClient client, StageLayoutId stageLayoutId, uint subgroupId)
        {
            var quests = new List<Quest>();
            foreach (var questScheduleId in QuestManager.CollectQuestScheduleIds(client, stageLayoutId))
            {
                var quest = QuestManager.GetQuestByScheduleId(questScheduleId);
                var questStateManager = GetEffectiveQuestStateManager(client, quest);
                if (quest.OverrideEnemySpawn && quest.HasEnemiesInCurrentStageGroup(stageLayoutId))
                {
                    quests.Add(quest);
                }
                else if (!quest.OverrideEnemySpawn && questStateManager.HasEnemiesForCurrentQuestStepInStageGroup(quest, stageLayoutId, subgroupId))
                {
                    quests.Add(quest);
                }
            }

            // There may be multiple quests conflicting for a StageId.LayerNo.GroupNo.
            // Certain quests should have a higher priority than other quests
            // and this list describes the ranking of the different quest types.
            var questPriorityList = new List<QuestType>()
            {
                QuestType.Main,
                QuestType.Tutorial,
                QuestType.WildHunt,
                QuestType.World,
            };

            Quest priorityQuest = null;
            foreach (var questType in questPriorityList)
            {
                var matches = quests.Where(x => x.QuestType == questType).ToList();
                if (matches.Count > 0)
                {
                    priorityQuest = matches[0];
                    break;
                }
            }

            if (priorityQuest == null && quests.Count > 0)
            {
                priorityQuest = quests[0];
            }

            return priorityQuest;
        }
    }
}

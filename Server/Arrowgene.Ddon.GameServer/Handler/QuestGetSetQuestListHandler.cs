using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.GameServer.Quests;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestGetSetQuestListHandler : GameRequestPacketHandler<C2SQuestGetSetQuestListReq, S2CQuestGetSetQuestListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestGetQuestPartyBonusListHandler));

        public QuestGetSetQuestListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestGetSetQuestListRes Handle(GameClient client, C2SQuestGetSetQuestListReq request)
        {
            client.Character.AreaId = request.DistributeId;

            S2CQuestGetSetQuestListRes res = new S2CQuestGetSetQuestListRes()
            {
                DistributeId = request.DistributeId
            };

            // If shared QuestState is empty and this client is the leader, reload their quest
            // progress from DB into shared state.
            if (client.Party.Leader?.Client == client && client.Party.QuestState.GetActiveQuestScheduleIds().Count == 0)
            {
                var progress = Server.Database.GetQuestProgressByType(client.Character.CommonId, QuestType.All);
                var deliveryProgressByQuest = Server.Database.GetAllQuestDeliveryProgress(client.Character.CommonId)
                    .GroupBy(x => x.QuestScheduleId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                foreach (var questProgress in progress)
                {
                    var quest = QuestManager.GetQuestByScheduleId(questProgress.QuestScheduleId);
                    if (quest is null) continue;
                    QuestStateManager questStateManager = QuestManager.GetQuestStateManager(client, quest);
                    questStateManager.AddNewQuest(questProgress.QuestScheduleId, questProgress.Step);

                    if (deliveryProgressByQuest.TryGetValue(questProgress.QuestScheduleId, out var deliveryItems))
                    {
                        foreach (var dp in deliveryItems)
                            questStateManager.RestoreDeliveryProgress(questProgress.QuestScheduleId, (ItemId)dp.ItemId, dp.AmountDelivered);
                    }
                }
                Logger.Info(client, $"[QuestGetSetQuestList] Reloaded quest state for promoted leader {client.Character.CharacterId}");
            }

            // Remove all world quests which have no progress made.
            client.Party.QuestState.RemoveInactiveWorldQuests();

            // Build the NTC, mutating path
            S2CQuestGetSetQuestListNtc ntc = BuildQuestListNtc(client, request.DistributeId, mutating: true);
            res.SetQuestList = ntc.SetQuestList;

            // Only broadcast to all if the requesting client is the leader.
            // Non-leader area entry should not overwrite other members' displayed quest state.
            if (client.Party.Leader?.Client == client)
            {
                client.Party.SendToAll(ntc);
            }
            else
            {
                client.Send(ntc);
            }

            // Resync the client's delivery UI with any partial deliveries already in memory.
            uint charId = client.Character.CharacterId;
            foreach (var deliveryNtc in client.QuestState.GetRestoredDeliveryNtcs(charId))
                client.Send(deliveryNtc);
            foreach (var deliveryNtc in client.Party.QuestState.GetRestoredDeliveryNtcs(charId))
                client.Send(deliveryNtc);

            return res;
        }

        public static S2CQuestGetSetQuestListNtc BuildQuestListNtc(GameClient client, QuestAreaId areaId, bool mutating = false)
        {
            var leaderCharacter = client.Party.Leader?.Client?.Character;

            var ntc = new S2CQuestGetSetQuestListNtc()
            {
                DistributeId = areaId,
                SelectCharacterId = leaderCharacter?.CharacterId ?? client.Character.CharacterId,
                SetQuestList = new List<CDataSetQuestList>()
            };

            if (QuestManager.HasWorldQuestAreaReleased(client.Character, areaId))
            {
                foreach (var questScheduleId in client.Party.QuestState.GetActiveQuestScheduleIds())
                {
                    Quest quest = client.Party.QuestState.GetQuest(questScheduleId);
                    if (quest is null || !QuestManager.IsWorldQuest(quest.QuestId))
                    {
                        continue;
                    }

                    QuestState questState = client.Party.QuestState.GetQuestState(quest);
                    uint clearCount = leaderCharacter?.GetQuestPeriodFirstClears(quest.QuestType).Contains(quest.QuestScheduleId) == true ? 1u : 0u;
                    ntc.SetQuestList.Add(quest.ToCDataSetQuestList(questState?.Step ?? 0, clearCount));
                }

                foreach (var questScheduleId in client.Party.QuestState.AreaQuests(areaId))
                {
                    Quest quest = QuestManager.GetQuestByScheduleId(questScheduleId);

                    if (quest is null
                        || client.Party.QuestState.IsQuestActive(questScheduleId)
                        || client.Party.QuestState.IsCompletedWorldQuest(questScheduleId))
                    {
                        continue;
                    }

                    uint clearCount = leaderCharacter?.GetQuestPeriodFirstClears(quest.QuestType).Contains(quest.QuestScheduleId) == true ? 1u : 0u;
                    ntc.SetQuestList.Add(quest.ToCDataSetQuestList(0, clearCount));

                    if (mutating)
                    {
                        client.Party.QuestState.AddNewQuest(quest, 0);
                        // Enemy requests arrive before the quest list, so loaded groups may have generic enemies. Reset them.
                        quest.ResetEnemiesForStage(client, client.Character.Stage, onlyLoaded: true);
                    }
                }
            }

            var debugQuest = QuestManager.GetQuestByQuestId((QuestId)70000001);
            if (debugQuest != null)
            {
                ntc.SetQuestList.Add(new CDataSetQuestList()
                {
                    Detail = new CDataSetQuestDetail()
                    {
                        IsDiscovery = false,
                        ClearCount = 0
                    },
                    Param = debugQuest.ToCDataQuestList(0),
                });
            }

            return ntc;
        }
    }
}

using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestCancelHandler : GameRequestPacketHandler<C2SQuestQuestCancelReq, S2CQuestQuestCancelRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestCancelHandler));

        public QuestCancelHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestQuestCancelRes Handle(GameClient client, C2SQuestQuestCancelReq packet)
        {
            var quest = QuestManager.GetQuestByScheduleId(packet.QuestScheduleId);
            var questStateManager = QuestManager.GetQuestStateManager(client, quest);
            bool isPriority = Server.Database.DeletePriorityQuest(client.Character.CommonId, quest.QuestScheduleId);
            Server.Database.RemoveQuestProgress(client.Character.CommonId, quest.QuestScheduleId, quest.QuestType);

            bool isLeaderOrSolo = client.Party.IsSolo || client.Party.Leader?.Client == client;
            if (quest.IsPersonal || isLeaderOrSolo)
            {
                questStateManager.CancelQuest(quest.QuestScheduleId);

                S2CQuestQuestCancelNtc cancelNtc = new S2CQuestQuestCancelNtc()
                {
                    QuestId = (uint)quest.QuestId,
                    QuestScheduleId = quest.QuestScheduleId
                };
                client.Send(cancelNtc);

                if (!quest.IsPersonal && !client.Party.IsSolo)
                {
                    client.Party.SendToAllExcept(cancelNtc, client);
                }
            }

            if (isPriority && isLeaderOrSolo)
            {
                var leaderClient = client.Party.Leader?.Client ?? client;
                client.Party.QuestState.UpdatePriorityQuestList(leaderClient).Send();
            }
            
            return new S2CQuestQuestCancelRes()
            {
                QuestScheduleId = packet.QuestScheduleId
            };
        }
    }
}


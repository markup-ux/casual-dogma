using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;
using System;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestPlayStartTimerHandler : GameRequestPacketHandler<C2SQuestPlayStartTimerReq, S2CQuestPlayStartTimerRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestPlayStartTimerHandler));

        public QuestPlayStartTimerHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestPlayStartTimerRes Handle(GameClient client, C2SQuestPlayStartTimerReq request)
        {
            if (!BoardManager.BoardIdIsExm(client.Party.ContentId))
            {
                // See on the MSQ for the Spirit Dragon, this handler gets called
                // even though there is no timer present in the quest
                return new S2CQuestPlayStartTimerRes();
            }

            var quest = QuestManager.GetQuestByBoardId(client.Party.ContentId);
            if (quest.MissionParams.PlaytimeInSeconds > 0)
            {
                var ntc = new S2CQuestPlayStartTimerNtc()
                {
                    PlayEndDateTime = (ulong)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + quest.MissionParams.PlaytimeInSeconds)
                };
                client.Party.SendToAll(ntc);

                Server.PartyQuestContentManager.StartTimer(client.Party.Id, client, quest.MissionParams.PlaytimeInSeconds);
            }

            return new S2CQuestPlayStartTimerRes();
        }
    }
}

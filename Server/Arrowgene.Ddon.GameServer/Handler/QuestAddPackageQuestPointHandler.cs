using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestAddPackageQuestPointHandler : GameRequestPacketHandler<C2SQuestAddPackageQuestPointReq, S2CQuestAddPackageQuestPointRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(QuestAddPackageQuestPointHandler));

        public QuestAddPackageQuestPointHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestAddPackageQuestPointRes Handle(GameClient client, C2SQuestAddPackageQuestPointReq req)
        {
            // TODO: Probably need to record something here from the quest into the DB/quest state
            return new();
        }
    }
}


using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class QuestGetPackageQuestInfoHandler : GameRequestPacketHandler<C2SQuestGetPackageQuestInfoReq, S2CQuestGetPackageQuestInfoRes>
    {
        public QuestGetPackageQuestInfoHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CQuestGetPackageQuestInfoRes Handle(GameClient client, C2SQuestGetPackageQuestInfoReq req)
        {
            var requirements = Server.GameSettings.Get<Dictionary<QuestSubstoryGroupId, QuestId>>("substory", "SubstoryGroupIdUnlockReqs") ??
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_SERVER_CONFIG_ERROR);

            // 1, 2, 3
            var res = new S2CQuestGetPackageQuestInfoRes();
            foreach (var (substoryGroupId, questId) in requirements)
            {
                res.PackageQuestEntryList.Add(new()
                {
                    SubstoryGroupId = substoryGroupId,
                    IsEnabled = (substoryGroupId == QuestSubstoryGroupId.Carrie) || client.Character.HasQuestCompleted(questId)
                });
            }

            return res;
        }
    }
}

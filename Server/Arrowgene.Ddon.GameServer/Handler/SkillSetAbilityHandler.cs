using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class SkillSetAbilityHandler : GameRequestPacketHandler<C2SSkillSetAbilityReq, S2CSkillSetAbilityRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SkillSetAbilityHandler));

        public SkillSetAbilityHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CSkillSetAbilityRes Handle(GameClient client, C2SSkillSetAbilityReq packet)
        {
            if (packet.SlotNo == 0)
            {
                throw new ResponseErrorException(ErrorCode.ERROR_CODE_SKILL_INVALID_SLOT_NO, $"Requesting to set an ability to slot 0");
            }

            Ability abilitySlot = Server.JobManager.SetAbility(Server.Database, client, client.Character, packet.SlotNo, packet.AbilityId, packet.AbilityLv);

            return new S2CSkillSetAbilityRes() {
                SlotNo = packet.SlotNo,
                AbilityId = abilitySlot.AbilityId,
                AbilityLv = abilitySlot.AbilityLv
            };
        }
    }
}

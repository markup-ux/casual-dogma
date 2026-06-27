using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class CharacterGetReviveChargeableTimeHandler : GameRequestPacketHandler<C2SCharacterGetReviveChargeableTimeReq, S2CCharacterGetReviveChargeableTimeRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(CharacterGetReviveChargeableTimeHandler));
        
        public CharacterGetReviveChargeableTimeHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CCharacterGetReviveChargeableTimeRes Handle(GameClient client, C2SCharacterGetReviveChargeableTimeReq request)
        {
            S2CCharacterGetReviveChargeableTimeRes res = new S2CCharacterGetReviveChargeableTimeRes();

            //  Refresh revival at 5:00AM JST.

            Server.RevivalManager.ProcessPendingRecharges(client);
            res.RemainTime = Server.RevivalManager.GetRemainingRechargeSeconds(client.Character.CharacterId);

            return res;
        }
    }
}

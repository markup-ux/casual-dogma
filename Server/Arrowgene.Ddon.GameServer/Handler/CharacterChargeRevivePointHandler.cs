using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class CharacterChargeRevivePointHandler : GameRequestPacketHandler<C2SCharacterChargeRevivePointReq, S2CCharacterChargeRevivePointRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(CharacterChargeRevivePointHandler));
        
        public CharacterChargeRevivePointHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CCharacterChargeRevivePointRes Handle(GameClient client, C2SCharacterChargeRevivePointReq request)
        {
            Server.RevivalManager.TryGrantNpcRevivalPower(client);

            return new S2CCharacterChargeRevivePointRes()
            {
                RevivePoint = client.Character.StatusInfo.RevivePoint
            };
        }
    }
}

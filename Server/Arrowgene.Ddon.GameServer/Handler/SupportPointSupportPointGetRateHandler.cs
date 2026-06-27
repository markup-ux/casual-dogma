using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class SupportPointSupportPointGetRateHandler(DdonGameServer server) : GameRequestPacketHandler<C2SSupportPointSupportPointGetRateReq, S2CSupportPointSupportPointGetRateRes>(server)
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SupportPointSupportPointGetRateHandler));

        public override S2CSupportPointSupportPointGetRateRes Handle(GameClient client, C2SSupportPointSupportPointGetRateReq request)
        {
            return new()
            {
                SupportRate = [new()
                {
                    StatusType = 1,
                    Rate = Server.GameSettings.GameServerSettings.RentalPointConversionRate
                }]
            };
        }
    }
}

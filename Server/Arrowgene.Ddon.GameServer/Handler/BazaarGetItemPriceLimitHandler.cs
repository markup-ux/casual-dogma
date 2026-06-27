using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class BazaarGetItemPriceLimitHandler : GameRequestPacketHandler<C2SBazaarGetItemPriceLimitReq, S2CBazaarGetItemPriceLimitRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(BazaarGetItemPriceLimitHandler));
        
        public BazaarGetItemPriceLimitHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CBazaarGetItemPriceLimitRes Handle(GameClient client, C2SBazaarGetItemPriceLimitReq request)
        {
            return new S2CBazaarGetItemPriceLimitRes()
            {
                ItemId = request.ItemId,
                Low = Server.GameSettings.GameServerSettings.BazaarExhibitionMinPrice,
                High = Server.GameSettings.GameServerSettings.BazaarExhibitionMaxPrice,
                Num = Server.GameSettings.GameServerSettings.BazaarExhibitionMaxItemNum 
            };
        }
    }
}

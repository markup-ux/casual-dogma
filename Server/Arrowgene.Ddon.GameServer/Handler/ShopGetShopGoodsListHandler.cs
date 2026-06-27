using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class ShopGetShopGoodsListHandler : GameRequestPacketHandler<C2SShopGetShopGoodsListReq, S2CShopGetShopGoodsListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ShopGetShopGoodsListHandler));
        
        public ShopGetShopGoodsListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CShopGetShopGoodsListRes Handle(GameClient client, C2SShopGetShopGoodsListReq request)
        {
            Logger.Info($"ShopId={request.ShopId}");

            client.Character.LastEnteredShopId = request.ShopId;

            var goods = (S2CShopGetShopGoodsListRes)client.InstanceShopManager.GetAssets(request.ShopId).Clone();
            var settings = Server.GameSettings.GameServerSettings;

            // Gold has been made useless: show gold-priced shop goods as free.
            if (settings.MakeGoldFree && goods.WalletType == Shared.Model.WalletType.Gold)
            {
                foreach (var good in goods.GoodsParamList)
                {
                    good.Price = 0;
                }
            }

            goods = Shop.CombatGearShopFilter.FilterGoods(
                goods,
                Server.AssetRepository,
                settings.RemoveCombatGearFromShops);

            return goods;
        }

    }
}

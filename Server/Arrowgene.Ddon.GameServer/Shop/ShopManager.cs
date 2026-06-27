using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arrowgene.Ddon.Database;
using Arrowgene.Ddon.Shared;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;

namespace Arrowgene.Ddon.GameServer.Shop
{
    public class ShopManager : AssetManager<Shared.Model.Shop>
    {
        protected Dictionary<uint, S2CShopGetShopGoodsListRes> Goods;

        // Set of every item id purchasable from a gold-priced shop. Used to prevent the
        // "buy for 0 gold then sell for XP" exploit when gold has been made free.
        private HashSet<uint> GoldShopItemIds;

        public ShopManager(AssetRepository assetRepository, IDatabase database) : base(assetRepository, AssetRepository.ShopKey, database, assetRepository.ShopAsset)
        {
        }

        protected override void OnInit()
        {
            Goods = new Dictionary<uint, S2CShopGetShopGoodsListRes>();
            GoldShopItemIds = new HashSet<uint>();
        }

        public override void Load()
        {
            Goods.Clear();
            GoldShopItemIds.Clear();
            foreach (Shared.Model.Shop shop in this._assetList)
            {
                Goods.Add(shop.ShopId, shop.Data);

                if (shop.Data.WalletType == Shared.Model.WalletType.Gold)
                {
                    foreach (var good in shop.Data.GoodsParamList)
                    {
                        GoldShopItemIds.Add(good.ItemId);
                    }
                }
            }
        }

        public S2CShopGetShopGoodsListRes GetAssets(uint ShopId)
        {
            return Goods.GetValueOrDefault(ShopId, new S2CShopGetShopGoodsListRes());
        }

        /// <summary>
        /// Returns true if the given item id can be purchased from any gold-priced shop.
        /// </summary>
        public bool IsSoldInGoldShop(uint itemId)
        {
            return GoldShopItemIds.Contains(itemId);
        }

        public IReadOnlyCollection<uint> GetGoldShopItemIds()
        {
            return GoldShopItemIds;
        }
    }
}
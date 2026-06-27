using Arrowgene.Ddon.GameServer.GatheringItems;
using Arrowgene.Ddon.Shared;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Shop
{
    public static class CombatGearShopFilter
    {
        public static S2CShopGetShopGoodsListRes FilterGoods(
            S2CShopGetShopGoodsListRes source,
            AssetRepository assetRepository,
            bool removeCombatGear)
        {
            if (!removeCombatGear)
            {
                return source;
            }

            var filtered = (S2CShopGetShopGoodsListRes)source.Clone();
            filtered.GoodsParamList = filtered.GoodsParamList
                .Where(good => !IsBlockedShopGood(good.ItemId, assetRepository))
                .ToList();
            return filtered;
        }

        public static List<CDataJobValueShopItem> FilterJobValueLineup(
            IEnumerable<CDataJobValueShopItem> lineup,
            AssetRepository assetRepository,
            bool removeCombatGear)
        {
            if (!removeCombatGear)
            {
                return lineup.ToList();
            }

            return lineup
                .Where(item => !IsBlockedShopGood(item.ItemId, assetRepository))
                .ToList();
        }

        private static bool IsBlockedShopGood(uint itemId, AssetRepository assetRepository)
        {
            if (!assetRepository.ClientItemInfos.TryGetValue((ItemId)itemId, out ClientItemInfo? info))
            {
                return false;
            }

            return ExplorationProgressionCatalog.IsPerformanceCombatGear(info);
        }
    }
}

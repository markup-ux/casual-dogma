using Arrowgene.Ddon.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.GatheringItems.Generators
{
    /// <summary>
    /// Injects craftable performance gear and dress/cosmetic equipment into enemy kill loot.
    /// </summary>
    public class WildCraftDropGenerator : IDropGenerator
    {
        private static readonly HashSet<ItemSubCategory> CosmeticSubCategories = new()
        {
            ItemSubCategory.EquipClothingBody,
            ItemSubCategory.EquipClothingLeg,
            ItemSubCategory.EquipOverwear,
            ItemSubCategory.EquipEnsemble,
        };

        private readonly DdonGameServer _server;
        private readonly object _poolLock = new();
        private List<ItemId>? _craftedGearPool;
        private List<ItemId>? _cosmeticPool;

        public WildCraftDropGenerator(DdonGameServer server)
        {
            _server = server;
        }

        public List<InstancedGatheringItem> Generate(GameClient client, InstancedEnemy enemyKilled)
        {
            var settings = _server.GameSettings.GameServerSettings;
            if (!settings.EnableWildCraftedGearDrops && !settings.EnableWildCosmeticDrops)
            {
                return new();
            }

            if (client.GameMode != GameMode.Normal)
            {
                return new();
            }

            EnsurePoolsBuilt();

            var results = new List<InstancedGatheringItem>();
            bool isBoss = enemyKilled.IsBossGauge || enemyKilled.IsAreaBoss;

            if (settings.EnableWildCraftedGearDrops && _craftedGearPool!.Count > 0)
            {
                double chance = isBoss ? settings.WildCraftedGearBossDropChance : settings.WildCraftedGearDropChance;
                if (Random.Shared.NextDouble() <= chance)
                {
                    TryAddDrop(results, _craftedGearPool, enemyKilled.Lv, settings.WildGearDropLevelTolerance);
                }
            }

            if (settings.EnableWildCosmeticDrops && _cosmeticPool!.Count > 0)
            {
                double chance = isBoss ? settings.WildCosmeticBossDropChance : settings.WildCosmeticDropChance;
                if (Random.Shared.NextDouble() <= chance)
                {
                    TryAddDrop(results, _cosmeticPool, enemyKilled.Lv, settings.WildGearDropLevelTolerance);
                }
            }

            return results;
        }

        private void TryAddDrop(List<InstancedGatheringItem> results, List<ItemId> pool, ushort enemyLevel, uint levelTolerance)
        {
            uint enemyLv = enemyLevel;
            uint minLevel = enemyLv > levelTolerance ? enemyLv - levelTolerance : 1;
            uint maxLevel = enemyLv + levelTolerance;

            var candidates = new List<ItemId>();
            foreach (ItemId itemId in pool)
            {
                if (!_server.AssetRepository.ClientItemInfos.TryGetValue(itemId, out ClientItemInfo? info))
                {
                    continue;
                }

                uint itemLevel = GetEquipLevel(info);
                if (itemLevel >= minLevel && itemLevel <= maxLevel)
                {
                    candidates.Add(itemId);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            results.Add(new InstancedGatheringItem
            {
                ItemId = candidates[Random.Shared.Next(candidates.Count)],
                ItemNum = 1,
                Quality = 0,
            });
        }

        private void EnsurePoolsBuilt()
        {
            if (_craftedGearPool != null && _cosmeticPool != null)
            {
                return;
            }

            lock (_poolLock)
            {
                if (_craftedGearPool != null && _cosmeticPool != null)
                {
                    return;
                }

                var crafted = new HashSet<ItemId>();
                foreach (CraftingRecipeGroup group in _server.AssetRepository.CraftingRecipesAsset)
                {
                    foreach (CraftingRecipe recipe in group.RecipeList)
                    {
                        if (!TryGetEquipmentInfo(recipe.ItemID, out ClientItemInfo? info))
                        {
                            continue;
                        }

                        if (IsCosmetic(info))
                        {
                            continue;
                        }

                        crafted.Add((ItemId)recipe.ItemID);
                    }
                }

                var cosmetics = new HashSet<ItemId>();
                foreach ((ItemId itemId, ClientItemInfo info) in _server.AssetRepository.ClientItemInfos)
                {
                    if (info.Category != 3 || !IsCosmetic(info))
                    {
                        continue;
                    }

                    cosmetics.Add(itemId);
                }

                _craftedGearPool = crafted.ToList();
                _cosmeticPool = cosmetics.ToList();
            }
        }

        private bool TryGetEquipmentInfo(uint itemId, out ClientItemInfo? info)
        {
            if (!_server.AssetRepository.ClientItemInfos.TryGetValue((ItemId)itemId, out info))
            {
                return false;
            }

            return info.Category == 3;
        }

        private static bool IsCosmetic(ClientItemInfo info)
        {
            return CosmeticSubCategories.Contains(info.SubCategory);
        }

        private static uint GetEquipLevel(ClientItemInfo info)
        {
            if (info.Level.HasValue && info.Level.Value > 0)
            {
                return info.Level.Value;
            }

            return info.Rank > 0 ? (uint)info.Rank : 1u;
        }
    }
}

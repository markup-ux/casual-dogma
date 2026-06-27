using Arrowgene.Ddon.GameServer.Shop;
using Arrowgene.Ddon.Shared.Asset;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.GatheringItems
{
    /// <summary>
    /// Builds and caches tier/area-indexed loot pools for exploration progression drops.
    /// </summary>
    public sealed class ExplorationProgressionCatalog
    {
        /// <summary>
        /// Multi-slot outfits; excluded from exploration drops.
        /// </summary>
        private static readonly HashSet<ItemSubCategory> ExcludedExplorationSubCategories = new()
        {
            ItemSubCategory.EquipEnsemble,
        };

        /// <summary>
        /// Performance armor and weapons only (shop filter uses this).
        /// </summary>
        private static readonly HashSet<ItemSubCategory> CosmeticSubCategories = new()
        {
            ItemSubCategory.EquipClothingBody,
            ItemSubCategory.EquipClothingLeg,
            ItemSubCategory.EquipOverwear,
            ItemSubCategory.EquipEnsemble,
        };

        private static readonly HashSet<DropCategory> ExcludedMaterialCategories = new()
        {
            DropCategory.None,
            DropCategory.Consumable,
            DropCategory.Currency,
            DropCategory.Equipment,
            DropCategory.Jewelry,
            DropCategory.Unappraised,
        };

        private readonly object _lock = new();
        private readonly DdonGameServer _server;
        private Dictionary<uint, List<ItemId>>? _gearByLevel;
        private Dictionary<QuestAreaId, List<MaterialEntry>>? _materialsByArea;

        public ExplorationProgressionCatalog(DdonGameServer server)
        {
            _server = server;
        }

        public readonly record struct MaterialEntry(ItemId ItemId, uint ItemLevel, uint MinAreaRank);

        public static bool IsExplorationWearGear(ClientItemInfo info)
        {
            if (info.Category != 3)
            {
                return false;
            }

            if (ExcludedExplorationSubCategories.Contains(info.SubCategory))
            {
                return false;
            }

            return info.SubCategory is ItemSubCategory.EquipClothingBody
                or ItemSubCategory.EquipClothingLeg
                or ItemSubCategory.EquipOverwear;
        }

        public static bool IsExplorationDropGear(ClientItemInfo info)
        {
            return IsPerformanceCombatGear(info) || IsExplorationWearGear(info);
        }

        public static bool IsPerformanceCombatGear(ClientItemInfo info)
        {
            if (info.Category != 3)
            {
                return false;
            }

            if (CosmeticSubCategories.Contains(info.SubCategory))
            {
                return false;
            }

            return info.EquipSlot.HasValue;
        }

        public static bool IsJewelry(ClientItemInfo info)
        {
            return info.SubCategory >= ItemSubCategory.JewelrySubCategoryOffset
                   || info.SubCategory == ItemSubCategory.EquipJewelry;
        }

        public static bool IsWeapon(ClientItemInfo info)
        {
            return info.EquipSlot is EquipSlot.WepMain or EquipSlot.WepSub;
        }

        /// <summary>
        /// Filters out cosmetic gear, BBM jewelry shells, and non-progression combat items.
        /// Combat gear must come from the gold shop or a pawn craft recipe.
        /// </summary>
        public static bool IsEligibleExplorationGear(ClientItemInfo info, bool soldInGoldShop, bool isCraftedGear = false)
        {
            if (!IsExplorationDropGear(info))
            {
                return false;
            }

            if (info.Quality is > 0)
            {
                return false;
            }

            if (IsPerformanceCombatGear(info) && !soldInGoldShop && !isCraftedGear)
            {
                return false;
            }

            if (IsJewelry(info))
            {
                // Extra guard for stat-less template rows (Bracelet of Seizing and kin).
                if (info.Price == 0 && info.Rank >= 9 && !isCraftedGear)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Loot tier for banding. Uses the higher of equip Level and Rank so items like
        /// Hidden Seiðr (LV 1, IR 55) land in endgame buckets, not starter dungeons.
        /// </summary>
        public static uint GetEquipLevel(ClientItemInfo info)
        {
            uint rank = info.Rank > 0 ? info.Rank : 1u;
            if (info.Level.HasValue && info.Level.Value > 0)
            {
                return Math.Max(info.Level.Value, rank);
            }

            return rank;
        }

        /// <summary>
        /// When <paramref name="ignoreAreaRankWhenSynced"/> is true and the stage has a sync level,
        /// area rank is excluded so a rec-6 dungeon stays a rec-6 loot band even at high area rank.
        /// </summary>
        public static uint ResolveEffectiveTier(
            uint playerLevel,
            uint areaRank,
            uint syncRecommendedLevel,
            double areaRankTierMultiplier,
            bool ignoreAreaRankWhenSynced = true)
        {
            if (syncRecommendedLevel > 0 && ignoreAreaRankWhenSynced)
            {
                return Math.Max(playerLevel, syncRecommendedLevel);
            }

            uint areaTier = (uint)Math.Floor(areaRank * areaRankTierMultiplier);
            return Math.Max(playerLevel, Math.Max(areaTier, syncRecommendedLevel));
        }

        /// <summary>
        /// Every distinct item id currently in the character's bags, boxes, and equipment storage.
        /// </summary>
        public static HashSet<ItemId> CollectOwnedItemIds(Character character)
        {
            var owned = new HashSet<ItemId>();

            foreach (Storage storage in character.Storage.GetAllStorages().Values)
            {
                AddStorageItems(owned, storage);
            }

            AddStorageItems(owned, character.Equipment.Storage);
            return owned;
        }

        private static void AddStorageItems(HashSet<ItemId> owned, Storage storage)
        {
            foreach (Tuple<Item, uint>? entry in storage.Items)
            {
                if (entry?.Item1 == null)
                {
                    continue;
                }

                owned.Add((ItemId)entry.Item1.ItemId);
            }
        }

        public static (uint Min, uint Max) ResolveBand(uint effectiveTier, uint radius)
        {
            uint min = effectiveTier > radius ? effectiveTier - radius : 1;
            return (min, effectiveTier + radius);
        }

        public void EnsureBuilt()
        {
            if (_gearByLevel != null && _materialsByArea != null)
            {
                return;
            }

            lock (_lock)
            {
                if (_gearByLevel != null && _materialsByArea != null)
                {
                    return;
                }

                _gearByLevel = BuildGearPool();
                _materialsByArea = BuildMaterialPool();
            }
        }

        public List<ItemId> GetGearCandidates(JobId jobId, uint minLevel, uint maxLevel)
        {
            EnsureBuilt();

            var results = new List<ItemId>();
            for (uint level = minLevel; level <= maxLevel; level++)
            {
                if (!_gearByLevel!.TryGetValue(level, out List<ItemId>? items))
                {
                    continue;
                }

                foreach (ItemId itemId in items)
                {
                    if (!_server.AssetRepository.ClientItemInfos.TryGetValue(itemId, out ClientItemInfo? info))
                    {
                        continue;
                    }

                    HashSet<JobId>? jobs = info.JobIds;
                    if (jobs != null && !jobs.Contains(jobId))
                    {
                        continue;
                    }

                    results.Add(itemId);
                }
            }

            return results;
        }

        public List<MaterialEntry> GetMaterialCandidates(QuestAreaId areaId, uint areaRank, uint minLevel, uint maxLevel)
        {
            EnsureBuilt();

            if (!_materialsByArea!.TryGetValue(areaId, out List<MaterialEntry>? entries))
            {
                return [];
            }

            return entries
                .Where(entry => entry.MinAreaRank <= areaRank
                                && entry.ItemLevel >= minLevel
                                && entry.ItemLevel <= maxLevel)
                .ToList();
        }

        private Dictionary<uint, List<ItemId>> BuildGearPool()
        {
            var pool = new Dictionary<uint, List<ItemId>>();
            var seen = new HashSet<ItemId>();

            foreach (uint itemId in _server.ShopManager.GetGoldShopItemIds())
            {
                if (!TryAddGearItem(pool, seen, (ItemId)itemId, soldInGoldShop: true))
                {
                    continue;
                }
            }

            foreach (ItemId itemId in GetCraftableCombatGearIds())
            {
                TryAddGearItem(pool, seen, itemId, isCraftedGear: true);
            }

            // Fallback: wear/cosmetic gear not already indexed from shop or recipes.
            foreach ((ItemId itemId, ClientItemInfo info) in _server.AssetRepository.ClientItemInfos)
            {
                if (seen.Contains(itemId))
                {
                    continue;
                }

                if (!IsExplorationWearGear(info))
                {
                    continue;
                }

                TryAddGearItem(pool, seen, itemId, info, soldInGoldShop: false);
            }

            return pool;
        }

        private IEnumerable<ItemId> GetCraftableCombatGearIds()
        {
            foreach (CraftingRecipeGroup group in _server.AssetRepository.CraftingRecipesAsset)
            {
                foreach (CraftingRecipe recipe in group.RecipeList)
                {
                    if (!_server.AssetRepository.ClientItemInfos.TryGetValue(
                            (ItemId)recipe.ItemID,
                            out ClientItemInfo? info))
                    {
                        continue;
                    }

                    if (info.Category != 3 || IsExplorationWearGear(info))
                    {
                        continue;
                    }

                    yield return (ItemId)recipe.ItemID;
                }
            }
        }

        private bool TryAddGearItem(
            Dictionary<uint, List<ItemId>> pool,
            HashSet<ItemId> seen,
            ItemId itemId,
            ClientItemInfo? info = null,
            bool soldInGoldShop = false,
            bool isCraftedGear = false)
        {
            info ??= _server.AssetRepository.ClientItemInfos.GetValueOrDefault(itemId);
            soldInGoldShop |= _server.ShopManager.IsSoldInGoldShop((uint)itemId);
            if (info == null || !IsEligibleExplorationGear(info, soldInGoldShop, isCraftedGear))
            {
                return false;
            }

            if (!seen.Add(itemId))
            {
                return false;
            }

            uint level = GetEquipLevel(info);
            if (!pool.TryGetValue(level, out List<ItemId>? list))
            {
                list = [];
                pool[level] = list;
            }

            list.Add(itemId);
            return true;
        }

        private Dictionary<QuestAreaId, List<MaterialEntry>> BuildMaterialPool()
        {
            var pool = new Dictionary<QuestAreaId, List<MaterialEntry>>();
            DefaultGatheringDropsAsset asset = _server.AssetRepository.DefaultGatheringDropsAsset;

            foreach ((QuestAreaId areaId, Dictionary<DropCategory, List<DefaultGatheringDrop>> categories) in asset.AreaDefaultDrops)
            {
                foreach ((DropCategory category, List<DefaultGatheringDrop> drops) in categories)
                {
                    if (ExcludedMaterialCategories.Contains(category))
                    {
                        continue;
                    }

                    foreach (DefaultGatheringDrop drop in drops)
                    {
                        if (!_server.AssetRepository.ClientItemInfos.TryGetValue(drop.ItemId, out ClientItemInfo? info))
                        {
                            continue;
                        }

                        if (info.Category != 2)
                        {
                            continue;
                        }

                        if (!pool.TryGetValue(areaId, out List<MaterialEntry>? list))
                        {
                            list = [];
                            pool[areaId] = list;
                        }

                        list.Add(new MaterialEntry(drop.ItemId, drop.ItemLevel, drop.MinAreaRank));
                    }
                }
            }

            foreach (List<MaterialEntry> list in pool.Values)
            {
                list.Sort((a, b) => a.ItemLevel.CompareTo(b.ItemLevel));
            }

            return pool;
        }
    }
}

using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.GatheringItems.Generators
{
    /// <summary>
    /// Personal, tier-aware finished gear and regional craft-material drops for Casual Dogma
    /// exploration progression. Tier resolves from player job level, area rank, and sync level.
    /// </summary>
    public class ExplorationProgressionDropGenerator : IDropGenerator
    {
        private static readonly EquipSlot[] PerformanceEquipSlots =
        [
            EquipSlot.WepMain,
            EquipSlot.WepSub,
            EquipSlot.ArmorHelm,
            EquipSlot.ArmorBody,
            EquipSlot.WearBody,
            EquipSlot.ArmorArm,
            EquipSlot.ArmorLeg,
            EquipSlot.WearLeg,
            EquipSlot.Accessory,
            EquipSlot.Jewelry1,
            EquipSlot.Jewelry2,
            EquipSlot.Jewelry3,
            EquipSlot.Jewelry4,
            EquipSlot.Jewelry5,
            EquipSlot.Lantern,
        ];

        private readonly DdonGameServer _server;
        private readonly ExplorationProgressionCatalog _catalog;

        public ExplorationProgressionDropGenerator(DdonGameServer server)
        {
            _server = server;
            _catalog = new ExplorationProgressionCatalog(server);
        }

        public List<InstancedGatheringItem> Generate(GameClient client, InstancedEnemy enemyKilled)
        {
            var settings = _server.GameSettings.GameServerSettings;
            if (!settings.EnableExplorationProgressionDrops)
            {
                return [];
            }

            if (client.GameMode != GameMode.Normal)
            {
                return [];
            }

            if (client.Character?.ActiveCharacterJobData == null)
            {
                return [];
            }

            StageInfo stageInfo;
            try
            {
                stageInfo = Stage.StageInfoFromStageLayoutId(enemyKilled.StageLayoutId);
            }
            catch (KeyNotFoundException)
            {
                return [];
            }

            QuestAreaId areaId = stageInfo.AreaId;
            if (areaId == QuestAreaId.None)
            {
                areaId = client.Character.AreaId;
            }

            uint areaRank = client.Character.AreaRanks.GetValueOrDefault(areaId)?.Rank ?? 0;
            uint playerLevel = client.Character.ActiveCharacterJobData.Lv;
            uint syncLevel = _server.LevelSyncManager.GetRecommendedLevel(enemyKilled.StageLayoutId.Id);
            uint effectiveTier = ExplorationProgressionCatalog.ResolveEffectiveTier(
                playerLevel,
                areaRank,
                syncLevel,
                settings.ExplorationAreaRankTierMultiplier,
                settings.ExplorationIgnoreAreaRankInSyncZones);

            (uint gearMin, uint gearMax) = ExplorationProgressionCatalog.ResolveBand(
                effectiveTier,
                settings.ExplorationLootBandRadius);
            (uint materialMin, uint materialMax) = ExplorationProgressionCatalog.ResolveBand(
                effectiveTier,
                settings.ExplorationMaterialLootBandRadius);

            bool isBoss = enemyKilled.IsBossGauge || enemyKilled.IsAreaBoss;
            var results = new List<InstancedGatheringItem>();

            double gearChance = isBoss
                ? settings.ExplorationGearBossDropChance
                : settings.ExplorationGearDropChance;

            uint emptyPerformanceSlots = CountEmptyPerformanceSlots(client);
            if (emptyPerformanceSlots > 0)
            {
                gearChance = Math.Min(
                    1.0,
                    gearChance + settings.ExplorationEmptySlotDropChanceBonus * emptyPerformanceSlots);
            }

            uint pityThreshold = settings.ExplorationGearPityKillThreshold;
            bool forceGearDrop = !isBoss
                                 && pityThreshold > 0
                                 && client.ExplorationKillsWithoutGear + 1 >= pityThreshold;

            bool rolledGear = false;
            if (forceGearDrop || Random.Shared.NextDouble() <= gearChance)
            {
                ItemId? gear = RollGear(client, gearMin, gearMax, allowBandExpansion: forceGearDrop);
                if (gear.HasValue)
                {
                    results.Add(new InstancedGatheringItem
                    {
                        ItemId = gear.Value,
                        ItemNum = 1,
                        Quality = 0,
                    });
                    rolledGear = true;
                }
            }

            if (rolledGear)
            {
                client.ExplorationKillsWithoutGear = 0;
            }
            else
            {
                client.ExplorationKillsWithoutGear++;
            }

            double materialChance = isBoss
                ? settings.ExplorationMaterialBossDropChance
                : settings.ExplorationMaterialDropChance;
            if (Random.Shared.NextDouble() <= materialChance)
            {
                ItemId? material = RollMaterial(areaId, areaRank, materialMin, materialMax);
                if (material.HasValue)
                {
                    results.Add(new InstancedGatheringItem
                    {
                        ItemId = material.Value,
                        ItemNum = 1,
                        Quality = 0,
                    });
                }
            }

            return results;
        }

        private ItemId? RollGear(GameClient client, uint minLevel, uint maxLevel, bool allowBandExpansion = false)
        {
            double weaponFirstChance = _server.GameSettings.GameServerSettings.ExplorationWeaponFirstRollChance;
            if (weaponFirstChance > 0 && Random.Shared.NextDouble() <= weaponFirstChance)
            {
                ItemId? weapon = TryRollGear(client, minLevel, maxLevel, weaponsOnly: true);
                if (weapon.HasValue)
                {
                    return weapon;
                }
            }

            ItemId? gear = TryRollGear(client, minLevel, maxLevel, weaponsOnly: false);
            if (gear.HasValue || !allowBandExpansion)
            {
                return gear;
            }

            uint expandedMin = minLevel > 1 ? minLevel - 1 : 1;
            if (weaponFirstChance > 0 && Random.Shared.NextDouble() <= weaponFirstChance)
            {
                ItemId? weapon = TryRollGear(client, expandedMin, maxLevel + 1, weaponsOnly: true);
                if (weapon.HasValue)
                {
                    return weapon;
                }
            }

            return TryRollGear(client, expandedMin, maxLevel + 1, weaponsOnly: false);
        }

        private ItemId? TryRollGear(GameClient client, uint minLevel, uint maxLevel, bool weaponsOnly)
        {
            JobId jobId = client.Character.Job;
            HashSet<ItemId> ownedItemIds = ExplorationProgressionCatalog.CollectOwnedItemIds(client.Character);

            List<ItemId> candidates = _catalog.GetGearCandidates(jobId, minLevel, maxLevel)
                .Where(itemId => !ownedItemIds.Contains(itemId))
                .Where(itemId =>
                {
                    if (!weaponsOnly)
                    {
                        return true;
                    }

                    ClientItemInfo info = _server.AssetRepository.ClientItemInfos[itemId];
                    return ExplorationProgressionCatalog.IsWeapon(info);
                })
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            Dictionary<EquipSlot, uint> equippedLevels = GetEquippedLevels(client);
            bool mainWeaponEmpty = !equippedLevels.ContainsKey(EquipSlot.WepMain);
            var weights = new List<int>(candidates.Count);
            foreach (ItemId itemId in candidates)
            {
                ClientItemInfo info = _server.AssetRepository.ClientItemInfos[itemId];
                uint itemLevel = ExplorationProgressionCatalog.GetEquipLevel(info);
                uint equippedLevel = 0;
                if (info.EquipSlot.HasValue && equippedLevels.TryGetValue(info.EquipSlot.Value, out uint slotLevel))
                {
                    equippedLevel = slotLevel;
                }

                int weight = 2;
                if (equippedLevel == 0)
                {
                    // Empty slot: strongly prefer filling the set.
                    weight += 12;
                }
                else if (itemLevel > equippedLevel)
                {
                    weight += Math.Min(8, (int)(itemLevel - equippedLevel));
                }
                else
                {
                    // Already wearing something at or above this tier in the slot.
                    weight = 1;
                }

                if (ExplorationProgressionCatalog.IsWeapon(info))
                {
                    weight += 18;
                    if (info.EquipSlot == EquipSlot.WepMain && mainWeaponEmpty)
                    {
                        weight += 25;
                    }
                }

                weights.Add(weight);
            }

            return WeightedPick(candidates, weights);
        }

        private uint CountEmptyPerformanceSlots(GameClient client)
        {
            var occupied = new HashSet<EquipSlot>();
            foreach ((Item? item, ushort _, EquipType _) in client.Character.Equipment.GetItemsTuple())
            {
                if (item == null)
                {
                    continue;
                }

                if (!_server.AssetRepository.ClientItemInfos.TryGetValue(
                        (ItemId)item.ItemId,
                        out ClientItemInfo? info)
                    || !info.EquipSlot.HasValue)
                {
                    continue;
                }

                occupied.Add(info.EquipSlot.Value);
            }

            uint empty = 0;
            foreach (EquipSlot slot in PerformanceEquipSlots)
            {
                if (!occupied.Contains(slot))
                {
                    empty++;
                }
            }

            return empty;
        }

        private ItemId? RollMaterial(QuestAreaId areaId, uint areaRank, uint minLevel, uint maxLevel)
        {
            List<ExplorationProgressionCatalog.MaterialEntry> candidates =
                _catalog.GetMaterialCandidates(areaId, areaRank, minLevel, maxLevel);
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Shared.Next(candidates.Count)].ItemId;
        }

        private Dictionary<EquipSlot, uint> GetEquippedLevels(GameClient client)
        {
            var levels = new Dictionary<EquipSlot, uint>();
            foreach ((Item? item, ushort _, EquipType _) in client.Character.Equipment.GetItemsTuple())
            {
                if (item == null)
                {
                    continue;
                }

                if (!_server.AssetRepository.ClientItemInfos.TryGetValue((ItemId)item.ItemId, out ClientItemInfo? info))
                {
                    continue;
                }

                if (!info.EquipSlot.HasValue)
                {
                    continue;
                }

                uint level = ExplorationProgressionCatalog.GetEquipLevel(info);
                EquipSlot slot = info.EquipSlot.Value;
                if (!levels.TryGetValue(slot, out uint existing) || level > existing)
                {
                    levels[slot] = level;
                }
            }

            return levels;
        }

        private static ItemId? WeightedPick(List<ItemId> items, List<int> weights)
        {
            int total = weights.Sum();
            if (total <= 0)
            {
                return items[Random.Shared.Next(items.Count)];
            }

            int roll = Random.Shared.Next(total);
            for (int i = 0; i < items.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                {
                    return items[i];
                }
            }

            return items[^1];
        }
    }
}

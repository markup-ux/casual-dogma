#load "libs.csx"

private static class Settings
{
    public static double DefaultGatherDropsRandomBias
    {
        get
        {
            return LibDdon.GetSetting<double>("GameServerSettings", "DefaultGatherDropsRandomBias");
        }
    }

    public static int DefaultGatherDropMaxSlots
    {
        get
        {
            return LibDdon.GetSetting<int>("GameServerSettings", "DefaultGatherDropMaxSlots");
        }
    }

    public static int MaximumDropsPerDefaultGatherRoll
    {
        get
        {
            return LibDdon.GetSetting<int>("GameServerSettings", "MaximumDropsPerDefaultGatherRoll");
        }
    }
}

private class GatheringExtensions
{
    public const int MIN_GATHERING_RANK = 1;
    public const int MAX_GATHERING_RANK = 11;

    private static readonly Dictionary<GatheringType, int> ChestModifierRank = new Dictionary<GatheringType, int>()
    {
        [GatheringType.OM_GATHER_NONE] = 0,
        [GatheringType.OM_GATHER_TREA_OLD] = 0,
        [GatheringType.OM_GATHER_SHIP] = 1,
        [GatheringType.OM_GATHER_TREA_TREE] = 1,
        [GatheringType.OM_GATHER_KEY_LV1] = 1,
        [GatheringType.OM_GATHER_KEY_LV2] = 2,
        [GatheringType.OM_GATHER_TREA_IRON] = 2,
        [GatheringType.OM_GATHER_KEY_LV3] = 3,
        [GatheringType.OM_GATHER_KEY_LV4] = 4,
    };

    private static readonly Dictionary<OmGatheringPoint, int> TreasureChestBaseRank = new Dictionary<OmGatheringPoint, int>()
    {
        [OmGatheringPoint.BrownChest] = 1,
        [OmGatheringPoint.IronChest] = 2,
        [OmGatheringPoint.TreasureChest] = 3,
        [OmGatheringPoint.SmallRoundChest0] = 3,
        [OmGatheringPoint.SmallRoundChest1] = 3,
        [OmGatheringPoint.BronzeChest] = 4,
        [OmGatheringPoint.SilverChest] = 5,
        [OmGatheringPoint.GoldChest] = 6,
        [OmGatheringPoint.PurpleChest] = 7,
    };

    public static int GetTreasureChestRank(GatheringSpotInfo spotInfo)
    {
        if (!spotInfo.UnitId.IsTreasureChest())
        {
            return MIN_GATHERING_RANK;
        }
        return TreasureChestBaseRank[spotInfo.UnitId] + ChestModifierRank[spotInfo.GatheringType];
    }

    private static readonly Dictionary<GatheringType, int> LumberModifierRank = new Dictionary<GatheringType, int>()
    {
        [GatheringType.OM_GATHER_NONE] = 0,
        [GatheringType.OM_GATHER_TREE_LV1] = 1,
        [GatheringType.OM_GATHER_TREE_LV2] = 2,
        [GatheringType.OM_GATHER_TREE_LV3] = 3,
        [GatheringType.OM_GATHER_TREE_LV4] = 4,
    };

    private static readonly Dictionary<GatheringType, int> GemstoneModifierRank = new Dictionary<GatheringType, int>()
    {
        [GatheringType.OM_GATHER_NONE] = 0,
        [GatheringType.OM_GATHER_JWL_LV1] = 1,
        [GatheringType.OM_GATHER_JWL_LV2] = 2,
        [GatheringType.OM_GATHER_JWL_LV3] = 3,
    };

    private static readonly Dictionary<GatheringType, int> OreModiferRank = new Dictionary<GatheringType, int>()
    {
        [GatheringType.OM_GATHER_NONE] = 0,
        [GatheringType.OM_GATHER_CRST_LV1] = 1,
        [GatheringType.OM_GATHER_CRST_LV2] = 2,
        [GatheringType.OM_GATHER_CRST_LV3] = 3,
        [GatheringType.OM_GATHER_CRST_LV4] = 4,
    };

    private static readonly Dictionary<GatheringType, int> CorpseModifier = new Dictionary<GatheringType, int>()
    {
        [GatheringType.OM_GATHER_NONE] = 0,
        [GatheringType.OM_GATHER_CORPSE] = 1,
        [GatheringType.OM_GATHER_DRAGON] = 2,
    };


    public static int GetGatherSpotRank(GatheringSpotInfo spotInfo)
    {
        switch (spotInfo.UnitId.GetGatheringPointType())
        {
            case GatheringPointType.TreasureChest:
                return GetTreasureChestRank(spotInfo);
            case GatheringPointType.Lumber:
                return MIN_GATHERING_RANK + LumberModifierRank[spotInfo.GatheringType];
            case GatheringPointType.Gemstone:
                return MIN_GATHERING_RANK + GemstoneModifierRank[spotInfo.GatheringType];
            case GatheringPointType.Ore:
                return MIN_GATHERING_RANK + OreModiferRank[spotInfo.GatheringType];
            case GatheringPointType.Corpse:
                return MIN_GATHERING_RANK + CorpseModifier[spotInfo.GatheringType];
        }
        return MIN_GATHERING_RANK;
    }
}

public class Mixin : IDefaultGatherMixin
{
    private static readonly ILogger Logger = LogProvider.Logger(typeof(Mixin));

    public override List<InstancedGatheringItem> GenerateGatheringDrops(GameClient client, StageLayoutId stageLayoutId, uint index)
    {
        if (StageManager.IsBitterBlackMazeStageId(stageLayoutId) || StageManager.IsEpitaphRoadStageId(stageLayoutId))
        {
            return new();
        }

        List<InstancedGatheringItem> results = new();
        if (LibDdon.Assets.DefaultGatheringDropsAsset.SpotDefaultDrops.ContainsKey((stageLayoutId, index)))
        {
            return new();
        }

        return HandleAreaDrops(client, stageLayoutId, index);
    }

    private List<InstancedGatheringItem> HandleAreaDrops(GameClient client, StageLayoutId stageLayoutId, uint index)
    {
        var stage = Stage.StageInfoFromStageLayoutId(stageLayoutId);
        if (!LibDdon.Assets.GatheringSpotInfoAsset.GatheringInfoMap.ContainsKey(stage.StageNo))
        {
            return new();
        }

        var stageSpots = LibDdon.Assets.GatheringSpotInfoAsset.GatheringInfoMap[stage.StageNo];
        if (!stageSpots.ContainsKey((stageLayoutId.GroupId, index)))
        {
            return new();
        }

        var areaId = stage.AreaId;
        if (stage.StageId == Stage.Lestania.StageId)
        {
            areaId = client.Character.AreaId;
            if (areaId == QuestAreaId.None)
            {
                // Default to hidell plains so something can drop
                areaId = QuestAreaId.HidellPlains;
            }
        }

        if (!LibDdon.Assets.DefaultGatheringDropsAsset.AreaDefaultDrops.ContainsKey(areaId))
        {
            return new();
        }

        var spotInfo = stageSpots[(stageLayoutId.GroupId, index)];

        Logger.Debug($"{stageLayoutId}.{index}  OmType={spotInfo.UnitId}, GatheringType={spotInfo.GatheringType}");

        var dropTable = GetDropCategoriesForSpot(spotInfo)
            .Select(x => LibDdon.Assets.DefaultGatheringDropsAsset.AreaDefaultDrops[areaId][x])
            .Where(x => x.Count > 0)
            .SelectMany(x => x)
            .Where(x => x.StageId == stage.StageId)
            .GroupBy(x => x.ItemId)
            .Select(x => x.First())
            .OrderBy(x => x.ItemLevel)
            .ToDictionary(x => x.ItemId, x => x);
        if (dropTable.Count == 0)
        {
            return new();
        }

        var gatherPointRank = GatheringExtensions.GetGatherSpotRank(spotInfo);
        var potentialSlots = Settings.DefaultGatherDropMaxSlots + (int)(gatherPointRank - GatheringExtensions.MIN_GATHERING_RANK);

        // Create a list which holds all the items we can roll
        var rolls = dropTable.Keys.ToList();

        var bias = FindRollBiasForSpot(gatherPointRank);

        // Determine how many items to generate
        var slots = Random.Shared.WeightedNext(1, potentialSlots + 1, Settings.DefaultGatherDropsRandomBias);
        return RollDrops(slots, rolls, dropTable, bias);
    }

    private double FindRollBiasForSpot(int rank)
    {
        var preferenceScore = GatheringExtensions.MAX_GATHERING_RANK - rank + 1;

        double minBias = Settings.DefaultGatherDropsRandomBias; // Higher makes more skewed to common items
        double maxBias = 0.3; // lower makes it more skewed to rarer items

        double t = (rank - 1.0) / (GatheringExtensions.MAX_GATHERING_RANK - 1.0); // Normalized rank [0, 1]
        return minBias * Math.Pow(maxBias / minBias, t);
    }

    private List<InstancedGatheringItem> RollDrops(int slots, List<ItemId> rolls, Dictionary<ItemId, DefaultGatheringDrop> dropTable, double rollBias)
    {
        Logger.Debug($"GatheringBias={rollBias}");

        var results = new List<InstancedGatheringItem>();
        for (int i = 0; i < slots && rolls.Count > 0; i++)
        {
            var itemId = rolls[Random.Shared.WeightedNext(rolls.Count, rollBias)];

            var item = dropTable[itemId];

            var drop = new InstancedGatheringItem()
            {
                ItemId = item.ItemId,
                Quality = item.Quality
            };

            if (item.MaxAmount == 0)
            {
                drop.ItemNum = (uint)Random.Shared.WeightedNext((int)item.MinAmount, Settings.MaximumDropsPerDefaultGatherRoll + 1, Settings.DefaultGatherDropsRandomBias);
            }
            else
            {
                drop.ItemNum = (uint)Random.Shared.WeightedNext((int)item.MinAmount, (int)item.MaxAmount + 1, Settings.DefaultGatherDropsRandomBias);
            }

            if (drop.ItemNum == 0)
            {
                // Skip item since none got generated
                // TODO: Is this a configuration issue?
                continue;
            }

            rolls.Remove(itemId);
            results.Add(drop);
        }
        return results;
    }

    private static Dictionary<GatheringPointType, List<DropCategory>> DropCategories = new Dictionary<GatheringPointType, List<DropCategory>>()
    {
        [GatheringPointType.Alchemy] = [DropCategory.Liquids, DropCategory.Lumber, DropCategory.Other],
        [GatheringPointType.Box] = [
            DropCategory.Consumable, DropCategory.Dye, DropCategory.Thread, DropCategory.Fabric, DropCategory.Ingots, DropCategory.Ore,
            DropCategory.Gemstones, DropCategory.Scrolls, DropCategory.Leather
        ],
        [GatheringPointType.Corpse] = [
            DropCategory.Meat, DropCategory.Claws, DropCategory.Bones, DropCategory.Fang, DropCategory.Hides, DropCategory.Horns,
            DropCategory.Furs, DropCategory.Feathers
        ],
        [GatheringPointType.Furniture] = [DropCategory.Consumable, DropCategory.Dye, DropCategory.Thread, DropCategory.Fabric, DropCategory.CrestArmor, DropCategory.CrestWeapon],
        [GatheringPointType.Gemstone] = [DropCategory.Gemstones],
        [GatheringPointType.Lumber] = [DropCategory.Lumber],
        [GatheringPointType.Mushroom] = [DropCategory.Mushrooms],
        [GatheringPointType.Ore] = [DropCategory.Ore],
        [GatheringPointType.Plants] = [DropCategory.Plants],
        [GatheringPointType.Sand] = [DropCategory.Sand],
        [GatheringPointType.SealedTreasureChest] = [DropCategory.Equipment, DropCategory.Jewelry, DropCategory.Unappraised, DropCategory.Regional],
        [GatheringPointType.Shell] = [DropCategory.Shell],
        [GatheringPointType.TreasureChest] = DropCategoryExtension.All,
        [GatheringPointType.Twinkle] = DropCategoryExtension.All,
        [GatheringPointType.Water] = [DropCategory.Liquids],
    };

    private List<DropCategory> GetDropCategoriesForSpot(GatheringSpotInfo spotInfo)
    {
        var gatheringPointType = spotInfo.UnitId.GetGatheringPointType();
        if (!DropCategories.ContainsKey(gatheringPointType))
        {
            return new();
        }
        return DropCategories[gatheringPointType];
    }
}

return new Mixin();

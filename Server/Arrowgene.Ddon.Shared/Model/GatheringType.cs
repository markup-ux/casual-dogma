using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Model
{
    public enum OmGatheringPoint : uint
    {
        // SetInfoOmGather
        Alchemy0 = 520080,
        Alchemy1 = 520081,
        Alchemy2 = 520111,
        Antique0 = 520110,
        Antique1 = 520111,
        Book = 520041,
        Box0 = 520070,
        Box1 = 520071,
        Corpse = 520170,
        Flower0 = 520010,
        Flower1 = 520011,
        Flower2 = 520012,
        Grass0 = 520000,
        Grass1 = 520001,
        Grass2 = 520002,
        Grass3 = 520003,
        Grass4 = 520004,
        Lumber0 = 520030,
        Lumber1 = 520031,
        Lumber2 = 520032,
        Lumber3 = 520033,
        MiningGemstone0 = 520050,
        MiningGemstone1 = 520051,
        MiningGemstone2 = 520052,
        MiningGemstone3 = 520160,
        MiningGemstone4 = 520161,
        MiningGemstone5 = 520162,
        MiningOre0 = 520160,
        MiningOre1 = 520161,
        MiningOre2 = 520162,
        MiningOre3 = 520163,
        Mushroom0 = 520020,
        Mushroom1 = 520021,
        Mushroom2 = 520022,
        Mushroom3 = 520023,
        Mushroom4 = 520024,
        OneOff0 = 513054,
        OneOff1 = 520171,
        Sand = 520060,
        Shells = 520100,
        Twinkle0 = 520170,
        Twinkle1 = 522552,
        Twinkle2 = 523240,
        Water = 520090,
        // Chain Dungeon Sealed Chests
        BrownSealedChest = 523907, // Chain Dungeon (EXM)
        TealSealedChest = 523908, // Chain Dungeon (EXM)
        // SetInfoOmTreasureBox
        IronChest = 513050,
        BrownChest = 513051,
        TreasureChest = 513052,
        BronzeChest = 513053,
        SilverChest = 513054,
        GoldChest = 513055,
        PurpleChest = 513056,
        SmallRoundChest0 = 513060, // Usually found on tables in dungeons
        SmallRoundChest1 = 513061,
        BronzeChest1 = 523241, // Chain Dungeon (EXM)
        PearlescentChest1 = 523242, // Chain Dungeon (EXM)
        // SetInfoOmTreasureBoxG
        OrangeSealedChest = 513130, // (BBM)
        PurpleSealedChest = 513133, // (BBM)
        PearlescentChest = 513134, // 3.4 EXM reward treasure box
    }

    public enum GatheringPointType
    {
        None,
        Alchemy,
        Box,
        Corpse,
        Furniture,
        Gemstone,
        Lumber,
        Mushroom,
        OneOff,
        Ore,
        Plants,
        Sand,
        SealedTreasureChest,
        Shell,
        TreasureChest,
        Twinkle,
        Water,
    }

    public static class GatheringPointTypeExtension
    {
        private static readonly Dictionary<OmGatheringPoint, GatheringPointType> OmGatherToGatheringPointTypeMap = new Dictionary<OmGatheringPoint, GatheringPointType>()
        {
            // OmSetGather
            [OmGatheringPoint.Alchemy0] = GatheringPointType.Alchemy,
            [OmGatheringPoint.Alchemy1] = GatheringPointType.Alchemy,
            [OmGatheringPoint.Alchemy2] = GatheringPointType.Alchemy,
            [OmGatheringPoint.Antique0] = GatheringPointType.Furniture,
            [OmGatheringPoint.Antique1] = GatheringPointType.Furniture,
            [OmGatheringPoint.Book] = GatheringPointType.Furniture,
            [OmGatheringPoint.Box0] = GatheringPointType.Box,
            [OmGatheringPoint.Box1] = GatheringPointType.Box,
            [OmGatheringPoint.Corpse] = GatheringPointType.Corpse,
            [OmGatheringPoint.Flower0] = GatheringPointType.Plants,
            [OmGatheringPoint.Flower1] = GatheringPointType.Plants,
            [OmGatheringPoint.Flower2] = GatheringPointType.Plants,
            [OmGatheringPoint.MiningGemstone0] = GatheringPointType.Gemstone,
            [OmGatheringPoint.MiningGemstone1] = GatheringPointType.Gemstone,
            [OmGatheringPoint.MiningGemstone2] = GatheringPointType.Gemstone,
            [OmGatheringPoint.MiningGemstone3] = GatheringPointType.Gemstone,
            [OmGatheringPoint.MiningGemstone4] = GatheringPointType.Gemstone,
            [OmGatheringPoint.MiningGemstone5] = GatheringPointType.Gemstone,
            [OmGatheringPoint.Grass0] = GatheringPointType.Plants,
            [OmGatheringPoint.Grass1] = GatheringPointType.Plants,
            [OmGatheringPoint.Grass2] = GatheringPointType.Plants,
            [OmGatheringPoint.Grass3] = GatheringPointType.Plants,
            [OmGatheringPoint.Grass4] = GatheringPointType.Plants,
            [OmGatheringPoint.Lumber0] = GatheringPointType.Lumber,
            [OmGatheringPoint.Lumber1] = GatheringPointType.Lumber,
            [OmGatheringPoint.Lumber2] = GatheringPointType.Lumber,
            [OmGatheringPoint.Lumber3] = GatheringPointType.Lumber,
            [OmGatheringPoint.Mushroom0] = GatheringPointType.Mushroom,
            [OmGatheringPoint.Mushroom1] = GatheringPointType.Mushroom,
            [OmGatheringPoint.Mushroom2] = GatheringPointType.Mushroom,
            [OmGatheringPoint.Mushroom3] = GatheringPointType.Mushroom,
            [OmGatheringPoint.Mushroom4] = GatheringPointType.Mushroom,
            [OmGatheringPoint.MiningOre0] = GatheringPointType.Ore,
            [OmGatheringPoint.MiningOre1] = GatheringPointType.Ore,
            [OmGatheringPoint.MiningOre2] = GatheringPointType.Ore,
            [OmGatheringPoint.MiningOre3] = GatheringPointType.Ore,
            [OmGatheringPoint.Sand] = GatheringPointType.Sand,
            [OmGatheringPoint.Shells] = GatheringPointType.Shell,
            [OmGatheringPoint.Twinkle0] = GatheringPointType.Twinkle,
            [OmGatheringPoint.Twinkle1] = GatheringPointType.Twinkle,
            [OmGatheringPoint.Twinkle2] = GatheringPointType.Twinkle,
            [OmGatheringPoint.Water] = GatheringPointType.Water,
            // OneOffGathering (special)
            [OmGatheringPoint.OneOff0] = GatheringPointType.OneOff,
            [OmGatheringPoint.OneOff1] = GatheringPointType.OneOff,
            // OmTreasureBox
            [OmGatheringPoint.IronChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.BrownChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.TreasureChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.BronzeChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.SilverChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.GoldChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.PurpleChest] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.SmallRoundChest0] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.SmallRoundChest1] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.BrownSealedChest] = GatheringPointType.SealedTreasureChest,
            [OmGatheringPoint.TealSealedChest] = GatheringPointType.SealedTreasureChest,
            [OmGatheringPoint.BronzeChest1] = GatheringPointType.TreasureChest,
            [OmGatheringPoint.PearlescentChest1] = GatheringPointType.TreasureChest,
            // OmTreasureBoxG
            [OmGatheringPoint.OrangeSealedChest] = GatheringPointType.SealedTreasureChest,
            [OmGatheringPoint.PurpleSealedChest] = GatheringPointType.SealedTreasureChest,
            [OmGatheringPoint.PearlescentChest] = GatheringPointType.SealedTreasureChest,
        };

        public static GatheringPointType GetGatheringPointType(this OmGatheringPoint omGatheringPoint)
        {
            if (OmGatherToGatheringPointTypeMap.ContainsKey(omGatheringPoint))
            {
                return OmGatherToGatheringPointTypeMap[omGatheringPoint];
            }
            return GatheringPointType.None;
        }

        public static bool IsTreasureChest(this OmGatheringPoint omGatheringPoint)
        {
            var gatheringPointType = GetGatheringPointType(omGatheringPoint);
            return gatheringPointType == GatheringPointType.TreasureChest || gatheringPointType == GatheringPointType.SealedTreasureChest;
        }

        public static bool IsLumberGatheringPoint(this OmGatheringPoint omGatheringPoint)
        {
            return GetGatheringPointType(omGatheringPoint) == GatheringPointType.Lumber;
        }

        public static bool IsOreMiningPoint(this OmGatheringPoint omGatheringPoint)
        {
            return GetGatheringPointType(omGatheringPoint) == GatheringPointType.Ore;
        }

        public static bool IsGemstoneMiningPoint(this OmGatheringPoint omGatheringPoint)
        {
            return GetGatheringPointType(omGatheringPoint) == GatheringPointType.Gemstone;
        }
    }

    public enum GatheringType : uint
    {
        OM_GATHER_NONE = 0x0,
        OM_GATHER_TREE_LV1 = 0x1,
        OM_GATHER_TREE_LV2 = 0x2,
        OM_GATHER_TREE_LV3 = 0x3,
        OM_GATHER_TREE_LV4 = 0x4,
        OM_GATHER_JWL_LV1 = 0x5,
        OM_GATHER_JWL_LV2 = 0x6,
        OM_GATHER_JWL_LV3 = 0x7,
        OM_GATHER_CRST_LV1 = 0x8,
        OM_GATHER_CRST_LV2 = 0x9,
        OM_GATHER_CRST_LV3 = 0xA,
        OM_GATHER_CRST_LV4 = 0xB,
        OM_GATHER_KEY_LV1 = 0xC,
        OM_GATHER_KEY_LV2 = 0xD,
        OM_GATHER_KEY_LV3 = 0xE,
        OM_GATHER_TREA_IRON = 0xF,
        OM_GATHER_DRAGON = 0x10,
        OM_GATHER_CORPSE = 0x11,
        OM_GATHER_SHIP = 0x12,
        OM_GATHER_GRASS = 0x13,
        OM_GATHER_FLOWER = 0x14,
        OM_GATHER_MUSHROOM = 0x15,
        OM_GATHER_CLOTH = 0x16,
        OM_GATHER_BOOK = 0x17,
        OM_GATHER_SAND = 0x18,
        OM_GATHER_BOX = 0x19,
        OM_GATHER_ALCHEMY = 0x1A,
        OM_GATHER_WATER = 0x1B,
        OM_GATHER_SHELL = 0x1C,
        OM_GATHER_ANTIQUE = 0x1D,
        OM_GATHER_TWINKLE = 0x1E,
        OM_GATHER_TREA_OLD = 0x1F,
        OM_GATHER_TREA_TREE = 0x20,
        OM_GATHER_TREA_SILVER = 0x21,
        OM_GATHER_TREA_GOLD = 0x22,
        OM_GATHER_KEY_LV4 = 0x23,
        OM_GATHER_ONE_OFF = 0x24,
    }

    public static class GatherTypeExtension
    {
        private static List<GatheringType> LockedChestType = new()
        {
            GatheringType.OM_GATHER_KEY_LV1,
            GatheringType.OM_GATHER_KEY_LV2,
            GatheringType.OM_GATHER_KEY_LV3,
            GatheringType.OM_GATHER_KEY_LV4
        };

        public static bool IsLockedChest(this GatheringType gatheringType)
        {
            return LockedChestType.Contains(gatheringType);
        }
    }
}

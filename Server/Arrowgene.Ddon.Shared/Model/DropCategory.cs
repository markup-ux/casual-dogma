using System;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.Shared.Model
{
    public enum DropCategory : uint
    {
        None,
        Bones, // MaterialAnimalBone
        Claws,
        Consumable,
        Currency,
        CrestArmor, // MaterialCrestArmor
        CrestWeapon, // MaterialCrestWeapon
        Dye, // MaterialSpecialDye
        DragonAbility, // MaterialDragonAbility
        Equipment,
        Fabric, // MaterialSewingCloth
        Fang, // MaterialAnimalFang
        Feathers, //MaterialAnimalFeather
        Furs, // MaterialSewingFur
        Gemstones, //MaterialInorganicGem
        Hides, // MaterialAnimalSkin
        Horns, // MaterialAnimalHorn
        Ingots, // MaterialInorganicMetal
        Jewelry,
        Leather,
        Liquids, // MaterialInorganicLiquid
        Lumber, // MaterialPlantLumber
        Meat, // MaterialAnimalMeat
        Mushrooms, // MaterialPlantMushroom
        Ore, // MaterialInorganicOre
        Other, // MaterialSpecialOther
        PawnInspiration, // MaterialPawnInspiration
        Plants, // MaterialPlantGrass
        RefiningArmor, // MaterialRefiningArmor
        RefiningWeapon, // MaterialRefiningWeapon
        Regional, //  MaterialRegionalMaterial
        Sand, // MaterialInorganicSand
        Scrolls, // MaterialSpecialScroll
        Shell, // MaterialUnusedShell
        SlayerStone, // MaterialSlayerStone
        Thread, // MaterialSewingString
        Unappraised, // MaterialAppraisedItem
    }

    public static class DropCategoryExtension
    {
        public static readonly List<DropCategory> All = Enum.GetValues(typeof(DropCategory)).Cast<DropCategory>().Where(x => x != DropCategory.None).ToList();
    }
}

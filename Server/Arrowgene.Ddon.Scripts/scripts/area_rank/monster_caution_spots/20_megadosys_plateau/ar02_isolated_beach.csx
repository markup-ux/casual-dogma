/**
 * @brief Enemy Spot in "Megadosys Plateau" for "Isolated Beach"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MegadosysPlateau.AsStageLayoutId(35);
    public override QuestAreaId AreaId => QuestAreaId.MegadosysPlateau;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.MegadosysPlateauLiberationArmySupport;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 92, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 92, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Witch, 92, 21000, 2)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 92, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 92, 4200, 4),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.SpiritPurifierStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.SpiritPurifierStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.HighlandAkadama, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.SpiritPurifierStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.SpiritPurifierStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.HighlandAkadama, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.SpiritPurifierStone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SpiritPurifierStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.HighlandAkadama, 1, 1, DropRate.UNCOMMON);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.SpiritPurifierStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.SpiritPurifierStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.HighlandAkadama, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.SpiritPurifierStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.SpiritPurifierStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.HighlandAkadama, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

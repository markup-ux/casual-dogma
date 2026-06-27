/**
 * @brief Enemy Spot in "Urteca Mountains" for "Pyre Saurian Colony"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.UrtecaMountains.AsStageLayoutId(8);
    public override QuestAreaId AreaId => QuestAreaId.UrtecaMountains;
    public override uint RequiredAreaRank => 1;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.PyreSaurianSalamander, 95, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.PyreSaurianSalamander, 95, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.PyreSaurianSalamander, 95, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.PyreSaurianSalamander, 95, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.PyreSaurianSalamander, 95, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.PyreSaurianSalamander, 95, 4200, 5),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.UrtecaHotSpringWater, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.UrtecaHotSpringWater, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.UrtecaHotSpringWater, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.UrtecaHotSpringWater, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.UrtecaHotSpringWater, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.UrtecaHotSpringWater, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Waterweed, 1, 1, DropRate.RARE);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

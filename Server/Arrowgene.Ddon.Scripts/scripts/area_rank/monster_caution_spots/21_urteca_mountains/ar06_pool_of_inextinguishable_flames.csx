/**
 * @brief Monster Gathering Spot in "Urteca Mountains" for "Pool of Inextinguishable Flames"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.UrtecaMountains.AsStageLayoutId(44);
    public override QuestAreaId AreaId => QuestAreaId.UrtecaMountains;
    public override uint RequiredAreaRank => 6;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 13),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 14),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 15),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 16),
            LibDdon.Enemy.Create(EnemyId.BlazeHarpy, 98, 4200, 17),
            LibDdon.Enemy.Create(EnemyId.BlazeHarpy, 98, 4200, 18),
            LibDdon.Enemy.Create(EnemyId.BlazeHarpy, 98, 4200, 19),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 20),
            LibDdon.Enemy.Create(EnemyId.BlazeChimera, 98, 21000, 21)
                .SetIsBoss(true),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.UNCOMMON);
        enemies[5].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[6]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[6].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[7]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[7].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

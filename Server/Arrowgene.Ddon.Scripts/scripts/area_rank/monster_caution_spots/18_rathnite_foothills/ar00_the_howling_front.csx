/**
 * @brief Enemy Spot in "Rathnite Foothills Lakeside" for "The Howling Front"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothillsLakeside0.AsStageLayoutId(1);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 0;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.Strix, 80, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 80, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 80, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.Strix, 80, 4200, 8),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 80, 4200, 9),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BrittleSandstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.PyroclasticRock, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BrittleSandstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.PyroclasticRock, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BrittleSandstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.PyroclasticRock, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BrittleSandstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.PyroclasticRock, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BrittleSandstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.PyroclasticRock, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

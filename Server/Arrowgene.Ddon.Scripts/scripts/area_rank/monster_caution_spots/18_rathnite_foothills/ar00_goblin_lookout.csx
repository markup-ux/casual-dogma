/**
 * @brief Enemy Spot in "Rathnite Foothills" for "Goblin Lookout"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothills.AsStageLayoutId(10);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 0;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 80, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 80, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.GoblinAidShaman, 80, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.GrimGoblinLeader, 80, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 80, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.GoblinAidShaman, 80, 4200, 5),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RathniteLocalHardwood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RathniteLocalHardwood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RathniteLocalHardwood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RathniteLocalHardwood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RathniteLocalHardwood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RathniteLocalHardwood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.VERY_RARE);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

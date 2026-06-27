/**
 * @brief Enemy Spot in "Rathnite Foothills" for "Deserted Village of Denisyr"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothills.AsStageLayoutId(8);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.RathniteFoothillsRescueRequest;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 83, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 83, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 83, 4200, 8),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStone, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStone, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStone, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStone, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GustyWindsStone, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

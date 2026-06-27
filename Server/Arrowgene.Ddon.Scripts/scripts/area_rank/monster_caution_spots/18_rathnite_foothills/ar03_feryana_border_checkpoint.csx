/**
 * @brief Enemy Spot in "Rathnite Foothills Lakeside" for "Feryana Border Checkpoint"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothillsLakeside0.AsStageLayoutId(31);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 5;
    public override QuestId QuestUnlockId => QuestId.RathniteFoothillsPursueAndDefeatEnemies;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.BlackGriffin0, 83, 105000, 0)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 2),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

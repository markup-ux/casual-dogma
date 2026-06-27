/**
 * @brief Enemy Spot in "Rathnite Foothills" for "Former Rest Station"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothills.AsStageLayoutId(9);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.RathniteFoothillsLiberationArmySupport;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 80, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.Behemoth0, 80, 105000, 1)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 80, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 80, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.GoblinShaman, 80, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.GoblinShaman, 80, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 80, 4200, 6),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.VERY_RARE);
        enemies[5].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[6]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AnimalHunterStone, 1, 1, DropRate.VERY_RARE);
        enemies[6].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

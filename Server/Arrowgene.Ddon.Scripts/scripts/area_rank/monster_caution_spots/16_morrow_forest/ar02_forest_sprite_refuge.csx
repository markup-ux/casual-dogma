#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MorrowForest.AsStageLayoutId(7);
    public override QuestAreaId AreaId => QuestAreaId.MorrowForest;
    public override uint RequiredAreaRank => 2;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 70, 0)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 70, 1)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 70, 2)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 70, 3)
                .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RottenTree, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.MorrowLauanWood, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RottenTree, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.MorrowLauanWood, 1, 1, DropRate.UNCOMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RottenTree, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.MorrowLauanWood, 1, 1, DropRate.UNCOMMON);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RottenTree, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.MorrowLauanWood, 1, 1, DropRate.UNCOMMON);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

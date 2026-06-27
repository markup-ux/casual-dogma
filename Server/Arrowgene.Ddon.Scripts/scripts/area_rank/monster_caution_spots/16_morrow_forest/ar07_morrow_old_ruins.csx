#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MorrowForest.AsStageLayoutId(15);
    public override QuestAreaId AreaId => QuestAreaId.MorrowForest;
    public override uint RequiredAreaRank => 7;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 73, 0)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 73, 1)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 73, 2)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 73, 3)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 73, 4)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 73, 5)
                .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.DiscoloredOrgans, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonScraps, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.DiscoloredOrgans, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonScraps, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.DiscoloredOrgans, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonScraps, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.DiscoloredOrgans, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonScraps, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.DiscoloredOrgans, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonScraps, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.DiscoloredOrgans, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonScraps, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.Wijnruit, 1, 1, DropRate.UNCOMMON);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}


return new MonsterSpotInfo();

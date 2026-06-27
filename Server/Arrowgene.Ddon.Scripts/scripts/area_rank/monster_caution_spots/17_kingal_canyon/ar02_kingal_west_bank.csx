#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.KingalCanyon.AsStageLayoutId(7);
    public override QuestAreaId AreaId => QuestAreaId.KingalCanyon;
    public override uint RequiredAreaRank => 2;

    public class NamedParamId
    {
        public const uint Mutated = 1292;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.Griffin0, 70, 0, isBoss: true)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Grimwarg, 70, 1)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Grimwarg, 70, 2)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Grimwarg, 70, 3)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Grimwarg, 70, 4)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Grimwarg, 70, 5)
                .SetNamedEnemyParams(NamedParamId.Mutated)
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.UNCOMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.UNCOMMON);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.UNCOMMON);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.UNCOMMON);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.UNCOMMON);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

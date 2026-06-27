#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.KingalCanyon.AsStageLayoutId(16);
    public override QuestAreaId AreaId => QuestAreaId.KingalCanyon;
    public override uint RequiredAreaRank => 5;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.RockSaurian, 70, 0)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.RockSaurian, 70, 1)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.RockSaurian, 70, 2)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.RockSaurian, 70, 3)
            .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.VERY_COMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.RingingOre, 1, 1, DropRate.VERY_COMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.VERY_COMMON);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.AdamasOre, 1, 1, DropRate.VERY_COMMON);
        enemies[3].SetDropsTable(dropsTable);


        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

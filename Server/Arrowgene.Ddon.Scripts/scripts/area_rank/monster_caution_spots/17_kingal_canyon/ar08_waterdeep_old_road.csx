#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.KingalCanyon.AsStageLayoutId(12);
    public override QuestAreaId AreaId => QuestAreaId.KingalCanyon;
    public override uint RequiredAreaRank => 8;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.Grigori, 73, 0)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Grigori, 73, 1)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.BeardedGrigori, 73, 2)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.BeardedGrigori, 73, 3)
            .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
        .AddDrop(ItemId.ValleySpringWater, 1, 2, DropRate.ALWAYS);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}


return new MonsterSpotInfo();

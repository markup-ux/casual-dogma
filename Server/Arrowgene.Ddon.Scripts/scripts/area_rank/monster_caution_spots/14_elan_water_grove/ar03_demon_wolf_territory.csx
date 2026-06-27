#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.ElanWaterGrove.AsStageLayoutId(10);
    public override QuestAreaId AreaId => QuestAreaId.ElanWaterGrove;
    public override uint RequiredAreaRank => 3;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        AddEnemies(new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.Warg, 70, 1)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Warg, 70, 2)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Warg, 70, 3)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Warg, 70, 4)
            .SetNamedEnemyParams(NamedParamId.Mutated),
        });
    }
}

return new MonsterSpotInfo();

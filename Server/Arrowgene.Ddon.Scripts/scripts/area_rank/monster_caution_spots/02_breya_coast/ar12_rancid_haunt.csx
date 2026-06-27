/**
 * @brief Enemy Spot in "Breya Coast" for "Rancid Haunt"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.LighthouseOldWell.AsStageLayoutId(4);
    public override QuestAreaId AreaId => QuestAreaId.BreyaCoast;
    public override uint RequiredAreaRank => 12;

    public class NamedParamId
    {
        public const uint SparkBlue = 892; // Spark Blue
    }

    public override void Initialize()
    {
        AddEnemies(new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.LargeNewt, 15, 0)
                .SetNamedEnemyParams(NamedParamId.SparkBlue),
            LibDdon.Enemy.CreateAuto(EnemyId.BlueNewt, 14, 1),
            LibDdon.Enemy.CreateAuto(EnemyId.BlueNewt, 14, 2),
            LibDdon.Enemy.CreateAuto(EnemyId.BlueNewt, 14, 3),
            LibDdon.Enemy.CreateAuto(EnemyId.BlueNewt, 13, 4),
            LibDdon.Enemy.CreateAuto(EnemyId.BlueNewt, 13, 5),
        });
    }
}

return new MonsterSpotInfo();

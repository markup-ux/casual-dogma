#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.BloodbaneIsle0.AsStageLayoutId(3);
    public override QuestAreaId AreaId => QuestAreaId.BloodbaneIsle;
    public override uint RequiredAreaRank => 3;

    public override void Initialize()
    {
        AddEnemies(new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblin, 61, 1),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblin, 61, 2),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblin, 61, 3),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 61, 4),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 61, 5),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 61, 6),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblin, 61, 7),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblin, 61, 8),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblin, 61, 9),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 61, 10),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 61, 11),
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedHobgoblinFighter, 61, 12),
        });
    }
}

return new MonsterSpotInfo();

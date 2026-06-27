#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FaranaPlains0.AsStageLayoutId(50);
    public override QuestAreaId AreaId => QuestAreaId.FaranaPlains;
    public override uint RequiredAreaRank => 6;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.GhostMail, 73, 0)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.GhostMail, 73, 1)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Mudman, 73, 2)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Mudman, 73, 0)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Mudman, 73, 1)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.Mudman, 73, 2)
                .SetNamedEnemyParams(NamedParamId.Mutated),                
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.WhiteBoneRock, 1, 3, DropRate.COMMON)
            .AddDrop(ItemId.FaranaMint, 1, 3, DropRate.COMMON);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

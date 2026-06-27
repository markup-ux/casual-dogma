#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.ElanWaterGrove.AsStageLayoutId(4);
    public override QuestAreaId AreaId => QuestAreaId.ElanWaterGrove;
    public override uint RequiredAreaRank => 11;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.Ent, 73, 0, isBoss: true)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.GreenGuardian, 73, 1)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.GreenGuardian, 73, 2)
            .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.GreenGuardian, 73, 3)
            .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
        .AddDrop(ItemId.ElanaWater, 1, 2, DropRate.ALWAYS)
        .AddDrop(ItemId.OldGeranium, 1, 2, DropRate.ALWAYS);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}


return new MonsterSpotInfo();

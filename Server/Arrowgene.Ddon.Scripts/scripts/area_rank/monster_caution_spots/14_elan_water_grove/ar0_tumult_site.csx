#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.ElanWaterGrove.AsStageLayoutId(7);
    public override QuestAreaId AreaId => QuestAreaId.ElanWaterGrove;
    public override uint RequiredAreaRank => 0;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.LittleSpine, 70, 0)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.LittleSpine, 70, 1)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.LittleSpine, 70, 2)
                .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 3, DropRate.COMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 3, DropRate.COMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 3, DropRate.COMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 3, DropRate.COMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 3, DropRate.COMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 3, DropRate.COMMON);
        enemies[2].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

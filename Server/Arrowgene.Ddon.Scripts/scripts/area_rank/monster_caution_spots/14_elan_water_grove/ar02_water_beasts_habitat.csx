#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.ElanWaterGrove.AsStageLayoutId(2);
    public override QuestAreaId AreaId => QuestAreaId.ElanWaterGrove;
    public override uint RequiredAreaRank => 2;

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
            LibDdon.Enemy.CreateAuto(EnemyId.LittleSpine, 70, 3)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.LittleSpine, 70, 4)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.LittleSpine, 70, 5)
                .SetNamedEnemyParams(NamedParamId.Mutated),
            LibDdon.Enemy.CreateAuto(EnemyId.MothCyanSleep, 1, 6),
            LibDdon.Enemy.CreateAuto(EnemyId.MothMagentaPoison, 1, 7),
            LibDdon.Enemy.CreateAuto(EnemyId.MothCyanSleep, 1, 8),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.ElanaWater, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.ElanaWater, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 1, DropRate.UNCOMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 3, DropRate.UNCOMMON)
            .AddDrop(ItemId.ElanaWater, 1, 3, DropRate.UNCOMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 1, DropRate.UNCOMMON);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.ElanaWater, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 1, DropRate.UNCOMMON);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.ElanaWater, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 1, DropRate.UNCOMMON);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.OldGeranium, 1, 3, DropRate.UNCOMMON)
            .AddDrop(ItemId.ElanaWater, 1, 3, DropRate.UNCOMMON)
            .AddDrop(ItemId.PremiumFragrantWood, 1, 1, DropRate.UNCOMMON);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

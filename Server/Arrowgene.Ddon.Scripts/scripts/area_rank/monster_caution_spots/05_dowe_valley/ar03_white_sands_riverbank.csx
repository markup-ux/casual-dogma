/**
 * @brief Enemy Spot in "Dowe Valley" for "White Sands Riverbank"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.Lestania.AsStageLayoutId(491);
    public override QuestAreaId AreaId => QuestAreaId.DoweValley;
    public override uint RequiredAreaRank => 3;

    public class NamedParamId
    {
        public const uint Strange = 218; // <name> Strange
        public const uint SulfurTigerTail = 219; // Sulfur Tiger-Tail
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.GiantSulfurSaurian, 32, 5)
                .SetNamedEnemyParams(NamedParamId.SulfurTigerTail),
            LibDdon.Enemy.CreateAuto(EnemyId.SulfurSaurian, 30, 6)
                .SetNamedEnemyParams(NamedParamId.Strange),
            LibDdon.Enemy.CreateAuto(EnemyId.SulfurSaurian, 30, 7)
                .SetNamedEnemyParams(NamedParamId.Strange),
            LibDdon.Enemy.CreateAuto(EnemyId.SnowHarpy, 28, 3),
            LibDdon.Enemy.CreateAuto(EnemyId.SnowHarpy, 28, 4),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.CrestOfBurning0, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.CrestOfFireWarding0, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MorrowForest.AsStageLayoutId(31);
    public override QuestAreaId AreaId => QuestAreaId.MorrowForest;
    public override uint RequiredAreaRank => 14;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedGriffin, 80, 0, isBoss: true),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 70, 1),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 70, 2),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 70, 3),
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedStymphalides, 70, 4)
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.VictoriousChanceStone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.VictoriousChanceStoneShard, 1, 2, DropRate.VERY_COMMON)
            .AddDrop(ItemId.CorruptedSmallFeathers, 1, 2, DropRate.VERY_COMMON)
            .AddDrop(ItemId.CorruptedGriffinBlood, 1, 2, DropRate.VERY_COMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.CrabBrittlegill, 1, 3, DropRate.COMMON);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.CrabBrittlegill, 1, 3, DropRate.COMMON);
        enemies[2].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

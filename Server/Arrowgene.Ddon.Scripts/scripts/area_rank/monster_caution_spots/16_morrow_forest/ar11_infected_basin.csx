#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MorrowForest.AsStageLayoutId(22);
    public override QuestAreaId AreaId => QuestAreaId.MorrowForest;
    public override uint RequiredAreaRank => 11;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.SeverelyInfectedBehemoth, 78, 0, isBoss: true)
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.CorrodedSpine, 1, 1, DropRate.VERY_COMMON)
            .AddDrop(ItemId.InfectedDemonBoneFragment, 1, 3, DropRate.VERY_COMMON)
            .AddDrop(ItemId.MorrowLauanWood, 1, 1, DropRate.VERY_COMMON);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}


return new MonsterSpotInfo();

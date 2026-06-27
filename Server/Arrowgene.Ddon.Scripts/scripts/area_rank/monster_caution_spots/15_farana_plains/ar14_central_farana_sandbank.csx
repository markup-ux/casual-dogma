#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FaranaPlains0.AsStageLayoutId(80);
    public override QuestAreaId AreaId => QuestAreaId.FaranaPlains;
    public override uint RequiredAreaRank => 14;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.WisenedTarasque0, 80, 0, isBoss: true),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.ArmoredScale, 1, 2, DropRate.ALWAYS)
            .AddDrop(ItemId.GiantScale, 1, 2, DropRate.ALWAYS)
            .AddDrop(ItemId.SparklingWater, 1, 1, DropRate.ALWAYS);
        enemies[0].SetDropsTable(dropsTable);


        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

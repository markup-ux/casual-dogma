#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FaranaPlains0.AsStageLayoutId(9);
    public override QuestAreaId AreaId => QuestAreaId.FaranaPlains;
    public override uint RequiredAreaRank => 3;

    public class NamedParamId
    {
        public const uint Mutated = 1292;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.Nightmare, 70, 0, isBoss: true)
            .SetNamedEnemyParams(NamedParamId.Mutated),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.WhiteBoneRock, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.AshenFlowerRock, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.FaranaMint, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}


return new MonsterSpotInfo();

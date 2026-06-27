#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.BloodbaneIsle0.AsStageLayoutId(11);
    public override QuestAreaId AreaId => QuestAreaId.BloodbaneIsle;
    public override uint RequiredAreaRank => 2;

    public class NamedParamId
    {
        public const uint Variant = 915;
    }

    public override void Initialize()
    {
        AddEnemies(new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.InfectedGorecyclops, 65, 0, isBoss: true)
            .SetNamedEnemyParams(NamedParamId.Variant)
            .AddDrop(ItemId.CrestOfFortitude0, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.CrestOfStubbornPerseverance0, 1, 1, DropRate.UNCOMMON),
        });
    }
}

return new MonsterSpotInfo();

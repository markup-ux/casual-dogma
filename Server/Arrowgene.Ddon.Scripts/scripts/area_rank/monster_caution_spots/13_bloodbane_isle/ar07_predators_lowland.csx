#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.BloodbaneIsle0.AsStageLayoutId(4);
    public override QuestAreaId AreaId => QuestAreaId.BloodbaneIsle;
    public override uint RequiredAreaRank => 7;

    public class NamedParamId
    {
        public const uint ImmortalDrugTestSubject = 381; // Immortal Drug Test Subject
        public const uint SergiusTheApothecary = 386; // Sergius the Apothecary
    }

    public override void Initialize()
    {
        AddEnemies(new List<InstancedEnemy>()
        {
            // TODO: Named enemy should drop crests but no information about what crests should drop
            LibDdon.Enemy.CreateAuto(EnemyId.Nightmare, 63, 0, isBoss: true),
        });
    }
}

return new MonsterSpotInfo();

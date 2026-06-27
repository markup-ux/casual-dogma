/**
 * @brief Enemy Spot in "Elan Water Grove" for "Visiting Square"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.ElanWaterGrove.AsStageLayoutId(15);
    public override QuestAreaId AreaId => QuestAreaId.ElanWaterGrove;
    public override uint RequiredAreaRank => 14;

    public override void Initialize()
    {
        AddEnemies(new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.PhindymianEnt0, 80, 20)
                .AddDrop(ItemId.CrystalEncrustedTwig, 1, 1, DropRate.UNCOMMON)
                .AddDrop(ItemId.CrystallizedSap, 1, 1, DropRate.UNCOMMON)
                .AddDrop(ItemId.OldGeranium, 1, 3, DropRate.COMMON)
        });
    }
}

return new MonsterSpotInfo();

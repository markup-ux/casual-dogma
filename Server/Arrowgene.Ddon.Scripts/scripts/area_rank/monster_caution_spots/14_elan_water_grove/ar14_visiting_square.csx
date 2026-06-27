#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.ElanWaterGrove.AsStageLayoutId(15);
    public override QuestAreaId AreaId => QuestAreaId.ElanWaterGrove;
    public override uint RequiredAreaRank => 14;

    public class NamedParamId
    {
        public const uint Mutated = 1290;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.PhindymianEnt0, 80, 0, isBoss: true)
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
        .AddDrop(ItemId.CrystallizedSap, 1, 2, DropRate.VERY_COMMON)
        .AddDrop(ItemId.CrystalEncrustedTwig, 1, 3, DropRate.VERY_COMMON)
        .AddDrop(ItemId.FallenSoldierStoneShard, 1, 2, DropRate.COMMON)
        .AddDrop(ItemId.FallenSoldierStone, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}


return new MonsterSpotInfo();

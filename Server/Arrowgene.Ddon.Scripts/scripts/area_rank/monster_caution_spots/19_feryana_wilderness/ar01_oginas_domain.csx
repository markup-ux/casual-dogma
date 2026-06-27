// Ogina's Domain — stageid463 GroupId7 MaxPos6 (positions 0-6)
// Monster Caution Spot — Feryana Wilderness, Lv85, AR0
#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FeryanaWilderness.AsStageLayoutId(7);
    public override QuestAreaId AreaId => QuestAreaId.FeryanaWilderness;
    public override uint RequiredAreaRank => 1;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.SlingGrimGoblinOilFlask, 85, 0),
            LibDdon.Enemy.CreateAuto(EnemyId.SlingGrimGoblinOilFlask, 85, 1),
            LibDdon.Enemy.CreateAuto(EnemyId.GreaterGoblinSword, 85, 2),
            LibDdon.Enemy.CreateAuto(EnemyId.GreaterGoblinSword, 85, 3),
            LibDdon.Enemy.CreateAuto(EnemyId.GoblinAidShaman, 85, 4),
            LibDdon.Enemy.CreateAuto(EnemyId.GoblinAidShaman, 85, 5),
            LibDdon.Enemy.CreateAuto(EnemyId.GreaterGoblinSword, 85, 6),
        };

        // Available Items (3): MisleadingTwig, BloodDrinkingVessel, UnrefinedAlloyLump
        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[5].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[6]).Clone()
            .AddDrop(ItemId.MisleadingTwig, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BloodDrinkingVessel, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[6].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

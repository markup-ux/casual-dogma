// Farthest Bank — stageid463 GroupId59 MaxPos3 (positions 0-3)
// Monster Caution Spot — Feryana Wilderness, Lv85, AR0
#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FeryanaWilderness.AsStageLayoutId(59);
    public override QuestAreaId AreaId => QuestAreaId.FeryanaWilderness;
    public override uint RequiredAreaRank => 0;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.CreateAuto(EnemyId.WarReadySaurianLightArmor, 85, 0),
            LibDdon.Enemy.CreateAuto(EnemyId.WarReadySaurianLightArmor, 85, 1),
            LibDdon.Enemy.CreateAuto(EnemyId.WarReadySaurianLightArmor, 85, 2),
            LibDdon.Enemy.CreateAuto(EnemyId.WarReadyGiantSaurianLightArmor, 85, 3),
        };

        // Available Items (3): UnderworldDrop, ScarredLizardscalePelt, UnrefinedAlloyLump
        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.UnderworldDrop, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.UnderworldDrop, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.UnderworldDrop, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.UnderworldDrop, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Feryana Wilderness" for "Wailing Wetland"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FeryanaWilderness.AsStageLayoutId(2);
    public override QuestAreaId AreaId => QuestAreaId.FeryanaWilderness;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.FeryanaWildernessLiberationArmySupport;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadySaurianLightArmor, 88, 4200, 0)
                .SetInfectionType(3),
            LibDdon.Enemy.Create(EnemyId.WarReadySaurianLightArmor, 88, 4200, 1)
                .SetInfectionType(3),
            LibDdon.Enemy.Create(EnemyId.Lindwurm0, 88, 105000, 2)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.WarReadySaurianLightArmor, 88, 4200, 3)
                .SetInfectionType(3),
        };

        // Available Items (5): FeryanaSweetMushroom, ScarredLizardscalePelt, UnrefinedAlloyLump, UndeadDestroyerStoneShard, UndeadDestroyerStone
        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.FeryanaSweetMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UndeadDestroyerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UndeadDestroyerStone, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.FeryanaSweetMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UndeadDestroyerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UndeadDestroyerStone, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.FeryanaSweetMushroom, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.UndeadDestroyerStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.UndeadDestroyerStone, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.FeryanaSweetMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScarredLizardscalePelt, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UndeadDestroyerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UndeadDestroyerStone, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Feryana Wilderness" for "Demon Army Relay Station"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FeryanaWilderness.AsStageLayoutId(1);
    public override QuestAreaId AreaId => QuestAreaId.FeryanaWilderness;
    public override uint RequiredAreaRank => 5;
    public override QuestId QuestUnlockId => QuestId.FeryanaWildernessPursueAndDefeatEnemies;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 88, 105000, 1)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 8),
        };

        // Available Items (4): BattleArmorFragment, UnrefinedAlloyLump, SkeletonSmasherStoneShard, SkeletonSmasherStone
        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStone, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.SkeletonSmasherStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.SkeletonSmasherStone, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStone, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStone, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.SkeletonSmasherStone, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

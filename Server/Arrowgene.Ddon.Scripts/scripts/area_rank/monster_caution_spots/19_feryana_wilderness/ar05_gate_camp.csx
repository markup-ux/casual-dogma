/**
 * @brief Enemy Spot in "Feryana Wilderness" for "Gate Camp"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FeryanaWilderness.AsStageLayoutId(80);
    public override QuestAreaId AreaId => QuestAreaId.FeryanaWilderness;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.FeryanaWildernessRescueRequest;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.WarReadyGoremanticoreLightArmor, 88, 105000, 3)
                .SetIsBoss(true),
        };

        // Available Items (4): BattleArmorFragment, UnrefinedAlloyLump, GiantKillerStoneShard, GiantKillerStone
        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantKillerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantKillerStone, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantKillerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantKillerStone, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantKillerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantKillerStone, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantKillerStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantKillerStone, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

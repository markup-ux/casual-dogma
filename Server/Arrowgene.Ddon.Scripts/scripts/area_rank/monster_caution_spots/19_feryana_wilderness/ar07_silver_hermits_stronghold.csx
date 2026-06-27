/**
 * @brief Enemy Spot in "Feryana Wilderness" for "Silver Hermit's Stronghold"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.FeryanaWilderness.AsStageLayoutId(8);
    public override QuestAreaId AreaId => QuestAreaId.FeryanaWilderness;
    public override uint RequiredAreaRank => 5;
    public override QuestId QuestUnlockId => QuestId.FeryanaWildernessPreventEnemyAttack;

    public class NamedParamId
    {
        public const uint BrutalHermit = 1735;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.Goremanticore, 88, 105000, 0)
                .SetNamedEnemyParams(NamedParamId.BrutalHermit)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.SnowHarpy, 88, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.SnowHarpy, 88, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.SnowHarpy, 88, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.SnowHarpy, 88, 4200, 4),
        };

        // Available Items (4): WarmMud, UnrefinedAlloyLump, DemonExpellerStoneShard, DemonExpellerStone
        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.WarmMud, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.WarmMud, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.WarmMud, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.WarmMud, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.WarmMud, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.UnrefinedAlloyLump, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemonExpellerStone, 1, 1, DropRate.VERY_RARE);
        enemies[4].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

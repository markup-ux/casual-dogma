/**
 * @brief Enemy Spot in "Rathnite Foothills Lakeside" for "Dewdrop Crater"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothillsLakeside0.AsStageLayoutId(6);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 5;
    public override QuestId QuestUnlockId => QuestId.RathniteFoothillsPreventEnemyAttack;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.Chimera0, 83, 105000, 0)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 83, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 83, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 83, 4200, 3),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemihumanCutterStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.DemihumanCutterStone , 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemihumanCutterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemihumanCutterStone , 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemihumanCutterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemihumanCutterStone , 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemihumanCutterStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.DemihumanCutterStone , 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

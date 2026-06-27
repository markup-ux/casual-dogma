/**
 * @brief Enemy Spot in "Urteca Mountains" for "Primitive Settlement"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.UrtecaMountains.AsStageLayoutId(32);
    public override QuestAreaId AreaId => QuestAreaId.UrtecaMountains;
    public override uint RequiredAreaRank => 6;
    public override QuestId QuestUnlockId => QuestId.UrtecaMountainsTrialPrimitiveSettlement;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.AncestorOrigin, 98, 21000, 0)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 4),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.WingedSlayersStone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.WingedSlayersStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.PetrifiedWood, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.WingedSlayersStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.WingedSlayersStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.PetrifiedWood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.WingedSlayersStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.WingedSlayersStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.PetrifiedWood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.WingedSlayersStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.WingedSlayersStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.PetrifiedWood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.WingedSlayersStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.WingedSlayersStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.PetrifiedWood, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

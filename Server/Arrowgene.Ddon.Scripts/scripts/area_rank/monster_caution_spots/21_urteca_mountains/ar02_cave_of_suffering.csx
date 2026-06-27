/**
 * @brief Enemy Spot in "Urteca Mountains" for "Cave of Suffering"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.UrtecaMountains.AsStageLayoutId(17);
    public override QuestAreaId AreaId => QuestAreaId.UrtecaMountains;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.UrtecaMountainsTrialCaveOfSuffering;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyOgreLightArmor, 95, 21000, 0)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 6),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.MagickalConstructsDestructionGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.MagickalConstructsDestructionShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.ScorchedMushroom, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.MagickalConstructsDestructionGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.MagickalConstructsDestructionShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.ScorchedMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.MagickalConstructsDestructionGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.MagickalConstructsDestructionShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.ScorchedMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.MagickalConstructsDestructionGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.MagickalConstructsDestructionShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.ScorchedMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.MagickalConstructsDestructionGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.MagickalConstructsDestructionShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.ScorchedMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.MagickalConstructsDestructionGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.MagickalConstructsDestructionShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.ScorchedMushroom, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Urteca Mountains" for "Silent Resistance Battle Site"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.UrtecaMountains.AsStageLayoutId(12);
    public override QuestAreaId AreaId => QuestAreaId.UrtecaMountains;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.UrtecaMountainsTrialSilentResistanceBattleSite;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.Gargoyle, 95, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.Gargoyle, 95, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.Gargoyle, 95, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 95, 4200, 8),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 95, 4200, 9),
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 95, 21000, 10)
                .SetIsBoss(true),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.SoftTissueErasureGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FragmentsOfTheSoftTissueErasureGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.VolcanicTuff, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.SoftTissueErasureGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FragmentsOfTheSoftTissueErasureGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.VolcanicTuff, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.SoftTissueErasureGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FragmentsOfTheSoftTissueErasureGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.VolcanicTuff, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.SoftTissueErasureGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FragmentsOfTheSoftTissueErasureGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.VolcanicTuff, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.SoftTissueErasureGemstone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FragmentsOfTheSoftTissueErasureGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.VolcanicTuff, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.SoftTissueErasureGemstone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FragmentsOfTheSoftTissueErasureGemstone, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.OldBrickMaterial, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.VolcanicTuff, 1, 1, DropRate.UNCOMMON);
        enemies[5].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

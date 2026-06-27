/**
 * @brief Enemy Spot in "Megadosys Plateau" for "Prisoner Interrogation Gaol"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MegadosysPlateau.AsStageLayoutId(59);
    public override QuestAreaId AreaId => QuestAreaId.MegadosysPlateau;
    public override uint RequiredAreaRank => 5;
    public override QuestId QuestUnlockId => QuestId.MegadosysPlateauPreventEnemyAttack;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.WarReadyOgreLightArmor, 93, 105000, 5)
                .SetIsBoss(true),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.CorruptedSealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.CorruptedSealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.CorruptedSealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.CorruptedSealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.CorruptedSealerStone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.CorruptedSealerStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.UNCOMMON);
        enemies[2].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Megadosys Plateau" for "Demon Army Standby Spot"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MegadosysPlateau.AsStageLayoutId(52);
    public override QuestAreaId AreaId => QuestAreaId.MegadosysPlateau;
    public override uint RequiredAreaRank => 5;
    public override QuestId QuestUnlockId => QuestId.MegadosysPlateauPursueAndDefeatEnemies;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 93, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 8),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.UNCOMMON);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[4]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[4].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[5]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[5].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[6]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[6].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[7]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[7].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[8]).Clone()
            .AddDrop(ItemId.AlchemySealerStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.AlchemySealerStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.RoseMegadosys, 1, 1, DropRate.RARE);
        enemies[8].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Megadosys Plateau" for "Merciful Funeral Hall"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MegadosysPlateau.AsStageLayoutId(40);
    public override QuestAreaId AreaId => QuestAreaId.MegadosysPlateau;
    public override uint RequiredAreaRank => 2;
    public override QuestId QuestUnlockId => QuestId.MegadosysPlateauRescueRequest;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.GrudgeGhost, 92, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.GrudgeGhost, 92, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.GrudgeGhost, 92, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.DeathKnight, 92, 21000, 0)
                .SetIsBoss(true),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.CursedExorciserStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.CursedExorciserStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.CursedExorciserStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.CursedExorciserStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.CursedExorciserStone, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.CursedExorciserStoneShard, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.CursedExorciserStone, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.CursedExorciserStoneShard, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantCaterpillar, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.FlameMushroom, 1, 1, DropRate.UNCOMMON);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Megadosys Plateau" for "Firefall Mountain Eroded Rock"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MegadosysPlateau.AsStageLayoutId(18);
    public override QuestAreaId AreaId => QuestAreaId.MegadosysPlateau;
    public override bool CautionPlayer => false;
    public override uint RequiredAreaRank => 1;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 90, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 90, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Grimwarg, 90, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.Grimwarg, 90, 4200, 3),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.Phlogopite, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BlazingOre, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.RedGarnet, 1, 1, DropRate.RARE);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

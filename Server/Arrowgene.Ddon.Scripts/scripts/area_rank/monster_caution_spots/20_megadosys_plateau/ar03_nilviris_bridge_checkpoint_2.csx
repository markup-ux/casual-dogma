/**
 * @brief Monster Gathering Spot in "Megadosys Plateau" for "Nilviris Bridge Checkpoint"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.MegadosysPlateau.AsStageLayoutId(72);
    public override QuestAreaId AreaId => QuestAreaId.MegadosysPlateau;
    public override bool CautionPlayer => false;
    public override uint RequiredAreaRank => 3;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 93, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 93, 4200, 3),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.LowGradeReinforcedArmor, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.LowGradeReinforcedArmor, 1, 1, DropRate.VERY_RARE)
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.GiantAnimalSkull, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

/**
 * @brief Enemy Spot in "Urteca Mountains" for "Hall of the Magic Horns"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.SacredFlamePath0.AsStageLayoutId(1);
    public override QuestAreaId AreaId => QuestAreaId.UrtecaMountains;
    public override uint RequiredAreaRank => 10;

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.Ushumgal, 100, 105000, 0)
				.SetIsBoss(true),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.RingOfTheWiseDragon, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

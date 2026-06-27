/**
 * @brief Monster Gathering Spot in "Rathnite Foothills Lakeside" for "Beastmaster's Garden"
 */

#load "libs.csx"

public class MonsterSpotInfo : IMonsterSpotInfo
{
    public override StageLayoutId StageLayoutId => Stage.RathniteFoothillsLakeside0.AsStageLayoutId(7);
    public override QuestAreaId AreaId => QuestAreaId.RathniteFoothills;
    public override uint RequiredAreaRank => 6;

    public class NamedParamId
    {
        public const uint Subordinate = 1861;
    }

    public override void Initialize()
    {
        var enemies = new List<InstancedEnemy>()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyGoremanticoreLightArmor, 83, 105000, 1)
                .SetNamedEnemyParams(NamedParamId.Subordinate)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 4),
        };

        var dropsTable = LibDdon.Enemy.GetDropsTable(enemies[0]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.UNCOMMON)
            .AddDrop(ItemId.CampaignBattleArmor, 1, 1, DropRate.RARE);
        enemies[0].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[1]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.CampaignBattleArmor, 1, 1, DropRate.VERY_RARE);
        enemies[1].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[2]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.CampaignBattleArmor, 1, 1, DropRate.VERY_RARE);
        enemies[2].SetDropsTable(dropsTable);

        dropsTable = LibDdon.Enemy.GetDropsTable(enemies[3]).Clone()
            .AddDrop(ItemId.BeastLuringMeat, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.BattleArmorFragment, 1, 1, DropRate.RARE)
            .AddDrop(ItemId.CampaignBattleArmor, 1, 1, DropRate.VERY_RARE);
        enemies[3].SetDropsTable(dropsTable);

        AddEnemies(enemies);
    }
}

return new MonsterSpotInfo();

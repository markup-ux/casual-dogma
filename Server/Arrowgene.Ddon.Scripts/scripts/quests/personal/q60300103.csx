/**
 * @brief Adventure Spot Guide: Rathnite Foothills IV
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300103; // Schedule ID: 1673544064
    public override ushort RecommendedLevel => 85;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.RothgillTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 8));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);
        AddFixedItemReward(ItemId.UnappraisedSnowTrinketGeneral, 3);
    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10; 
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter, Stage.UltaAncientWatercourse, 4, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyGoremanticoreLightArmor, 85, 343000, 0)
                .SetInfectionType(1)
                .SetIsAreaBoss(true)
				.SetIsBoss(true)
                .SetEnemyTargetTypesId(TargetTypesId.AreaBoss),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 85, 11432, 1)
                .SetInfectionType(2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 85, 11432, 2)
                .SetInfectionType(2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 85, 11432, 3)
                .SetInfectionType(1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 85, 11432, 4)
                .SetInfectionType(1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.RothgillTravelersInn, NpcId.Laura, 25816);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Encounter)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Laura, 25817)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundForOrder(1023, 4, 0)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(1023, 0) // Chest room
			]);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Laura, 25818)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(631, NpcId.Laura) 
			]);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

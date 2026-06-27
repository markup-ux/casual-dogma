/**
 * @brief Adventure Spot Guide: Rathnite Foothills II
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300101; // Schedule ID: 1673543808
    public override ushort RecommendedLevel => 80;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.PiremothTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 2));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 1880);
        AddFixedItemReward(ItemId.AcreRoyalFamilyIntaglio, 1);
    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10; 
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter, Stage.ReservoirUndergroundTunnel, 4, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 80, 207360, 0)
                .SetIsAreaBoss(true)
				.SetIsBoss(true)
                .SetEnemyTargetTypesId(TargetTypesId.AreaBoss),
            LibDdon.Enemy.Create(EnemyId.GrimGoblin, 80, 5184, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.GrimGoblin, 80, 5184, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.GrimGoblin, 80, 5184, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.GrimGoblin, 80, 5184, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.GrimGoblin, 80, 5184, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.PiremothTravelersInn, NpcId.Bruno, 25798);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Encounter)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruno, 25799)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundForOrder(1080, 4, 0)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(1080, 0) // Chest room
			]);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruno, 25800)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(630, NpcId.Bruno) 
			]);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

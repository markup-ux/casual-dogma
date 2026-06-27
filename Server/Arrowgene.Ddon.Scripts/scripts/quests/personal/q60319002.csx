/**
 * @brief Feryana Wilderness: Liberation Army Support
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60319002; // Schedule ID: 1677099264
    public override ushort RecommendedLevel => 86;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.FeryanaWilderness;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60319003));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.SkeletonSmasherStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.FeryanaWilderness, 2, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadySaurianLightArmor, 86, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.WarReadySaurianLightArmor, 86, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Lindwurm0, 86, 105000, 2)
				.SetIsBoss(true),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.ShrineoftheEternalBlaze, 10, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Witch, 85, 5384, 0)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.SkeletonMage0, 85, 5384, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonMage0, 85, 5384, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonSorcerer0, 85, 5384, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonSorcerer0, 85, 5384, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsTutorialQuestClear(60319003)
			]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.FeryanaWilderness, 0, 0, NpcId.Versa, 23877)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5684)
			]);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Versa, 23879)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(132, 0, 0, 60319002),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(2)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter0)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(5, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Versa, 24332)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter0, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(6, 3)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Checkpoint, Stage.FeryanaWilderness, 0, 0, NpcId.Versa, 23881)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(7, 3)
			]);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Procure supplies
        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(132, NpcId.Versa, 0)
			]);
		process1.AddNewDeliverItemsBlock(QuestAnnounceType.None, Stage.FeryanaWilderness, 0, 0, NpcId.Versa, ItemId.UnderworldDrop, 5, 24330)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0),
				QuestManager.ResultCommand.UpdateAnnounceDirect(1, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Versa, 23878)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2)
			]);

		//Branch 2 - Survey spots
        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(132, NpcId.Versa, 1)
			]);
        process2.AddDiscoverGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(1),
				QuestManager.ResultCommand.UpdateAnnounceDirect(2, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Versa, 23880)
			]);
        process2.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(3, 3)
			]);
        process2.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.FeryanaWilderness, 0, 0, NpcId.Versa, 24327)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(4, 3)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2)
			]);
    }
}

return new ScriptedQuest();

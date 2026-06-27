/**
 * @brief Rathnite Foothills Liberation Army Support
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60318002; // Schedule ID: 1676935424
    public override ushort RecommendedLevel => 81;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.RathniteFoothills;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 2));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.AnimalHunterStoneShard, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.RathniteFoothills, 9, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 81, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.Behemoth0, 81, 105000, 1)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 81, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 81, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.SlingGrimGoblinOilFlask, 81, 4200, 6),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.CaveofHellsDescent, 19, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Gorecyclops0, 78, 51692, 3)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Saurian, 78, 5168, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.Saurian, 78, 5168, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.Saurian, 78, 5168, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.Saurian, 78, 5168, 7)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.RathniteFoothills, 2);
        process0.AddNewNpcTalkAndOrderBlock(Stage.RathniteFoothills, 0, 0, NpcId.Derek, 60318002)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 5680)
            .AddResultCmdQstTalkChg(NpcId.Derek, 23648);
		process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Derek, 23650)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(130, 0, 0, 60318002),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(2)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter0)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(5, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Derek, 24331)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter0, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(6, 3)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.RathniteFoothills, 0, 0, NpcId.Derek, 60318002)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(7, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Derek, 23652)
			]);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Procure supplies
        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Derek, 0)
			]);
		process1.AddNewDeliverItemsBlock(QuestAnnounceType.None, Stage.RathniteFoothills, 0, 0, NpcId.Derek, ItemId.PlanktonLiquid, 5, 24326)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0),
				QuestManager.ResultCommand.UpdateAnnounceDirect(1, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Derek, 23649)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2)
			]);

		//Branch 2 - Survey spots
        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Derek, 1)
			]);
        process2.AddDiscoverGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(1),
				QuestManager.ResultCommand.UpdateAnnounceDirect(2, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Derek, 23651)
			]);
        process2.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(3, 3)
			]);
        process2.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.RathniteFoothills, 0, 0, NpcId.Derek, 60318002)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(4, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Derek, 24323)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2)
			]);
    }
}

return new ScriptedQuest();

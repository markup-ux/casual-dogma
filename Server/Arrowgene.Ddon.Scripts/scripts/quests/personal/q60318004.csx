/**
 * @brief Rathnite Foothills: Prevent Enemy Attack
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60318004; // Schedule ID: 1676935680
    public override ushort RecommendedLevel => 83;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.RathniteFoothills;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 5));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.DemihumanCutterStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.RathniteFoothills, 19, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 83, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 83, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.WarReadyGrimwargLightArmor, 83, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.Gorechimera0, 83, 105000, 3)
				.SetIsBoss(true),
        });
    }

    protected override void InitializeBlocks() // flag 5682 dudley 6266 shenay 6267 versa 6268 keik 5568 anna
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.RathniteFoothills, 5);
        process0.AddNewNpcTalkAndOrderBlock(Stage.RathniteFoothills, 0, 0, NpcId.Dudley, 60318004)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 5682)
            .AddResultCmdQstTalkChg(NpcId.Dudley, 23658);
		process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Dudley, 23659)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6266)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6267)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6268)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(130, 0, 0, 60318004),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.QuestTalkNpcRadius(131, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.QuestTalkNpcRadius(131, 1, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.QuestTalkNpcRadius(131, 2, 0)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.RathniteFoothills, 0, 0, NpcId.Dudley, 23662);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Shenay
		var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.RathniteFoothills, NpcId.Dudley, 0);
		process1.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6266)
			.AddResultCmdQstTalkChgFsm(NpcId.Shenai, 23660)
			.AddResultCmdQstTalkChgFsm(NpcId.Dudley, 24441)
			.AddCheckCmdIsMyquestLayoutFlagOn(10);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdQstTalkChgFsm(NpcId.Shenai, 23661);


		// Branch 2 - Versa
		var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.RathniteFoothills, NpcId.Dudley, 1);
		process2.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6267)
			.AddResultCmdQstTalkChgFsm(NpcId.Versa, 24437)
			.AddResultCmdQstTalkChgFsm(NpcId.Dudley, 24442)
			.AddCheckCmdIsMyquestLayoutFlagOn(10);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdQstTalkChgFsm(NpcId.Versa, 24439);

		//Branch 3 - Keik
		var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.RathniteFoothills, NpcId.Dudley, 2);
		process3.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6268)
			.AddResultCmdQstTalkChgFsm(NpcId.Keiku, 24438)
			.AddResultCmdQstTalkChgFsm(NpcId.Dudley, 24436)
			.AddCheckCmdIsMyquestLayoutFlagOn(10);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdQstTalkChgFsm(NpcId.Keiku, 24440);

    }
}

return new ScriptedQuest();

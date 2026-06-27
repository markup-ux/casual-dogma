/**
 * @brief Feryana Wilderness Trial: Supply Unit Support
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60319000; // Schedule ID: 1677099008
    public override ushort RecommendedLevel => 86;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.MephiteTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.FeryanaWilderness, 3));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.RoyalCrestMedalFeryanaDistrict, 3);
        AddFixedItemReward(ItemId.MisleadingTwig, 2);
        AddFixedItemReward(ItemId.BlazingOre, 2);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.FeryanaWilderness, 3);
        process0.AddNpcTalkAndOrderBlock(Stage.MephiteTravelersInn, NpcId.Nayajiku, 23868)
            .AddQuestFlag(QuestFlagAction.Set, QuestFlags.MephiteTravelersInn.Nayajiku);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5607),
				QuestManager.ResultCommand.QstLayoutFlagOn(6296),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Nayajiku, 23867)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(132, 0, 0, 60319000),
				QuestManager.CheckCommand.DummyNotProgress()
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(1001, 0, 0, 60319000),
				QuestManager.CheckCommand.DummyNotProgress()
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(5607)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6296)
			]);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3249)
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.None, Stage.MephiteTravelersInn, NpcId.Nayajiku, 23871);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Drew
		var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(5607)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Drew, 23869)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(132, NpcId.Drew, 0)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6296),
				QuestManager.ResultCommand.UpdateAnnounceDirect(2, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Drew, 23870)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(635)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6297),
				QuestManager.ResultCommand.QstLayoutFlagOff(5607),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Drew, 24680),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Nayajiku, 27286)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(635, 0, 0, 60319000)
			]);
		process1.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Drew, 24682)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(1093)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6299),
				QuestManager.ResultCommand.QstLayoutFlagOff(6297),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Drew, 24684)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(1093, 0, 0, 60319000)
			]);
		process1.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3248),
				QuestManager.ResultCommand.QstLayoutFlagOn(6301),
				QuestManager.ResultCommand.QstLayoutFlagOn(6302)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6301),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6302)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3249),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Drew, 24686)
			]);

		// Branch 2 - Anna
		var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6296)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24675)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(1001, NpcId.Anna, 0)
			]);
		process2.AddNewDeliverItemsBlock(QuestAnnounceType.Update, Stage.BerthasBanditGroupHideout, 0, 0, NpcId.Anna, ItemId.AshenGrass, 10, 24679)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(5607),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24678)
			]);
		process2.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24690)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(635)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6298),
				QuestManager.ResultCommand.QstLayoutFlagOff(6296),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24681),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Nayajiku, 27286)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(635, 1, 0, 60319000)
			]);
		process2.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24683)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(1093)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6300),
				QuestManager.ResultCommand.QstLayoutFlagOff(6298),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24685)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(1093, 1, 0, 60319000)
			]);
		process2.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3248),
				QuestManager.ResultCommand.QstLayoutFlagOn(6301),
				QuestManager.ResultCommand.QstLayoutFlagOn(6302)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6301),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6302)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3249),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Anna, 24687)
			]);

		var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6301)
			]);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(1093, 2, 0)
			]);
		process3.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6301)
			]);

		var process4 = AddNewProcess(4);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6302)
			]);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(1093, 3, 0)
			]);
		process4.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6302)
			]);
    }
}

return new ScriptedQuest();

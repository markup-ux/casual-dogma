/**
 * @brief Mysterious Summoning Bell
 * @cheats
 *  /giveitem 7968 15
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300010;
    public override ushort RecommendedLevel => 15;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared(QuestId.ExtendGarden));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 7500);
        AddWalletReward(WalletType.Gold, 7500);
        AddWalletReward(WalletType.RiftPoints, 1500);
        AddFixedItemReward(ItemId.ChattingBell, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear(QuestId.ExtendGarden);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Barton, 26071);
		process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Barton, 26072)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(200, NpcId.Barton, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(200, NpcId.Barton),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.ArisensRoom, 0, 0, NpcId.Sorcerer1, 60300010)
            .AddResultCmdQstTalkChg(NpcId.Barton, 26074)
            .AddResultCmdQstTalkChg(NpcId.Sorcerer1, 24016)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 5677);
		process0.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdQstTalkChg(NpcId.Sorcerer1, 24017)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(211, 0, 0, 60300010),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3278)
			]);
        process0.AddDelayBlock(QuestAnnounceType.Update, 1, 180)
            .AddResultCmdQstTalkChg(NpcId.Sorcerer1, 25028);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.ArisensRoom, 0, 0, NpcId.Sorcerer1, 60300010)
            .AddResultCmdQstTalkChg(NpcId.Sorcerer1, 25029)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3282);
        process0.AddProcessEndBlock(true)
            .AddResultCmdPlayMessage(26120, 544)
            .AddResultCmdTutorialDialog(TutorialId.PawnTacticsTraining);

		var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(211, NpcId.Sorcerer1, 0)
			]);
		process1.AddNewDeliverItemsBlock(QuestAnnounceType.None, Stage.ArisensRoom, 0, 0, NpcId.Sorcerer1, ItemId.Amethyst, 10, 25014)
            .AddResultCmdQstTalkChg(NpcId.Sorcerer1, 25015)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 1);
		process1.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3278);

		var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(211, NpcId.Sorcerer1, 1)
			]);
		process2.AddNewDeliverItemsBlock(QuestAnnounceType.None, Stage.ArisensRoom, 0, 0, NpcId.Sorcerer1, ItemId.PhantomOre, 5, 25016)
            .AddResultCmdQstTalkChg(NpcId.Sorcerer1, 25017)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 1);
		process2.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3278);

		var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(211),
				QuestManager.CheckCommand.MyQstFlagOn(3282)
			]);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25029, 544)
			]);
    }
}

return new ScriptedQuest();

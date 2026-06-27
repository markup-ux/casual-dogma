/**
 * @brief Extend Garden
 * @cheats
 *  /giveitem 7968 15
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.ExtendGarden; // Schedule ID: 1673527296
    public override ushort RecommendedLevel => 20;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared(QuestId.LivingWithThePartnerPawn));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 10000);
        AddWalletReward(WalletType.Gold, 10000);
        AddWalletReward(WalletType.RiftPoints, 2000);
        AddFixedItemReward(ItemId.RiftstoneShard, 5);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear(QuestId.LivingWithThePartnerPawn);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Barton, 24006);
		process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Barton, 24007)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3277)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3397)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(200, NpcId.Barton),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdMyQstFlagOn(0);
        process0.AddIsStageNoBlock(QuestAnnounceType.None, Stage.TheWhiteDragonTemple0)
            .AddResultCmdReleaseAnnounce(ContentsRelease.YourRoomsTerrace);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Deliver materials
        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.TheWhiteDragonTemple0, NpcId.Barton, 0);
        process1.AddDeliverItemsBlock(QuestAnnounceType.Update, Stage.TheWhiteDragonTemple0, NpcId.Barton, ItemId.PineLumber, 15, 25004)
            .AddResultCmdMyQstFlagOn(3277)
			.AddResultCmdQstTalkChg(NpcId.Barton, 25003);
        process1.AddDelayBlock(QuestAnnounceType.None, 1, 180)
            .AddResultCmdQstLayoutFlagOn(6327)
            .AddResultCmdQstLayoutFlagOn(6328)
            .AddResultCmdQstLayoutFlagOn(6329)
			.AddResultCmdQstTalkChg(NpcId.Barton, 25025)
			.AddResultCmdQstTalkChg(NpcId.WhiteKnight0, 25022)
			.AddResultCmdQstTalkChg(NpcId.WhiteKnight1, 25023)
			.AddResultCmdQstTalkChg(NpcId.WhiteKnight2, 25024)
            .AddResultCmdUpdateAnnounceDirect(7, QuestAnnounceType.Update);
        process1.AddTalkToNpcBlock(QuestAnnounceType.None, Stage.TheWhiteDragonTemple0, NpcId.Barton, 25026)
            .AddResultCmdUpdateAnnounceDirect(8, QuestAnnounceType.Update);
        process1.AddProcessEndBlock(false)
            .AddResultCmdMyQstFlagOn(0);

		// Branch 2 - Gather personnel
        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.TheWhiteDragonTemple0, NpcId.Barton, 1);
		process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdUpdateAnnounceDirect(2, QuestAnnounceType.Update)
			.AddResultCmdQstTalkChg(NpcId.Barton, 25006)
            .AddResultCmdMyQstFlagOn(3397)
            .AddCheckCmdMyQstFlagOn(1)
            .AddCheckCmdMyQstFlagOn(2)
            .AddCheckCmdMyQstFlagOn(3);
        process2.AddIsStageNoBlock(QuestAnnounceType.None, Stage.ArisensRoom);
        process2.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.ArisensRoom, 0, 0, NpcId.Man510, 25009)
            .AddResultCmdQstLayoutFlagOn(6324)
            .AddResultCmdQstLayoutFlagOn(6325)
            .AddResultCmdQstLayoutFlagOn(6326)
            .AddResultCmdQstLayoutFlagOff(5675)
            .AddResultCmdQstLayoutFlagOff(6320)
            .AddResultCmdQstLayoutFlagOff(6321)
			.AddResultCmdQstTalkChg(NpcId.Man511, 25011)
			.AddResultCmdQstTalkChg(NpcId.Woman0, 25012);
        process2.AddTalkToNpcBlock(QuestAnnounceType.Update, Stage.TheWhiteDragonTemple0, NpcId.Barton, 25002)
			.AddResultCmdQstTalkChg(NpcId.Man510, 25010);
        process2.AddProcessEndBlock(false)
            .AddResultCmdMyQstFlagOn(0);

		// Branch 2 - First worker
        var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdMyQstFlagOn(3397);
        process3.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.Lestania, 0, 0, NpcId.Man510, 25006)
            .AddResultCmdQstLayoutFlagOn(5675);
		process3.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdMyQstFlagOn(1);
        process3.AddProcessEndBlock(false);

		// Branch 2 - Second worker
        var process4 = AddNewProcess(4);
		process4.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdMyQstFlagOn(3397);
        process4.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.Lestania, 1, 0, NpcId.Man511, 25007)
            .AddResultCmdQstLayoutFlagOn(6320);
		process4.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdMyQstFlagOn(2);
        process4.AddProcessEndBlock(false);

		// Branch 2 - Third worker
        var process5 = AddNewProcess(5);
		process5.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdMyQstFlagOn(3397);
        process5.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.Lestania, 2, 0, NpcId.Woman0, 25008)
            .AddResultCmdQstLayoutFlagOn(6321);
		process5.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdMyQstFlagOn(3);
        process5.AddProcessEndBlock(false);
    }
}

return new ScriptedQuest();

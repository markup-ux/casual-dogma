/**
 * @brief Restored Medal of the Royal Family IV
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300043; // Schedule ID: 1673532800
    public override ushort RecommendedLevel => 100;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.FirefallMountainCampsite;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;
    public override bool Enabled => false;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.SauveurMantleOfTheRoyalFamily0, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdHasAchievement(6, 2867)
			.AddCheckCmdHasAchievement(6, 2868)
			.AddCheckCmdHasAchievement(6, 2870);
        process0.AddNpcTalkAndOrderBlock(Stage.FirefallMountainCampsite, NpcId.Bacias, 30071);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bacias, 30072)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(139, NpcId.Bacias, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(139, NpcId.Bacias, 1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(139, NpcId.Bacias, 2)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(139, NpcId.Bacias),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FirefallMountainCampsite, NpcId.Bacias, 30070);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

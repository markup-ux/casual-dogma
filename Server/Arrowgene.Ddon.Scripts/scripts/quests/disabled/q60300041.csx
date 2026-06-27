/**
 * @brief Restored Medal of the Royal Family II
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300041; // Schedule ID: 1673532544
    public override ushort RecommendedLevel => 90;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.LookoutCastle1;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;
    public override bool Enabled => false;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.SadekMantle0, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdHasAchievement(6, 2857)
			.AddCheckCmdHasAchievement(6, 2858)
			.AddCheckCmdHasAchievement(6, 2859)
			.AddCheckCmdHasAchievement(6, 2860);
        process0.AddNpcTalkAndOrderBlock(Stage.LookoutCastle1, NpcId.Nayajiku, 25881);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Nayajiku, 25882)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(451, NpcId.Nayajiku, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(451, NpcId.Nayajiku, 1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(451, NpcId.Nayajiku, 2)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(451, NpcId.Nayajiku, 3)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(451, NpcId.Nayajiku),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.LookoutCastle1, NpcId.Nayajiku, 25880);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

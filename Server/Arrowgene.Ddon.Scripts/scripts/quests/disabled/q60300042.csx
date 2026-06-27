/**
 * @brief Restored Medal of the Royal Family III
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300042; // Schedule ID: 1673532672
    public override ushort RecommendedLevel => 95;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.FortressCityMegadoResidentialLevel1;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;
    public override bool Enabled => false;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.RoyalMantleOfTheShoulderArmor0, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdHasAchievement(6, 2862)
			.AddCheckCmdHasAchievement(6, 2863)
			.AddCheckCmdHasAchievement(6, 2864)
			.AddCheckCmdHasAchievement(6, 2865);
        process0.AddNpcTalkAndOrderBlock(Stage.FortressCityMegadoResidentialLevel1, NpcId.Doris, 29264);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Doris, 29265)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(461, NpcId.Doris, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(461, NpcId.Doris, 1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(461, NpcId.Doris, 2)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(461, NpcId.Doris, 3)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(461, NpcId.Doris),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FortressCityMegadoResidentialLevel1, NpcId.Doris, 29270);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

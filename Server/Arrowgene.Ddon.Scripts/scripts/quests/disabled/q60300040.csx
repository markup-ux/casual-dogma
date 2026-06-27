/**
 * @brief Restored Medal of the Royal Family I
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300040; // Schedule ID: 1673532416
    public override ushort RecommendedLevel => 85;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.FortThines1;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;
    public override bool Enabled => false;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.MagicaCloak0, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdHasAchievement(6, 2852)
			.AddCheckCmdHasAchievement(6, 2853)
			.AddCheckCmdHasAchievement(6, 2854)
			.AddCheckCmdHasAchievement(6, 2855);
        process0.AddNpcTalkAndOrderBlock(Stage.FortThines1, NpcId.Endale, 25847);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Endale, 25848)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(443, NpcId.Endale, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(443, NpcId.Endale, 1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(443, NpcId.Endale, 2)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(443, NpcId.Endale, 3)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(443, NpcId.Endale),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FortThines1, NpcId.Endale, 25846);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

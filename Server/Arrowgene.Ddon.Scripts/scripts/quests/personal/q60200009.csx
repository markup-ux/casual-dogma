/**
 * @brief The Treasure Lying in the Frontier 4
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200009; // Schedule ID: 1652556928
    public override ushort RecommendedLevel => 65;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200008));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 1970);
        AddWalletReward(WalletType.Gold, 1072);
        AddWalletReward(WalletType.RiftPoints, 137);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear((QuestId)60200008);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18125);
        process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Isaac, 18127)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(803, 0) // Echo Cascade Cavern
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(806, 0) // Fungal Colony Caves
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(841, 0) // Forsaken Well
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(843, 0) // Hidden Observatory
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(851, 0) // Soaring Sky Terrace
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18129);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

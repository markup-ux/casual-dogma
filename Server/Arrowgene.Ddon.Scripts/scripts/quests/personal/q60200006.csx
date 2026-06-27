/**
 * @brief The Treasure Lying in the Frontier 1
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200006; // Schedule ID: 1652556544
    public override ushort RecommendedLevel => 60;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200012));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 1891);
        AddWalletReward(WalletType.Gold, 990);
        AddWalletReward(WalletType.RiftPoints, 125);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18056);
        process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Isaac, 18057)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(802, 0) // Cesspool of Filth
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(804, 0) // Statuary Hollow
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(821, 0) // Frigid Ancient Passage
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18058);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

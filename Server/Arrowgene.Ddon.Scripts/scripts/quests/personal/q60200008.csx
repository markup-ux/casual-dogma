/**
 * @brief The Treasure Lying in the Frontier 3
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200008; // Schedule ID: 1652556800
    public override ushort RecommendedLevel => 65;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200003));
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
            .AddCheckCmdIsTutorialQuestClear((QuestId)60200003);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18062);
        process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Isaac, 18063)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(807, 0) // Bridgeside Tunnel
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(825, 0) // Gardnox Sewers
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(872, 0) // Mysterious Mausoleum
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18064);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

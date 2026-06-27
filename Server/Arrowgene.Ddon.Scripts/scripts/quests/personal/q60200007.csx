/**
 * @brief The Treasure Lying in the Frontier 2
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200007; // Schedule ID: 1652556672
    public override ushort RecommendedLevel => 65;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200006));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 1992);
        AddWalletReward(WalletType.Gold, 1022);
        AddWalletReward(WalletType.RiftPoints, 130);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18059);
        process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Isaac, 18060)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(809, 0) // Scorching Blockade
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(822, 0) // Crypt of Murmurs
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(842, 0) // Towerside Dry Well
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(850, 0) // Misty Illusion Terrace
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(871, 0) // Penitentiary
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18061);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

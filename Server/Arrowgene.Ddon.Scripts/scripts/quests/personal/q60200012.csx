/**
 * @brief The Treasure Sleeping in the Ark
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200012; // Schedule ID: 1652557312
    public override ushort RecommendedLevel => 57;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200002));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 3600);
        AddWalletReward(WalletType.Gold, 990);
        AddWalletReward(WalletType.RiftPoints, 120);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18374);
        process0.AddTalkToNpcBlock(QuestAnnounceType.Accept, Stage.BlackGrapeInn, NpcId.Alfred, 18376)
            .AddResultCmdQstTalkChg(NpcId.Isaac, 18375);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
            .AddResultCmdQstTalkChg(NpcId.Alfred, 18377)
			.AddCheckCmdIsStageNo(Stage.TheArksLowerLevel);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdQstTalkChg(NpcId.Isaac, 18378)
			.AddCheckCmdSceHitIn(Stage.TheArksLowerLevel, 0);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18379);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

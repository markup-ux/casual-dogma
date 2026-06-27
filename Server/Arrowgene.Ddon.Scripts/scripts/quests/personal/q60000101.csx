/**
 * @brief Whirls of Dragon Force
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60000101; // Schedule ID: 1610629248
    public override ushort RecommendedLevel => 55;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 0);
        AddWalletReward(WalletType.Gold, 0);
        AddWalletReward(WalletType.RiftPoints, 0);
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.BeForevermoreWhiteDragon));
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMainQuestClear(QuestId.BeForevermoreWhiteDragon);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Seneka0, 16732);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddCheckCmdTouchActToNpc(Stage.AudienceChamber, NpcId.Joseph)
            .AddResultCmdQstTalkChg(NpcId.Seneka0, 16906);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.AudienceChamber, 103, 6);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Seneka0, 16717)
            .AddResultCmdQstTalkChg(NpcId.Joseph, 16909);
        process0.AddProcessEndBlock(true)
            .AddResultCmdTutorialDialog(TutorialId.ExtremeMissions)
            .AddResultCmdReleaseAnnounce(ContentsRelease.ExtremeMissions, flagInfo: QuestFlags.NpcFunctions.SenekaExm);
    }
}

return new ScriptedQuest();

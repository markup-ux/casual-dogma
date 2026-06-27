/**
 * @brief The Squirming Shadow
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200011; // Schedule ID: 1652557184
    public override ushort RecommendedLevel => 75;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.AudienceChamber;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 27350);
        AddWalletReward(WalletType.Gold, 8987);
        AddWalletReward(WalletType.RiftPoints, 1330);
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.TheNewGeneration));
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200010));
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMainQuestClear(QuestId.TheNewGeneration)
            .AddCheckCmdIsTutorialQuestClear((QuestId)60200010);
        process0.AddNpcTalkAndOrderBlock(Stage.AudienceChamber, NpcId.Joseph, 18657)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 4039);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddCheckCmdTouchActToNpc(Stage.AudienceChamber, NpcId.ArisenCorpsRegimentalSoldier8);
		process0.AddStageJumpBlock(QuestAnnounceType.None, Stage.BranchofContemplation, 8)
			.AddCheckCmdIsStageNo(Stage.BranchofContemplation);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.BranchofContemplation, 0, 6, QuestJumpType.After, Stage.AudienceChamber);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18659)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Clear, 4039)
			.AddResultCmdQstTalkChg(NpcId.Joseph, 18658);
        process0.AddProcessEndBlock(true)
            .AddResultCmdTutorialDialog(TutorialId.OnsetofDarkness);
    }
}

return new ScriptedQuest();

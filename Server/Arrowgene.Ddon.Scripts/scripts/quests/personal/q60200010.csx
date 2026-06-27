/**
 * @brief The Stirring Ground
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200010; // Schedule ID: 1652557056
    public override ushort RecommendedLevel => 70;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.AudienceChamber;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 15350);
        AddWalletReward(WalletType.Gold, 4618);
        AddWalletReward(WalletType.RiftPoints, 600);
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.TheDarknessOfTheHeart));
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200003));
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMainQuestClear(QuestId.TheDarknessOfTheHeart)
            .AddCheckCmdIsTutorialQuestClear((QuestId)60200003);
        process0.AddNpcTalkAndOrderBlock(Stage.AudienceChamber, NpcId.Joseph, 18651)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 4038);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddCheckCmdTouchActToNpc(Stage.AudienceChamber, NpcId.ArisenCorpsRegimentalSoldier8);
		process0.AddStageJumpBlock(QuestAnnounceType.None, Stage.FaranaPlains1, 0)
			.AddCheckCmdIsStageNo(Stage.FaranaPlains1);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.FaranaPlains1, 0, 6, QuestJumpType.After, Stage.AudienceChamber);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18655)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Clear, 4038)
			.AddResultCmdQstTalkChg(NpcId.Joseph, 18654);
        process0.AddProcessEndBlock(true)
            .AddResultCmdTutorialDialog(TutorialId.EarthsFury);
    }
}

return new ScriptedQuest();

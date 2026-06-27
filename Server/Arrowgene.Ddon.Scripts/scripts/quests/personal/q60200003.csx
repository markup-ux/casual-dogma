/**
 * @brief Signs of Chaos
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200003; // Schedule ID: 1652556160
    public override ushort RecommendedLevel => 65;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.AudienceChamber;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 1970);
        AddWalletReward(WalletType.Gold, 1072);
        AddWalletReward(WalletType.RiftPoints, 137);
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.TheEntrustedOne));
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60200002));
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMainQuestClear(QuestId.TheEntrustedOne)
            .AddCheckCmdIsTutorialQuestClear((QuestId)60200002);
        process0.AddNpcTalkAndOrderBlock(Stage.AudienceChamber, NpcId.Joseph, 18047)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 3751);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddCheckCmdTouchActToNpc(Stage.AudienceChamber, NpcId.ArisenCorpsRegimentalSoldier9);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.Lestania, 150, 159, QuestJumpType.Before, Stage.AudienceChamber);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.AudienceChamber, 165, 6, QuestJumpType.Before, Stage.Lestania);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18049)
			.AddResultCmdQstTalkChg(NpcId.Joseph, 18048)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Clear, 3751);
        process0.AddProcessEndBlock(true)
            .AddResultCmdTutorialDialog(TutorialId.PhantasmicGreatDragon);
    }
}

return new ScriptedQuest();

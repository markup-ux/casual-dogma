/**
 * @brief Dangerous Footsteps
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200002; // Schedule ID: 1652556032
    public override ushort RecommendedLevel => 57;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.AudienceChamber;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 3600);
        AddWalletReward(WalletType.Gold, 900);
        AddWalletReward(WalletType.RiftPoints, 120);

        AddFixedItemReward(ItemId.CorruptionCure, 1);
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.TheFateOfLestania));
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60000101));
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMainQuestClear(QuestId.TheFateOfLestania);
        process0.AddNpcTalkAndOrderBlock(Stage.AudienceChamber, NpcId.Joseph, 18044)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 3737);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddCheckCmdTouchActToNpc(Stage.AudienceChamber, NpcId.ArisenCorpsRegimentalSoldier8);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.BloodbaneIsle1, 0, 0, QuestJumpType.Before, Stage.AudienceChamber);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.AudienceChamber, 160, 6, QuestJumpType.Before, Stage.BloodbaneIsle1);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.TheWhiteDragonTemple0, NpcId.Isaac, 18046)
			.AddResultCmdQstTalkChg(NpcId.Joseph, 18045)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Clear, 3737);
        process0.AddProcessEndBlock(true)
            .AddResultCmdReleaseAnnounce(ContentsRelease.ExtremeMissions, flagInfo: QuestFlags.NpcFunctions.IsaacExm)
            .AddResultCmdTutorialDialog(TutorialId.AgentofCorruption);
    }
}

return new ScriptedQuest();

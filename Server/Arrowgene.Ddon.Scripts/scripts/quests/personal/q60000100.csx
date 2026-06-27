/**
 * @brief Further Signs of Misfortune
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60000100; // Schedule ID: 1610629120
    public override ushort RecommendedLevel => 60;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.AudienceChamber;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 0);
        AddWalletReward(WalletType.Gold, 0);
        AddWalletReward(WalletType.RiftPoints, 0);
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.ExtremeMissionCleared((QuestId)50103020));
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns());
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
		process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMainQuestClear(QuestId.BeForevermoreWhiteDragon);
        process0.AddNpcTouchAndOrderBlock(Stage.AudienceChamber, NpcId.TheWhiteDragon, 0);
        process0.AddPlayEventBlock(QuestAnnounceType.None, Stage.AudienceChamber, 105, 2);
        process0.AddTalkToNpcBlock(QuestAnnounceType.Accept, Stage.AudienceChamber, NpcId.Joseph, 16094)
            .AddResultCmdQstTalkChg(NpcId.TheWhiteDragon, 13026);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

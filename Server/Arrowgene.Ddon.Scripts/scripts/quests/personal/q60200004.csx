/**
 * @brief Supreme Radiance
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60200004; // Schedule ID: 1652556288
    public override ushort RecommendedLevel => 70;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.TheWhiteDragonTemple0;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared(QuestId.RoadToMastery));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 15350);
        AddWalletReward(WalletType.Gold, 4618);
        AddWalletReward(WalletType.RiftPoints, 600);
        AddFixedItemReward(ItemId.SupremeMeritMedal, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear(QuestId.RoadToMastery);
        process0.AddNpcTalkAndOrderBlock(Stage.TheWhiteDragonTemple0, NpcId.Renton0, 24394);
        process0.AddTalkToNpcBlock(QuestAnnounceType.Accept, Stage.CraftRoom, NpcId.Sonia, 24396)
			.AddResultCmdQstTalkChg(NpcId.Renton0, 24395);
        process0.AddProcessEndBlock(true)
            .AddResultCmdPlayMessage(24397, 1026);
    }
}

return new ScriptedQuest();

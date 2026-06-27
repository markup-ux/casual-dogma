/**
 * @brief The High Scepter's Heir
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Substory;
    public override QuestId QuestId => QuestId.TasteOfBitterMemories;
    public override ushort RecommendedLevel => 82;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.FortThinesGreatDiningHall;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class QstLayoutFlag
    {

    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.SoloWithPawns(1));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddSelectItemReward(new()
        {
            (ItemId.RoyalCrestMedalRathniteDistrict, 3),
            (ItemId.CarriesSpecialSandwich, 1),
        });
    }

    protected override void InitializeEnemyGroups()
    {
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.FortThinesGreatDiningHall,NpcId.Carrie2, 25635)
             .AddQuestFlag(QuestFlagAction.Set, QuestFlags.FortThinesGreatDiningHall.Carrie);
        process0.AddDeliverItemsBlock(QuestAnnounceType.Accept, Stage.FortThinesGreatDiningHall, NpcId.Carrie2, ItemId.BlazeGrass, 2, 25637);
        process0.AddNoProgressBlock();
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

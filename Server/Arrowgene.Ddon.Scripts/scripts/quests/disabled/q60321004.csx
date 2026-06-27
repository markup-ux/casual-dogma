/**
 * @brief Urteca Mountains Trial: Construction Material Depot
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.UrtecaMountainsTrialConstructionMaterialDepot; // Schedule ID: 1677885952
    public override ushort RecommendedLevel => 100;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.FirefallMountainCampsite;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;
    public override bool Enabled => false;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.UrtecaMountains, 6));
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.TheDreadfulPassage));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.WarReadyBreakStone, 1);
    }

    private class EnemyGroupId
    {
        public const uint Set7870 = 7870;
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Set7870, Stage.UrtecaMountains, 35, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.BlazeGrigori, 98, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.BurnedEnt, 98, 105000, 5)
                .SetIsBoss(true),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.UrtecaMountains, 6);
        process0.AddNpcTalkAndOrderBlock(Stage.FirefallMountainCampsite, NpcId.Cory, 30151);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Set7870)
            .AddResultCmdQstTalkChg(NpcId.Cory, 30152);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set7870, resetGroup: false);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FirefallMountainCampsite, NpcId.Cory, 30153);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

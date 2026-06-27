/**
 * @brief Urteca Mountains Trial: Primitive Settlement
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.UrtecaMountainsTrialPrimitiveSettlement; // Schedule ID: 1677885824
    public override ushort RecommendedLevel => 98;
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

        AddFixedItemReward(ItemId.WingedSlayersStone, 1);
    }

    private class EnemyGroupId
    {
        public const uint Set7869 = 7869;
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Set7869, Stage.UrtecaMountains, 32, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.AncestorOrigin, 98, 105000, 0)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 98, 4200, 4),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.UrtecaMountains, 6);
        process0.AddNpcTalkAndOrderBlock(Stage.FirefallMountainCampsite, NpcId.Sema, 30148);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Set7869)
			.AddResultCmdQstTalkChg(NpcId.Sema, 30149);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set7869, resetGroup: false);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FirefallMountainCampsite, NpcId.Sema, 30150);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

/**
 * @brief Urteca Mountains Trial: Ancestral Territory
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.UrtecaMountainsTrialAncestralTerritory; // Schedule ID: 1677885440
    public override ushort RecommendedLevel => 98;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.NorthernBanditHideout;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;
    public override bool Enabled => false;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.UrtecaMountains, 3));
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.TheRoyalFamilyMausoleum));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.RoyalCrestMedalUrtecaDistrict, 3);
        AddFixedItemReward(ItemId.Belladonna, 1);
        AddFixedItemReward(ItemId.PetrifiedWood, 1);
    }

    private class EnemyGroupId
    {
        public const uint Set8012 = 8012;
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Set8012, Stage.UrtecaMountains, 27, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.CaptainAncestorOrc, 98, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 98, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 98, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 98, 4200, 8),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 98, 4200, 9),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 98, 4200, 10),
            LibDdon.Enemy.Create(EnemyId.AncestorOrc, 98, 4200, 11),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.UrtecaMountains, 3);
        process0.AddNpcTalkAndOrderBlock(Stage.NorthernBanditHideout, NpcId.Bacias, 30154);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Set8012)
			.AddResultCmdQstTalkChg(NpcId.Bacias, 30155);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set8012, resetGroup: false);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.NorthernBanditHideout, NpcId.Bacias, 30156);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

/**
 * @brief Urteca Mountains Trial: Cave of Suffering
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.UrtecaMountainsTrialCaveOfSuffering; // Schedule ID: 1677885568
    public override ushort RecommendedLevel => 95;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.NorthernBanditHideout;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;
    public override bool Enabled => false;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.UrtecaMountains, 2));
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.NedosTrail));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.MagickalConstructsDestructionGemstone, 1);
    }

    private class EnemyGroupId
    {
        public const uint Set7844 = 7844;
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Set7844, Stage.UrtecaMountains, 17, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyOgreLightArmor, 95, 105000, 0)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.Hellhound, 95, 4200, 6),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.UrtecaMountains, 2);
        process0.AddNpcTalkAndOrderBlock(Stage.NorthernBanditHideout, NpcId.Baris, 29896);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Set7844)
            .AddResultCmdQstTalkChg(NpcId.Baris, 29897);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set7844, resetGroup: false);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.NorthernBanditHideout, NpcId.Baris, 29899);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

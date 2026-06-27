/**
 * @brief Urteca Mountains Trial: Silent Resistance Battle Site
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.UrtecaMountainsTrialSilentResistanceBattleSite; // Schedule ID: 1677885696
    public override ushort RecommendedLevel => 95;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override bool? EnableCancel => true;
    public override StageInfo StageInfo => Stage.UrtecaMountains;
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

        AddFixedItemReward(ItemId.SoftTissueErasureGemstone, 1);
    }

    private class EnemyGroupId
    {
        public const uint Set7863 = 7863;
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Set7863, Stage.UrtecaMountains, 12, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Gargoyle, 95, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.Gargoyle, 95, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.Gargoyle, 95, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 95, 4200, 8),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 95, 4200, 9),
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 95, 105000, 10)
                .SetIsBoss(true),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.UrtecaMountains, 2);
        process0.AddNpcTalkAndOrderBlock(Stage.UrtecaMountains, NpcId.Zef, 30145);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Set7863)
            .AddResultCmdQstTalkChg(NpcId.Zef, 30146);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set7863, resetGroup: false);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.UrtecaMountains, NpcId.Zef, 30147);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

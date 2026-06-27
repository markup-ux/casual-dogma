/**
 * @brief Adventure Spot Guide: Feryana Wilderness III
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300106;
    public override ushort RecommendedLevel => 88;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.MephiteTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.FeryanaWilderness, 8));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);
        AddFixedItemReward(ItemId.RedHotSmeltedOre, 1);
    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10; 
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter, Stage.RestlessWaterChannel, 19, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 0)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonCyclops, 90, 110000, 4)
                .SetIsAreaBoss(true)
				.SetIsBoss(true)
                .SetEnemyTargetTypesId(TargetTypesId.AreaBoss),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 7)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SkeletonWarg, 90, 4827, 8)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.MephiteTravelersInn, NpcId.Shekel, 25830);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Encounter)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Shekel, 25831)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundForOrder(1024, 19, -1)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(1024, 0) // Chest room
			]);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Shekel, 25832)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(635, NpcId.Shekel) 
			]);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

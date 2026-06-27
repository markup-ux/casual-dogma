/**
 * @brief Adventure Spot Guide: Feryana Wilderness II
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300105;
    public override ushort RecommendedLevel => 88;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.MephiteTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.FeryanaWilderness, 5));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);
        AddFixedItemReward(ItemId.ScorpionTailsHusk, 1);
    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10; 
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter, Stage.FalseRemains, 22, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyOgreLightArmor, 88, 218181, 0)
                .SetIsAreaBoss(true)
				.SetIsBoss(true)
                .SetEnemyTargetTypesId(TargetTypesId.AreaBoss),
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 88, 5454, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 5454, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 5454, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 5454, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 5454, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNpcTalkAndOrderBlock(Stage.MephiteTravelersInn, NpcId.Shekel, 25827);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Encounter)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Shekel, 25828)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundForOrder(1061, 22, -1)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(1061, 0) // Chest room
			]);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Shekel, 25829)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(635, NpcId.Shekel) 
			]);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

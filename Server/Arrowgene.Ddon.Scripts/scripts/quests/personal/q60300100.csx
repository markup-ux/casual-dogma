/**
 * @brief Adventure Spot Guide: Rathnite Foothills I
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60300100; // Schedule ID: 1673543680
    public override ushort RecommendedLevel => 78;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.PiremothTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 36850);
        AddWalletReward(WalletType.Gold, 10145);
        AddWalletReward(WalletType.RiftPoints, 1660);
        AddFixedItemReward(ItemId.DelicateIntaglio, 3);
    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10; 
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter, Stage.CaveofHellsDescentInnerDepths, 10, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Gorechimera0, 78, 94705, 0)
                .SetIsAreaBoss(true)
				.SetIsBoss(true)
                .SetEnemyTargetTypesId(TargetTypesId.AreaBoss),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.CheckAreaRank(18, 1)
			]);
        process0.AddNpcTalkAndOrderBlock(Stage.PiremothTravelersInn, NpcId.Bruno, 25750);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Accept, EnemyGroupId.Encounter)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruno, 25751)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundForOrder(1092, 10, -1)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(1092, 0) // Chest room
			]);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruno, 25752)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(630, NpcId.Bruno) 
			]);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

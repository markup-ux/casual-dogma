/**
 * @brief Feryana Wilderness: Pursue and Defeat Enemies
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60319001; // Schedule ID: 1677099136
    public override ushort RecommendedLevel => 88;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.FeryanaWilderness;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.FeryanaWilderness, 5));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.SkeletonSmasherStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.FeryanaWilderness, 1, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 88, 105000, 1)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 88, 4200, 8),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.CheckAreaRank(19, 5)
			]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.FeryanaWilderness, 0, 0, NpcId.Guy, 23876)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5683)
			]);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Guy, 23875)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(132, NpcId.Guy, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(132, 0, 0, 60319001),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Guy, 23874),
				QuestManager.ResultCommand.LayoutFlagRandomOn(6208, 6209, -1, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(132, 1, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(132, 2, 0, 0)
			]);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6208),
				QuestManager.ResultCommand.QstLayoutFlagOff(6209),
				QuestManager.ResultCommand.LayoutFlagRandomOn(6210, 6211, -1, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(132, 3, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(132, 4, 0, 0)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6210),
				QuestManager.ResultCommand.QstLayoutFlagOff(6211)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FeryanaWilderness, 0, 0, NpcId.Guy, 23873);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

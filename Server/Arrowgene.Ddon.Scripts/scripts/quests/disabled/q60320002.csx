/**
 * @brief Megadosys Plateau: Liberation Army Support
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.MegadosysPlateauLiberationArmySupport; // Schedule ID: 1677721856
    public override ushort RecommendedLevel => 90;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.EliGuardTower;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;
    public override bool Enabled => false;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.MegadosysPlateau, 2));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.SpiritPurifierStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.MegadosysPlateau, 35, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 2818, 0),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 2818, 1),
            LibDdon.Enemy.Create(EnemyId.Witch, 90, 5636, 2)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 2818, 3),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 2818, 4),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.QuietConcealedCave, 7, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 4807, 1),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 4807, 2),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 4807, 3),
            LibDdon.Enemy.Create(EnemyId.GiantGeoSaurian, 90, 4807, 5),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.CheckAreaRank(20, 2)
            ]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.EliGuardTower, 0, 0, NpcId.Bruce, 28166)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(7455)
			]);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruce, 28168)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(636, 0, 0, 60320002),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(2)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter0)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(5, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruce, 28175)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter0, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(6, 3)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Checkpoint, Stage.EliGuardTower, 0, 0, NpcId.Bruce, 28170)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(7, 3)
			]);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Procure supplies
        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(636, NpcId.Bruce, 0)
			]);
		process1.AddNewDeliverItemsBlock(QuestAnnounceType.None, Stage.EliGuardTower, 0, 0, NpcId.Bruce, ItemId.CaveShrimp, 5, 28173)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0),
				QuestManager.ResultCommand.UpdateAnnounceDirect(1, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruce, 28167)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2)
			]);

		//Branch 2 - Survey spots
        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(636, NpcId.Bruce, 1)
			]);
        process2.AddIsStageNoBlock(QuestAnnounceType.None, Stage.QuietConcealedCave)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(1),
				QuestManager.ResultCommand.UpdateAnnounceDirect(2, 3),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Bruce, 28169)
			]);
        process2.AddDiscoverGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1)
			.AddResultCmdTutorialDialog(TutorialId.WallsThatBreakunderCertainConditions);
        process2.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(3, 3)
			]);
        process2.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.EliGuardTower, 0, 0, NpcId.Bruce, 28174)
			.AddResultCommands([
				QuestManager.ResultCommand.UpdateAnnounceDirect(4, 3)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2)
			]);
    }
}

return new ScriptedQuest();

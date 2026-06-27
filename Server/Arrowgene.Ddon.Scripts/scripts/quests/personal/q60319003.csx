/**
 * @brief Feryana Wilderness: Rescue Request
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60319003; // Schedule ID: 1677099392
    public override ushort RecommendedLevel => 86;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.FeryanaWilderness;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.FeryanaWilderness, 2));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.GiantKillerStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.FeryanaWilderness, 80, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 86, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 86, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.WarReadyGoremanticoreLightArmor, 86, 105000, 3)
				.SetIsBoss(true),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.FeryanaWilderness, 9, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 86, 4200, 0)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 86, 4200, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 86, 4200, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 86, 4200, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.CheckAreaRank(19, 2)
			]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.FeryanaWilderness, 1, 0, NpcId.Dana, 24180)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5685)
			]);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Dana, 24181),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Artin, 23882)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(635, NpcId.Artin),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.StartTimer(1, 20)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FeryanaWilderness, 1, 0, NpcId.Dana, 24048)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2963),
				QuestManager.ResultCommand.QstLayoutFlagOff(10)
			]);
        process0.AddProcessEndBlock(true);

        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(635, NpcId.Artin, 0)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Artin, 23885)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEndTimer(2),
				QuestManager.CheckCommand.MyQstFlagOff(2963)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5610),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100219)
			]);

        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(635, NpcId.Artin, 1)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Artin, 23886)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEndTimer(2),
				QuestManager.CheckCommand.MyQstFlagOff(2963)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5700),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100219)
			]);

        var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(10)
			]);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEndTimer(1),
				QuestManager.CheckCommand.MyQstFlagOff(2963)
			]);
        process3.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1)
			.AddResultCommands([
				QuestManager.ResultCommand.StartTimer(2, 30),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100220)
			]);
    }
}

return new ScriptedQuest();

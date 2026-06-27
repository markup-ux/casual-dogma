/**
 * @brief Rathnite Foothills Rescue Request
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60318001; // Schedule ID: 1676935296
    public override ushort RecommendedLevel => 81;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.RathniteFoothills;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 2));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.GustyWindsStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.RathniteFoothills, 8, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 81, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 81, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 81, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 81, 4200, 8),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.RathniteFoothills, 65, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 81, 4200, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 81, 4200, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 81, 4200, 7)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.RathniteFoothills, 2);
        process0.AddNewNpcTalkAndOrderBlock(Stage.RathniteFoothills, 1, 0, NpcId.Guy, 60318001)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 5679)
            .AddResultCmdQstTalkChg(NpcId.Guy, 24049);
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Guy, 24050),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Fran, 23643)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(130, NpcId.Fran),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Fran, 23647)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.StartTimer(1, 20)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.RathniteFoothills, 1, 0, NpcId.Guy, 60318001)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2962),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Guy, 24026)
			]);
        process0.AddProcessEndBlock(true);

        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Fran, 0)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEndTimer(2),
				QuestManager.CheckCommand.MyQstFlagOff(2962)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5565),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100216)
			]);

        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Fran, 1)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEndTimer(2),
				QuestManager.CheckCommand.MyQstFlagOff(2962)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5699),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100216)
			]);

        var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(10)
			]);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEndTimer(1),
				QuestManager.CheckCommand.MyQstFlagOff(2962)
			]);
        process3.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1)
			.AddResultCommands([
				QuestManager.ResultCommand.StartTimer(2, 30),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100217)
			]);
    }
}

return new ScriptedQuest();

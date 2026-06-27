/**
 * @brief Rathnite Foothills Trial: Rathnite Foothills Pursue and Defeat Enemies
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60318003; // Schedule ID: 1676935552
    public override ushort RecommendedLevel => 83;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.RathniteFoothills;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 5));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.DemihumanCutterStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.RathniteFoothillsLakeside0, 31, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.BlackGriffin0, 83, 105000, 0),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 2),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.RathniteFoothills, 5);
        process0.AddNewNpcTalkAndOrderBlock(Stage.RathniteFoothills, 0, 0, NpcId.Dana, 60318003)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 5681)
            .AddResultCmdQstTalkChg(NpcId.Dana, 23653);
		process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCmdQstTalkChg(NpcId.Dana, 23654)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Dana, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(130, 0, 0, 60318003),
				QuestManager.CheckCommand.DummyNotProgress()
			]);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Dana, 23655),
				QuestManager.ResultCommand.SetRandom(1, 1, 2, 1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6205)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6206)
			]);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.SetRandom(2, 1, 2, 1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.RathniteFoothills, 0, 0, NpcId.Dana, 60318003)
            .AddResultCmdQstTalkChg(NpcId.Dana, 23656);
        process0.AddProcessEndBlock(true);

		var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(1, 1)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6203)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 0, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 1, 0, 0)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6203),
				QuestManager.ResultCommand.QstLayoutFlagOn(6205)
			]);

		var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(1, 2)
			]);
        process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6204)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 0, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 1, 0, 0)
			]);
        process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6204),
				QuestManager.ResultCommand.QstLayoutFlagOn(6206)
			]);

		var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(2, 1)
			]);
        process3.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6205)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 2, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 3, 0, 0)
			]);
        process3.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6205),
				QuestManager.ResultCommand.MyQstFlagOn(0)
			]);

		var process4 = AddNewProcess(4);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(2, 2)
			]);
        process4.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6206)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 2, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.OmSetTouchRadius(131, 3, 0, 0)
			]);
        process4.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6206),
				QuestManager.ResultCommand.MyQstFlagOn(0)
			]);
    }
}

return new ScriptedQuest();

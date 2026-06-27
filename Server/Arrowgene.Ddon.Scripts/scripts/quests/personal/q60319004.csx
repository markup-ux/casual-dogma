/**
 * @brief Feryana Wilderness: Prevent Enemy Attack
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60319004; // Schedule ID: 1677099520
    public override ushort RecommendedLevel => 88;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override StageInfo StageInfo => Stage.FeryanaWilderness;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
    }

    private class NamedParamId
    {
        public const uint BrutalHermit = 1735;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60319001));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.DemonExpellerStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.FeryanaWilderness, 15, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 0)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
            LibDdon.Enemy.Create(EnemyId.Goremanticore, 88, 105000, 1)
                .SetNamedEnemyParams(NamedParamId.BrutalHermit)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 2)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 10)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.FeryanaWilderness, 8, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Goremanticore, 88, 105000, 0)
                .SetNamedEnemyParams(NamedParamId.BrutalHermit)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 1)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 2)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 3)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
            LibDdon.Enemy.Create(EnemyId.Mudman, 88, 4200, 4)
				.SetRepopCount(1)
				.SetRepopWaitSecond(10),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsTutorialQuestClear(60319001)
			]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.FeryanaWilderness, 0, 0, NpcId.Eileen, 23887)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5686)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Accept, Stage.FeryanaWilderness, 0, 0, NpcId.Eileen, 23888);
		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6271),
				QuestManager.ResultCommand.QstLayoutFlagOn(6272),
				QuestManager.ResultCommand.QstLayoutFlagOn(6273)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(10)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(11)
			]);
		process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(10),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(11)
			]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FeryanaWilderness, 0, 0, NpcId.Eileen, 23891)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6271),
				QuestManager.ResultCommand.QstLayoutFlagOff(6272),
				QuestManager.ResultCommand.QstLayoutFlagOff(6273)
			]);
        process0.AddProcessEndBlock(true);

		// Branch 1a - Shenay
		var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6271)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Shenai, 23889)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.QuestTalkNpcRadius(132, 1, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6271)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6271)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6272),
				QuestManager.ResultCommand.QstLayoutFlagOff(6273),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Shenai, 23890),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Keiku, 24505),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Elton, 24589)
			]);

		// Branch 1b - Keik
		var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6272)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Keiku, 24503)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.QuestTalkNpcRadius(132, 2, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6272)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6272)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6271),
				QuestManager.ResultCommand.QstLayoutFlagOff(6273)
			]);

		var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6271),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6273)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6272),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6273)
			]);
        process3.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Shenai, 23890),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Keiku, 24505),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Elton, 24589)
			]);
        process3.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(10)
			]);

		// Branch 2 - Elton
		var process4 = AddNewProcess(4);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6273)
			]);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Elton, 24504)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.QuestTalkNpcRadius(132, 3, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6273)
			]);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6273)
			]);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(6271),
				QuestManager.ResultCommand.QstLayoutFlagOff(6272)
			]);

		var process5 = AddNewProcess(5);
		process5.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6271),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOff(6272),
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6273)
			]);
        process5.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter1)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Elton, 24506),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Shenai, 24587),
				QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Keiku, 24588)
			]);
        process5.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter1, resetGroup: false);
		process5.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOff(11)
			]);
    }
}

return new ScriptedQuest();

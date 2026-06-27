/**
 * @brief Rathnite Foothills Trial: Captives in the Hills
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60318000;
    public override ushort RecommendedLevel => 83;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.PiremothTravelersInn;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
        public const uint Encounter1 = 11;
        public const uint Keik1 = 20;
        public const uint Keik2 = 21;
        public const uint Shenay1 = 30;
        public const uint Shenay2 = 31;
        public const uint Empty1 = 40;
    }

    private class NamedParamId
    {
        public const uint ExecutionOfficer = 1836;
        public const uint ExecutionHead = 1837;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.RathniteFoothills, 3));
        AddQuestOrderCondition(QuestOrderCondition.MainQuestCompleted(QuestId.InSearchOfHope));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.RoyalCrestMedalRathniteDistrict, 3);
        AddFixedItemReward(ItemId.BlazeGrass, 1);
        AddFixedItemReward(ItemId.NaturalCharcoal, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Keik1, Stage.RathniteFoothills, 54, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 83, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 83, 4200, 8),
        });

        AddEnemies(EnemyGroupId.Keik2, Stage.RathniteFoothills, 36, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 83, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.GrimGoblinFighter, 83, 4200, 3),
        });

        AddEnemies(EnemyGroupId.Shenay1, Stage.RathniteFoothills, 33, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 1),
        });

        AddEnemies(EnemyGroupId.Shenay2, Stage.RathniteFoothills, 4, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 4),
        });

        AddEnemies(EnemyGroupId.Empty1, Stage.RathniteFoothills, 66, QuestEnemyPlacementType.Manual, new()
        {
        });

        AddEnemies(EnemyGroupId.Encounter0, Stage.RathniteFoothills, 39, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 3)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 83, 4200, 12)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 13)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.RathniteFoothills, 66, QuestEnemyPlacementType.Manual, new()
        {

            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 83, 4200, 0)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 83, 4200, 1)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 83, 21000, 2)
                .SetNamedEnemyParams(NamedParamId.ExecutionHead),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 3)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 4)
                .SetNamedEnemyParams(NamedParamId.ExecutionOfficer),


        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.RathniteFoothills, 3);

        // 1. Speak with Endale (quest giver)
        process0.AddNpcTalkAndOrderBlock(Stage.PiremothTravelersInn, NpcId.Endale, 23630)
            .AddQuestFlag(QuestFlagAction.Set, QuestFlags.PiremothTravelersInn.Endale);

        // 2. Speak with Youin
		process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Endale, 23631),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Youin, 23632)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(130, NpcId.Youin),
				QuestManager.CheckCommand.DummyNotProgress()
			]);

		process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Youin, 23633),
				QuestManager.ResultCommand.SetRandom(1, 1, 2, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(2)
			]);

		process0.AddRawBlock(QuestAnnounceType.Update)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6140) // Guy
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6141) // Eileen
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6142) // Ethel
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6143) // Cage 1
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6144) // Cage 2
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6145) // Cage 3
            .AddResultCmdMyQstFlagOn(3)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(4),
				QuestManager.CheckCommand.MyQstFlagOn(3127),
				QuestManager.CheckCommand.MyQstFlagOn(3128),
				QuestManager.CheckCommand.MyQstFlagOn(3129)
			]);

        process0.AddNewTalkToNpcBlock(QuestAnnounceType.None, Stage.RathniteFoothills, 4, 0, NpcId.Guy, 60318000)		
			.AddResultCommands([
				QuestManager.ResultCommand.Unknown(104, 595, 24247)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter1);
        process0.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1, resetGroup: false);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.RathniteFoothills, 4, 0, NpcId.Guy, 60318000)
			.AddResultCommands([
				QuestManager.ResultCommand.Unknown(104, 595, 24290)
			]);

        // 6. Return to Endale
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.PiremothTravelersInn, NpcId.Endale, 23634);
        process0.AddProcessEndBlock(true);

		// 3a. Keik branch
        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Youin, 0)
			]);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(0)
			]);

        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			]);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(1, 1)
			]);
		process2.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Keik1)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5564)
			]);

        var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(0)
			]);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(1, 2)
			]);
		process3.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Keik2)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6139)
			]);

        var process4 = AddNewProcess(4);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(5564, 0, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6139, 0, 0, 0)
			]);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.Unknown(104, 657, 24245)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.Unknown(228, 130, 1, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.Unknown(228, 130, 2, 0, 0)
			]);
		process4.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2),
				QuestManager.ResultCommand.Unknown(104, 657, 24243)
			]);

		// 3b. Shenay branch
        var process5 = AddNewProcess(5);
		process5.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpcChoice(130, NpcId.Youin, 1)
			]);
		process5.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(1)
			]);

        var process6 = AddNewProcess(6);
		process6.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			]);
		process6.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(1, 1)
			]);
		process6.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Shenay1)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6147)
			]);

        var process7 = AddNewProcess(7);
		process7.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			]);
		process7.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.RandomEq(1, 2)
			]);
		process7.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Shenay2)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(6148)
			]);

        var process8 = AddNewProcess(8);
		process8.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6147, 0, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6148, 0, 0, 0)
			]);
		process8.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.Unknown(104, 655, 24244)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.Unknown(228, 130, 3, 0, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.Unknown(228, 130, 10, 0, 0)
			]);
		process8.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(2),
				QuestManager.ResultCommand.Unknown(104, 655, 24246)
			]);

        var process9 = AddNewProcess(9);
		process9.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3)
			]);
        process9.AddSpawnGroupsBlock(QuestAnnounceType.None, [EnemyGroupId.Encounter0, EnemyGroupId.Empty1])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundWithoutMarker(130, 39, -1)
			]);
        process9.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
		process9.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdMyQstFlagOn(4);

        var process10 = AddNewProcess(10);
		process10.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3)
			]);
		process10.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsOmBrokenQuest(130, 5, 0)
			]);
		process10.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdMyQstFlagOn(3128); // Guy FSM


        var process11 = AddNewProcess(11);
		process11.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3)
			]);
		process11.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsOmBrokenQuest(130, 7, 0)
			]);
		process11.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdMyQstFlagOn(3129); // Eileen FSM


        var process12 = AddNewProcess(12);
		process12.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3)
			]);
		process12.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsOmBrokenQuest(130, 9, 0)
			]);
		process12.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCmdMyQstFlagOn(3127); // Ethel FSM
    }
}

return new ScriptedQuest();

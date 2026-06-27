/**
 * @brief Feryana Wilderness Trial: Into the Ancient Darkness
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60319011; // Schedule ID: 1677100416
    public override ushort RecommendedLevel => 88;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.LookoutCastle1;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;

    private class EnemyGroupId
    {
		public const uint Group2 = 0;
		public const uint Group3 = 1;
		public const uint Group4 = 2;
		public const uint Group5 = 3;
		public const uint Group6 = 4;
		public const uint Group7 = 5;
		public const uint Group8 = 6;
        public const uint Group12 = 7;
        public const uint Set6385 = 6385;
        public const uint Set6407 = 6407;
        public const uint Set6618 = 6618;
    }

    private class NamedParamId
    {
        public const uint Unnamed1 = 1832; // ???
		public const uint Unnamed2 = 1833; // ???
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60319010));
        AddQuestOrderCondition(QuestOrderCondition.RequiredItemRank(94));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.ReinforcedBattleArmor, 1);
        AddFixedItemReward(ItemId.ScrollOfTheStarrySky, 1);
        AddFixedItemReward(ItemId.BrightPurpleSteelIngot, 1);
        AddFixedItemReward(ItemId.EvilEyeLens, 1);
    }

    protected override void InitializeEnemyGroups()
    {

        AddEnemies(EnemyGroupId.Group2, Stage.OldTekiaGrotto0, 2, QuestEnemyPlacementType.Manual, new()
        {
        });

        AddEnemies(EnemyGroupId.Group3, Stage.OldTekiaGrotto0, 3, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.UndeadMale, 88, 4200, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetStartThinkTblNo(2),
            LibDdon.Enemy.Create(EnemyId.UndeadMale, 88, 4200, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetStartThinkTblNo(2),
            LibDdon.Enemy.Create(EnemyId.UndeadMale, 88, 4200, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetStartThinkTblNo(2),
        });

        AddEnemies(EnemyGroupId.Group4, Stage.OldTekiaGrotto0, 4, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.UndeadFemale, 88, 4200, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.UndeadMale, 88, 4200, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetStartThinkTblNo(2),
            LibDdon.Enemy.Create(EnemyId.UndeadMale, 88, 4200, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetStartThinkTblNo(2),
            LibDdon.Enemy.Create(EnemyId.UndeadFemale, 88, 4200, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.UndeadFemale, 88, 4200, 7)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.UndeadMale, 88, 4200, 8)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetStartThinkTblNo(2),
        });

        // Empty group 1
        AddEnemies(EnemyGroupId.Group5, Stage.OldTekiaGrotto0, 5, QuestEnemyPlacementType.Manual, new()
        {
        });

        // Tentacles
        AddEnemies(EnemyGroupId.Set6385, Stage.OldTekiaGrotto0, 5, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 2)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 3)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 4)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.VileEye, 88, 4200, 5)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.VileEye, 88, 4200, 6)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
        });

        AddEnemies(EnemyGroupId.Group6, Stage.OldTekiaGrotto0, 6, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.VileEye, 88, 4200, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.VileEye, 88, 4200, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 7)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
        });

        AddEnemies(EnemyGroupId.Group7, Stage.OldTekiaGrotto0, 7, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.VileEye, 88, 4200, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.VileEye, 88, 4200, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 7)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 9)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1),
        });

        AddEnemies(EnemyGroupId.Group8, Stage.OldTekiaGrotto0, 8, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 8)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 10)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1),
        });

        AddEnemies(EnemyGroupId.Set6407, Stage.OldTekiaGrotto0, 9, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.EvilEye0, 88, 0, 0)
                .SetNamedEnemyParams(NamedParamId.Unnamed2)
                .SetStartThinkTblNo(1)
				.SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 1)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1)
                .SetIsManualSet(true),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 2)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1)
                .SetIsManualSet(true),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 3)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1)
                .SetIsManualSet(true),
        });

        AddEnemies(EnemyGroupId.Set6618, Stage.OldTekiaGrotto0, 11, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.EvilEye0, 88, 105000, 1)
                .SetNamedEnemyParams(NamedParamId.Unnamed2)
                .SetStartThinkTblNo(1)
				.SetIsBoss(true),
        });

        AddEnemies(EnemyGroupId.Group12, Stage.OldTekiaGrotto0, 12, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 0)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1)
                .SetIsManualSet(true),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 1)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1)
                .SetIsManualSet(true),
            LibDdon.Enemy.Create(EnemyId.TentacleEvilEye0, 88, 4200, 2)
                .SetNamedEnemyParams(NamedParamId.Unnamed1)
                .SetStartThinkTblNo(1)
                .SetIsManualSet(true),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear((QuestId)60319010);
        process0.AddNpcTalkAndOrderBlock(Stage.LookoutCastle1, NpcId.Nayajiku, 23902);
        process0.AddRawBlock(QuestAnnounceType.Accept)
			.AddResultCommands([
				QuestManager.ResultCommand.WorldManageQuestFlagOn(3286, 70031001),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Nayajiku, 23903)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(1063)
			]);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.QstLayoutFlagOn(5639),
				QuestManager.ResultCommand.QstLayoutFlagOn(6382),
				QuestManager.ResultCommand.QstLayoutFlagOn(6383), 
				QuestManager.ResultCommand.QstLayoutFlagOn(6384),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Guy, 23904)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(1063, 0, 0, 60319011)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3372)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitIn(1063, 0)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddResultCommands([
				QuestManager.ResultCommand.QstTalkChg(NpcId.Dana, 23905)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(1063, 1, 0, 60319011)
			]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set6385)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3374),
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100257)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Set6385, resetGroup: false);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set6407)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3375)
			]);
        process0.AddRawBlock(QuestAnnounceType.Update)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3376)
			]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set6618, resetGroup: false)
			.AddResultCommands([
				QuestManager.ResultCommand.CallGeneralAnnounce(1, 100258)
			]);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.LookoutCastle1, NpcId.Nayajiku, 25737)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3377),
				QuestManager.ResultCommand.QstLayoutFlagOff(6407),
				QuestManager.ResultCommand.QstTalkChg(NpcId.Guy, 23906)
			]);
        process0.AddProcessEndBlock(true);

        var process1 = AddNewProcess(1);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3372)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25738, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundWithoutMarker(1063, 3, -1)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25739, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundWithoutMarker(1063, 4, -1)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25744, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.SceHitInWithoutMarker(1063, 0)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25740, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3374)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25741, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3375)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25914, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsEnemyFoundWithoutMarker(1063, 9, 0)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25742, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3376)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25743, 595)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3377)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCommands([
				QuestManager.ResultCommand.PlayMessage(25736, 595)
			]);

        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.StageNo(1063)
			]);
        process2.AddSpawnGroupsBlock(QuestAnnounceType.None, [EnemyGroupId.Group2, EnemyGroupId.Group3, EnemyGroupId.Group4, EnemyGroupId.Group5])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3375)
			]);
        process2.AddSpawnGroupsBlock(QuestAnnounceType.None, [EnemyGroupId.Group6, EnemyGroupId.Group7, EnemyGroupId.Group8])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(3376)
			]);
        process2.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Group12);

        var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsMyquestLayoutFlagOn(6407)
			]);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.EmHpLess(1063, 9, 0, 50)
			]);
		process3.AddRemoveGroupBlock(QuestAnnounceType.None, EnemyGroupId.Set6407)
			.AddResultCommands([
				QuestManager.ResultCommand.MyQstFlagOn(3376)
			]);
    }
}

return new ScriptedQuest();

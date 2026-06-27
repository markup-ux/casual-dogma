/**
 * @brief Rathnite Foothills Trial: Demon Tank Catoblepas
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => (QuestId)60318011;
    public override ushort RecommendedLevel => 85;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.FortThines1;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;

    private class EnemyGroupId
    {
		public const uint Empty1 = 0;
		public const uint Empty2 = 1;
		public const uint Empty3 = 2;
        public const uint Encounter1 = 10;
        public const uint Encounter2 = 11;
        public const uint Encounter3 = 12;
        public const uint Encounter4 = 13;
    }

    private class NamedParamId
    {
        public const uint BaseDefense  = 1828;
		public const uint DefenceOfficer = 1829;
        public const uint ArtilleryOfficer  = 1838;
    }

    private class QstLayoutFlag
    {
        public const uint AlliedMeirova = 5638; // Meirova — chosen ally (GroupNo 0)
        public const uint CannonOm1     = 6345; // Cannon OM 1 (GroupNo 1)
        public const uint CannonOm2     = 6346; // Cannon OM 2 (GroupNo 2)
        public const uint AlliedLise    = 6339; // Lise — chosen ally (GroupNo 3)
        public const uint AlliedAlways  = 6347; // Dudley + Dean — always present (GroupNo 4)
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared((QuestId)60318010));
        AddQuestOrderCondition(QuestOrderCondition.RequiredItemRank(84));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 42000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.RoyalCrestMedalRathniteDistrict, 5);
    }

    protected override void InitializeEnemyGroups()
    {
        // Empty group 1
        AddEnemies(EnemyGroupId.Empty1, Stage.RathniteFoothillsLakeside0, 60, QuestEnemyPlacementType.Manual, new()
        {
        });

        // Empty group 2
        AddEnemies(EnemyGroupId.Empty2, Stage.RathniteFoothillsLakeside0, 18, QuestEnemyPlacementType.Manual, new()
        {
        });

        // Empty group 3
        AddEnemies(EnemyGroupId.Empty3, Stage.RathniteFoothillsLakeside0, 50, QuestEnemyPlacementType.Manual, new()
        {
        });

        // First wave
        AddEnemies(EnemyGroupId.Encounter1, Stage.RathniteFoothillsLakeside0, 17, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 4)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 5)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 6)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 4200, 7)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 4200, 8)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 21000, 10)
                .SetNamedEnemyParams(NamedParamId.ArtilleryOfficer),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 21000, 11)
                .SetNamedEnemyParams(NamedParamId.ArtilleryOfficer),
        });

        // Second wave
        AddEnemies(EnemyGroupId.Encounter2, Stage.RathniteFoothillsLakeside0, 50, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 83, 21000, 1)
                .SetNamedEnemyParams(NamedParamId.DefenceOfficer),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 3)
                .SetEnemyTargetTypesId(TargetTypesId.Normal)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
        });

        // Third wave
        AddEnemies(EnemyGroupId.Encounter3, Stage.RathniteFoothillsLakeside0, 51, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 83, 21000, 0)
                .SetNamedEnemyParams(NamedParamId.DefenceOfficer),
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 83, 105000, 2)
				.SetIsBoss(true),
        });

        // Catoblepas
        AddEnemies(EnemyGroupId.Encounter4, Stage.RathniteFoothillsLakeside0, 5, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 4200, 2)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 4200, 3)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 83, 4200, 4)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 5)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.HeavySoldierDwarfOrc, 83, 4200, 6)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 85, 4200, 7)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.SwordSoldierDwarfOrc, 85, 4200, 8)
                .SetNamedEnemyParams(NamedParamId.BaseDefense),
            LibDdon.Enemy.Create(EnemyId.Catoblepas, 83, 84563, 9)
				.SetIsBoss(true),
        });
    }

    protected override void InitializeBlocks()
    {
        // 0. Order quest
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear((QuestId)60318010);
        process0.AddNpcTalkAndOrderBlock(Stage.FortThines1, NpcId.Endale, 23897);

        // 1. Check the situation with Victor
        process0.AddTalkToNpcBlock(QuestAnnounceType.Accept, Stage.RathniteFoothillsLakeside0, NpcId.Victor, 23899)
            .AddResultCmdQstTalkChg(NpcId.Endale, 23898);

        // 2. Ask Ozgur about the strength of the Liberation Army
		process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
            .AddResultCmdQstTalkChg(NpcId.Ozgur, 23900)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(2)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.TalkNpc(631, NpcId.Ozgur),
				QuestManager.CheckCommand.DummyNotProgress()
			]);

        // 3. Head into enemy territory and strike with a surprise attack
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter1)
            .AddResultCmdQstTalkChg(NpcId.Ozgur, 25529)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, QstLayoutFlag.AlliedAlways)
            .AddQuestFlag(QuestFlagType.WorldManageQuest, QuestFlagAction.Set, 3284, (QuestId)70030001);

        // 4. Destroy enemy artillery
		process0.AddRawBlock(QuestAnnounceType.Update)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3293)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, QstLayoutFlag.CannonOm1)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, QstLayoutFlag.CannonOm2)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsOmBrokenQuest(131, 1, 0)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.IsOmBrokenQuest(131, 2, 0)
			]);

        // 5. Defeat the leader of the reinforcements
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter2)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3294);
		process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdDieEnemy(Stage.RathniteFoothillsLakeside0, 50, 1);

        // 6. Head to the enemy's main stronghold deeper in
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter3)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3295);

        // 7. Defeat enemy forces in the mountains
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter3, resetGroup: false)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3296);

        // 8. Head to the deep stronghold again
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter4)
            .AddQuestFlag(QuestFlagType.MyQst, QuestFlagAction.Set, 3297);

        // 9. Subdue the enemy's main unit
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter4, resetGroup: false);

        // 10. Report the War Situation results to Endale
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FortThines1, NpcId.Endale, 25542)
            .AddResultCmdQstTalkChg(NpcId.Ozgur, 25543)
            .AddResultCmdQstTalkChg(NpcId.Victor, 25544)
            .AddResultCmdMyQstFlagOn(3);
        process0.AddProcessEndBlock(true);

		// Branch 1 - Meirova
        var process1 = AddNewProcess(1);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.RothgillTravelersInn, NpcId.Ozgur, 0);
		process1.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdMyQstFlagOn(1)
            .AddCheckCmdMyQstFlagOn(3293);
		process1.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 5638)
			.AddResultCmdPlayMessage(25530,31)
            .AddCheckCmdMyQstFlagOn(3294);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25534,31)
            .AddCheckCmdMyQstFlagOn(3295);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25536,31)
            .AddCheckCmdMyQstFlagOn(3297);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25538,31)
            .AddCheckCmdMyQstFlagOn(3);
		process1.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25540,31)
            .AddResultCmdQstTalkChg(NpcId.Meirova0, 25545);

		// Branch 2 - Lise
        var process2 = AddNewProcess(2);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdTalkNpcChoice(Stage.RothgillTravelersInn, NpcId.Ozgur, 1);
		process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdMyQstFlagOn(2)
            .AddCheckCmdMyQstFlagOn(3293);
		process2.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.QstLayout, QuestFlagAction.Set, 6339)
			.AddResultCmdPlayMessage(25531,13)
            .AddCheckCmdMyQstFlagOn(3294);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25535,13)
            .AddCheckCmdMyQstFlagOn(3295);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25537,13)
            .AddCheckCmdMyQstFlagOn(3297);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25539,13)
            .AddCheckCmdMyQstFlagOn(3);
		process2.AddRawBlock(QuestAnnounceType.None)
			.AddResultCmdPlayMessage(25541,13)
            .AddResultCmdQstTalkChg(NpcId.Lise0, 25546);

		// Send empty groups
		var process3 = AddNewProcess(3);
		process3.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(1),
				QuestManager.CheckCommand.StageNo(131)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.MyQstFlagOn(2),
				QuestManager.CheckCommand.StageNo(131)
			]);
        process3.AddSpawnGroupsBlock(QuestAnnounceType.None, [EnemyGroupId.Empty1, EnemyGroupId.Empty2, EnemyGroupId.Empty3]);
    }
}

return new ScriptedQuest();

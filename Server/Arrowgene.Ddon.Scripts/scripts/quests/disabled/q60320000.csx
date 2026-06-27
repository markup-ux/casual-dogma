/**
 * @brief Megadosys Plateau Trial: Material Procurement
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.MegadosysPlateauTrialMaterialProcurement; // Schedule ID: 1677721600
    public override ushort RecommendedLevel => 90;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.EliGuardTower;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;
    public override bool Enabled => false;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.MegadosysPlateau, 3));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.RoyalCrestMedalMegadosysDistrict, 3);
        AddFixedItemReward(ItemId.MisleadingTwig, 1);
        AddFixedItemReward(ItemId.BlazingOre, 1);
    }

    private class EnemyGroupId
    {
        public const uint Encounter = 10;
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter, Stage.MegadosysPlateau, 15, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.WarReadyGorecyclopsLightArmor0, 95, 105000, 3)
                .SetInfectionType(3)
                .SetIsBoss(true),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 95, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 95, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 95, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 95, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 95, 4200, 8),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 95, 4200, 9),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdCheckAreaRank(QuestAreaId.MegadosysPlateau, 3);
        process0.AddNpcTalkAndOrderBlock(Stage.EliGuardTower, NpcId.Doris, 28280)
            .AddQuestFlag(QuestFlagAction.Set, QuestFlags.EliGuardTower.Doris);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Accept, Stage.EliGuardTower, 0, 0, NpcId.Hayal, 28283)
            .AddResultCmdQstLayoutFlagOn(7486)
            .AddResultCmdQstTalkChg(NpcId.Doris, 28281);
        process0.AddRawBlock(QuestAnnounceType.CheckpointAndUpdate)
            .AddResultCmdQstTalkChg(NpcId.Hayal, 28284)
            .AddResultCmdQstLayoutFlagOn(7487)
            .AddResultCmdQstLayoutFlagOn(7488)
            .AddResultCmdQstLayoutFlagOn(7489)
            .AddResultCmdQstLayoutFlagOn(7490)
            .AddResultCmdQstLayoutFlagOn(7491)
            .AddResultCmdQstLayoutFlagOn(7492)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7490)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7491)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7492)
            ]);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7490),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7491)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7490),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7492)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7491),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7492)
            ]);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7490),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7491),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7492)
            ]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.EliGuardTower, 0, 0, NpcId.Hayal, 28285)
            .AddResultCmdQstLayoutFlagOff(7490)
            .AddResultCmdQstLayoutFlagOff(7491)
            .AddResultCmdQstLayoutFlagOff(7492);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter)
            .AddResultCmdWorldManageLayoutFlagOn(7946, QuestId.Q70032001)
            .AddResultCmdQstLayoutFlagOff(7486)
            .AddResultCmdQstLayoutFlagOn(7495)
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOn(0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOn(1)
            ]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter, resetGroup: false);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Update, Stage.MegadosysPlateau, 4, 0, NpcId.Hayal, 28288);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.EliGuardTower, NpcId.Doris, 28282)
            .AddResultCmdQstLayoutFlagOff(7495);
        process0.AddProcessEndBlock(true);

        var process1 = AddNewProcess(1);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMyquestLayoutFlagOn(7490);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdQuestOmReleaseTouch(Stage.MegadosysPlateau, 1, 0);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdQstLayoutFlagOff(7490);

        var process2 = AddNewProcess(2);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMyquestLayoutFlagOn(7491);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdQuestOmReleaseTouch(Stage.MegadosysPlateau, 3, 0);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdQstLayoutFlagOff(7491);

        var process3 = AddNewProcess(3);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMyquestLayoutFlagOn(7492);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdQuestOmReleaseTouch(Stage.QuietConcealedCave, 1, 0);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdQstLayoutFlagOff(7492);

        var process4 = AddNewProcess(4);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMyquestLayoutFlagOn(7495);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdQstTalkChg(NpcId.Hayal, 28286)
            .AddCheckCommands([
                QuestManager.CheckCommand.NewTalkNpc(133, 4, 0, 60320000)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOn(1)
            ]);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOff(1)
            ]);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdTutorialDialog(TutorialId.ArtilleryThatPlayersCanOperate)
            .AddResultCmdMyQstFlagOn(0)
            .AddResultCmdQstTalkChg(NpcId.Hayal, 28287);

        var process5 = AddNewProcess(5);
        process5.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsMyquestLayoutFlagOn(7495);
        process5.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsEnemyFoundWithoutMarker(133, 15, -1)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOn(0)
            ]);
        process5.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOff(0)
            ]);
        process5.AddRawBlock(QuestAnnounceType.None)
            .AddResultCmdMyQstFlagOn(1)
            .AddResultCmdPlayMessage(28290, 0);
    }
}

return new ScriptedQuest();

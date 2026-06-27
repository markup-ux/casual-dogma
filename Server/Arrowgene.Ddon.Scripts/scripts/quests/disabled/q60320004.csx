/**
 * @brief Megadosys Plateau: Prevent Enemy Attack
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.MegadosysPlateauPreventEnemyAttack; // Schedule ID: 1677722112
    public override ushort RecommendedLevel => 93;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.MegadosysPlateau;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.QuestUsefulForAdventure;
    public override bool Enabled => false;

    private class EnemyGroupId
    {
        public const uint Encounter0 = 10;
    }

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.MegadosysPlateau, 5));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.CorruptedSealerStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.MegadosysPlateau, 59, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.WarReadyOgreLightArmor, 93, 105000, 5)
                .SetIsBoss(true),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.CheckAreaRank(20, 5)
            ]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.MegadosysPlateau, 0, 0, NpcId.Bruce, 28229)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7471)
            ]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.Accept, Stage.MegadosysPlateau, 0, 0, NpcId.Bruce, 28230);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7472),
                QuestManager.ResultCommand.QstLayoutFlagOn(7473),
                QuestManager.ResultCommand.QstLayoutFlagOn(7474)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(10)
            ]);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(10)
            ]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.MegadosysPlateau, 0, 0, NpcId.Bruce, 28233)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(7472),
                QuestManager.ResultCommand.QstLayoutFlagOff(7473),
                QuestManager.ResultCommand.QstLayoutFlagOff(7474)
            ]);
        process0.AddProcessEndBlock(true);

        // Branch 1a - Anna
        var process1 = AddNewProcess(1);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7472)
            ]);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Anna, 28231)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.QuestTalkNpcRadius(133, 1, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7472)
            ]);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7472)
            ]);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(7473),
                QuestManager.ResultCommand.QstLayoutFlagOff(7474)
            ]);

        // Branch 1b - Dana
        var process2 = AddNewProcess(2);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7473)
            ]);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Dana, 28234)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.QuestTalkNpcRadius(133, 2, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7473)
            ]);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7473)
            ]);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(7472),
                QuestManager.ResultCommand.QstLayoutFlagOff(7474)
            ]);

        var process3 = AddNewProcess(3);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7472),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7474)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7473),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7474)
            ]);
        process3.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Anna, 28232),
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Dana, 28236),
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Versa, 28243)
            ]);
        process3.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(10)
            ]);

        // Branch 2 - Versa
        var process4 = AddNewProcess(4);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7474)
            ]);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Versa, 28235)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.QuestTalkNpcRadius(133, 3, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7474)
            ]);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7474)
            ]);
        process4.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(7472),
                QuestManager.ResultCommand.QstLayoutFlagOff(7473)
            ]);

        var process5 = AddNewProcess(5);
        process5.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7472),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOff(7473),
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(7474)
            ]);
        process5.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7470),
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Versa, 28237),
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Anna, 28241),
                QuestManager.ResultCommand.QstTalkChgFsm(NpcId.Dana, 28242)
            ]);
        process5.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
        process5.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(10)
            ]);
    }
}

return new ScriptedQuest();

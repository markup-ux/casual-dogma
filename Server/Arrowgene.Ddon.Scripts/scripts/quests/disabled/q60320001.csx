/**
 * @brief Megadosys Plateau: Rescue Request
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.MegadosysPlateauRescueRequest; // Schedule ID: 1677721728
    public override ushort RecommendedLevel => 92;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.MegadosysPlateau;
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

        AddFixedItemReward(ItemId.CursedExorciserStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.MegadosysPlateau, 45, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.DeathKnight, 92, 105000, 0)
                .SetIsBoss(true),
        });

        AddEnemies(EnemyGroupId.Encounter1, Stage.MegadosysPlateau, 40, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.GrudgeGhost, 92, 4200, 0)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.GrudgeGhost, 92, 4200, 1)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
            LibDdon.Enemy.Create(EnemyId.GrudgeGhost, 92, 4200, 2)
                .SetEnemyTargetTypesId(TargetTypesId.Normal),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.CheckAreaRank(20, 2)
            ]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.MegadosysPlateau, 0, 0, NpcId.Eileen, 28153)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7449)
            ]);
        process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChg(NpcId.Eileen, 28154),
                QuestManager.ResultCommand.QstTalkChg(NpcId.Umit, 28147)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.MyQstFlagOn(0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.TalkNpc(636, NpcId.Umit),
                QuestManager.CheckCommand.DummyNotProgress()
            ]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false)
            .AddResultCommands([
                QuestManager.ResultCommand.StartTimer(1, 20)
            ]);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.MegadosysPlateau, 0, 0, NpcId.Eileen, 28152)
            .AddResultCommands([
                QuestManager.ResultCommand.MyQstFlagOn(4378),
                QuestManager.ResultCommand.QstLayoutFlagOff(10)
            ]);
        process0.AddProcessEndBlock(true);

        var process1 = AddNewProcess(1);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.TalkNpcChoice(636, NpcId.Umit, 0)
            ]);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.MyQstFlagOn(0),
                QuestManager.ResultCommand.QstTalkChg(NpcId.Umit, 28150)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsEndTimer(2),
                QuestManager.CheckCommand.MyQstFlagOff(4378)
            ]);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7448),
                QuestManager.ResultCommand.CallGeneralAnnounce(1, 100562)
            ]);

        var process2 = AddNewProcess(2);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.TalkNpcChoice(636, NpcId.Umit, 1)
            ]);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.MyQstFlagOn(0),
                QuestManager.ResultCommand.QstTalkChg(NpcId.Umit, 28151)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.IsEndTimer(2),
                QuestManager.CheckCommand.MyQstFlagOff(4378)
            ]);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7450),
                QuestManager.ResultCommand.CallGeneralAnnounce(1, 100562)
            ]);

        var process3 = AddNewProcess(3);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsMyquestLayoutFlagOn(10)
            ]);
        process3.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.IsEndTimer(1),
                QuestManager.CheckCommand.MyQstFlagOff(4378)
            ]);
        process3.AddSpawnGroupBlock(QuestAnnounceType.None, EnemyGroupId.Encounter1)
            .AddResultCommands([
                QuestManager.ResultCommand.StartTimer(2, 30),
                QuestManager.ResultCommand.CallGeneralAnnounce(1, 100563)
            ]);
    }
}

return new ScriptedQuest();

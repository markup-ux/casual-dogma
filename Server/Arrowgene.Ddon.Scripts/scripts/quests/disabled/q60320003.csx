/**
 * @brief Megadosys Plateau: Pursue and Defeat Enemies
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.MegadosysPlateauPursueAndDefeatEnemies; // Schedule ID: 1677721984
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

        AddFixedItemReward(ItemId.AlchemySealerStone, 1);
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Encounter0, Stage.MegadosysPlateau, 52, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.SquadLeaderDwarfOrc, 93, 4200, 0),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 1),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 2),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 3),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 4),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 5),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 6),
            LibDdon.Enemy.Create(EnemyId.RangedSoldierDwarfOrc, 93, 4200, 7),
            LibDdon.Enemy.Create(EnemyId.BluntSoldierDwarfOrc, 93, 4200, 8),
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCommands([
                QuestManager.CheckCommand.CheckAreaRank(20, 5)
            ]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.MegadosysPlateau, 4, 0, NpcId.Dean, 28220)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOn(7460)
            ]);
        process0.AddRawBlock(QuestAnnounceType.Accept)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChg(NpcId.Dean, 28221)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.TalkNpcChoice(133, NpcId.Dean, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.NewTalkNpc(133, 4, 0, 60320001),
                QuestManager.CheckCommand.DummyNotProgress()
            ]);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCommands([
                QuestManager.ResultCommand.QstTalkChg(NpcId.Dean, 28222),
                QuestManager.ResultCommand.LayoutFlagRandomOn(7462, 7463, -1, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.OmSetTouchRadius(133, 0, 0, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.OmSetTouchRadius(133, 1, 0, 0)
            ]);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(7462),
                QuestManager.ResultCommand.QstLayoutFlagOff(7463),
                QuestManager.ResultCommand.LayoutFlagRandomOn(7464, 7465, -1, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.OmSetTouchRadius(133, 2, 0, 0)
            ])
            .AddCheckCommands([
                QuestManager.CheckCommand.OmSetTouchRadius(133, 3, 0, 0)
            ]);
        process0.AddDiscoverGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0)
            .AddResultCommands([
                QuestManager.ResultCommand.QstLayoutFlagOff(7464),
                QuestManager.ResultCommand.QstLayoutFlagOff(7465)
            ]);
        process0.AddDestroyGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Encounter0, resetGroup: false);
        process0.AddNewTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.MegadosysPlateau, 4, 0, NpcId.Dean, 28223);
        process0.AddProcessEndBlock(true);
    }
}

return new ScriptedQuest();

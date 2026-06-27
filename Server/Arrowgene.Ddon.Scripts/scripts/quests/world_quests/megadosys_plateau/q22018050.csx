/**
 * @brief Flames of Darkness
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.World;
    public override QuestId QuestId => (QuestId)22018050; // Schedule ID: 579115264
    public override ushort RecommendedLevel => 95;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => true;
    public override uint NewsImageId => 340;
    public override QuestAreaId QuestAreaId => QuestAreaId.MegadosysPlateau;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.HasAreaRank(QuestAreaId.MegadosysPlateau, 9));
    }

    private class EnemyGroupId
    {
        public const uint Set7644 = 7644;
        public const uint Set7645 = 7645;
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 50400);
        AddWalletReward(WalletType.Gold, 5500);
        AddWalletReward(WalletType.RiftPoints, 665);

        AddSelectItemReward(new()
        {
            (ItemId.BurningMagmaLump, 1),
            (ItemId.SuperiorHealingRemedy, 10),
            (ItemId.SuperiorStrongGalaExtract, 10),
        });
    }

    protected override void InitializeEnemyGroups()
    {
        AddEnemies(EnemyGroupId.Set7644, Stage.TheKingsRoomofConcealment0, 2, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.Ifrit1stForm, 95, 0, 0)
				.SetIsBoss(true),
        });

        AddEnemies(EnemyGroupId.Set7645, Stage.TheKingsRoomofConcealment0, 1, QuestEnemyPlacementType.Manual, new()
        {
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 1)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 2)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 3)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 4)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 5)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 6)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 7)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.BlazingBoulder, 95, 0, 8)
				.SetRepopCount(50)
				.SetIsRequired(false),
            LibDdon.Enemy.Create(EnemyId.Ifrit2ndForm, 95, 21000, 9)
				.SetStartThinkTblNo(1)
				.SetIsBoss(true),

			// Add caution spot drops to Ifrit later
        });
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.CheckAreaRank(20, 9),
				QuestManager.CheckCommand.WorldManageQuestFlagOn(4580, 70032001),
				QuestManager.CheckCommand.StageNo(133)
			])
			.AddCheckCommands([
				QuestManager.CheckCommand.NewTalkNpc(133, 0, 0, 0)
			]);
        process0.AddNewNpcTalkAndOrderBlock(Stage.MegadosysPlateau, 0, 0, NpcId.MegadoGuard0, 28774)
			.AddResultCmdQstLayoutFlagOn(7651);
        process0.AddPartyGatherBlock(QuestAnnounceType.Accept, Stage.MegadosysPlateau, -8649, 22089, -314957)
			.AddResultCmdQstTalkChg(NpcId.MegadoGuard0, 28775);
		process0.AddEventAfterJumpBlock(QuestAnnounceType.None, Stage.TheKingsRoomofConcealment0, 5, 0);
        process0.AddSpawnGroupBlock(QuestAnnounceType.Update, EnemyGroupId.Set7644)
			.AddCheckCmdIsLinkageEnemyFlag(Stage.TheKingsRoomofConcealment0, 2, 0, 2);
        process0.AddDestroyGroupBlock(QuestAnnounceType.None, EnemyGroupId.Set7645);
        process0.AddProcessEndBlock(true);

        var process1 = AddNewProcess(1);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCmdIsLinkageEnemyFlag(Stage.TheKingsRoomofConcealment0, 1, 9, 4);
        process1.AddRemoveGroupBlock(QuestAnnounceType.None, EnemyGroupId.Set7644)
			.AddCheckCmdDieEnemy(Stage.TheKingsRoomofConcealment0, 1, 9);
        process1.AddRemoveGroupBlock(QuestAnnounceType.None, EnemyGroupId.Set7645);
    }
}

return new ScriptedQuest();

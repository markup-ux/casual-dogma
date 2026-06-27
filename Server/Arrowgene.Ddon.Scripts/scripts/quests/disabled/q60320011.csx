/**
 * @brief The Ruler Overwhelmed by Power
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.Tutorial;
    public override QuestId QuestId => QuestId.TheRulerOverwhelmedByPower; // Schedule ID: 1677723008
    public override ushort RecommendedLevel => 95;
    public override byte MinimumItemRank => 104;
    public override bool IsDiscoverable => false;
    public override StageInfo StageInfo => Stage.FortressCityMegadoResidentialLevel1;
    public override QuestAdventureGuideCategory? AdventureGuideCategory => QuestAdventureGuideCategory.AreaTrialOrMission;
    public override bool Enabled => false;

    protected override void InitializeState()
    {
        AddQuestOrderCondition(QuestOrderCondition.PersonalQuestCleared(QuestId.MegadosysPlateauTrialShadowOfHeresy));
        AddQuestOrderCondition(QuestOrderCondition.RequiredItemRank(104));
    }

    protected override void InitializeRewards()
    {
        AddPointReward(PointType.ExperiencePoints, 105000);
        AddWalletReward(WalletType.Gold, 11000);
        AddWalletReward(WalletType.RiftPoints, 2000);

        AddFixedItemReward(ItemId.LowGradeReinforcedArmor, 1);
        AddFixedItemReward(ItemId.Padding, 1);
        AddFixedItemReward(ItemId.FireproofCord, 1);
    }

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddRawBlock(QuestAnnounceType.None)
            .AddCheckCmdIsTutorialQuestClear(QuestId.MegadosysPlateauTrialShadowOfHeresy);
        process0.AddNpcTalkAndOrderBlock(Stage.FortressCityMegadoResidentialLevel1, NpcId.Doris, 28298);
        process0.AddIsStageNoBlock(QuestAnnounceType.Accept, Stage.TheKingsRoomofConcealment0)
            .AddWorldManageUnlock(QuestFlags.MegadosysPlateau.TheKingsHiddenChamberGates)
            .AddResultCmdQstTalkChg(NpcId.Doris, 28299);
        process0.AddRawBlock(QuestAnnounceType.Update)
            .AddCheckCmdEmDieForRandomDungeon(Stage.TheKingsRoomofConcealment0, EnemyId.Ifrit2ndForm, 1);
        process0.AddTalkToNpcBlock(QuestAnnounceType.CheckpointAndUpdate, Stage.FortressCityMegadoResidentialLevel1, NpcId.Doris, 28300);
        process0.AddProcessEndBlock(true)
            .AddResultCmdAchievementBanner(6, 9); // This should show the achievement name, but how?
    }
}

return new ScriptedQuest();

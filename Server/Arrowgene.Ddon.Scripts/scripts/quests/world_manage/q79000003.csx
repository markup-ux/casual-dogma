/**
 * @brief Quest used to test out flags without reloading the server or changing quest files
 */

#load "libs.csx"

public class ScriptedQuest : IQuest
{
    public override QuestType QuestType => QuestType.WorldManage;
    public override QuestId QuestId => QuestId.WorldManageDebug;
    public override ushort RecommendedLevel => 0;
    public override byte MinimumItemRank => 0;
    public override bool IsDiscoverable => false;

    protected override void InitializeBlocks()
    {
        var process0 = AddNewProcess(0);
        process0.AddNoProgressBlock();
        process0.AddNoProgressBlock();
        process0.AddProcessEndBlock(false);

        var process1 = AddNewProcess(1);
        process1.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsTutorialQuestClear(60318011)
			]);
        process1.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.WorldManageQuest, QuestFlagAction.Set, 3284, (QuestId)70030001)
			.AddCheckCommands([
				QuestManager.CheckCommand.DummyNotProgress()
			]);

        var process2 = AddNewProcess(2);
        process2.AddRawBlock(QuestAnnounceType.None)
			.AddCheckCommands([
				QuestManager.CheckCommand.IsTutorialQuestClear(60319011)
			]);
        process2.AddRawBlock(QuestAnnounceType.None)
            .AddQuestFlag(QuestFlagType.WorldManageQuest, QuestFlagAction.Set, 3286, (QuestId)70031001)
			.AddCheckCommands([
				QuestManager.CheckCommand.DummyNotProgress()
			]);
    }
}

return new ScriptedQuest();

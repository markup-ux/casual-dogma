public class RankingBoardResetTask : WeeklyTask
{
    public RankingBoardResetTask(DayOfWeek day, uint hour, uint minute)
        : base(TaskType.RankingBoardReset, day, hour, minute) { }

    public override void RunTask(DdonGameServer server)
    {
        // TODO: Hand out rewards based on final rankings before clearing.
        server.Database.DeleteAllRankRecords();
    }
}

return new RankingBoardResetTask(DayOfWeek.Monday, 5, 0);

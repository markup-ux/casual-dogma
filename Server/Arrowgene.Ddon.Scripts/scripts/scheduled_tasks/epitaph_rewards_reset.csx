public class EpitaphSchedulerTask : WeeklyTask
{
    public EpitaphSchedulerTask(DayOfWeek day, uint hour, uint minute)
        : base(TaskType.EpitaphRoadRewardsReset, day, hour, minute) { }

    public override bool IsEnabled(DdonGameServer server) =>
        server.GameSettings.GameServerSettings.EnableEpitaphWeeklyRewards;

    public override void RunTask(DdonGameServer server)
    {
        server.Database.DeleteWeeklyEpitaphClaimedRewards();
        server.RpcManager.AnnounceAll("internal/command", RpcInternalCommand.EpitaphRoadWeeklyReset, null);
    }
}

return new EpitaphSchedulerTask(DayOfWeek.Monday, 5, 0);

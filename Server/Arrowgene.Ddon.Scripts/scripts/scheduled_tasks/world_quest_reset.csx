public class WorldQuestResetTask : WeeklyTask
{
    public WorldQuestResetTask(DayOfWeek day, uint hour, uint minute)
        : base(TaskType.WorldQuestRotation, day, hour, minute) { }

    public override bool IsEnabled(DdonGameServer server)
    {
        var settings = server.GameSettings.GameServerSettings;
        return settings.WorldQuestSystem == WorldQuestSystemMode.ServerReset
            || settings.WorldQuestFirstClearRewards;
    }

    public override void RunTask(DdonGameServer server)
    {
        long seed = WorldQuestManager.ComputeCurrentPeriodSeed(
            Day, Hour, Minute,
            server.GameSettings.GameServerSettings.GetEffectiveUtcOffset());
        server.RpcManager.AnnounceAll("internal/command", RpcInternalCommand.WorldQuestReset, seed);
    }
}

return new WorldQuestResetTask(DayOfWeek.Thursday, 10, 0);

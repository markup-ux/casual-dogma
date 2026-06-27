public class SupplyCacheResetTask : DailyTask
{
    public SupplyCacheResetTask(uint hour, uint minute)
        : base(TaskType.SupplyCacheReset, hour, minute) { }

    public override bool IsEnabled(DdonGameServer server) =>
        server.GameSettings.GameServerSettings.SupplyCachesEnabled
        && server.GameSettings.GameServerSettings.SupplyCacheCleanupWeekly
        && server.GameSettings.GameServerSettings.SupplyCacheLifetimeDays > 0;

    public override void RunTask(DdonGameServer server)
    {
        server.Database.ExecuteInTransaction(connection =>
        {
            server.SupplyCacheManager.ClearExpiredCaches(connection);
        });
    }
}

return new SupplyCacheResetTask(5, 0);

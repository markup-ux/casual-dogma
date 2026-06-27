public class ExtremeMissionRewardResetTask : DailyTask
{
    public ExtremeMissionRewardResetTask(uint hour, uint minute)
        : base(TaskType.ExtremeMissionRewardUpdate, hour, minute) { }

    public override void RunTask(DdonGameServer server)
    {
        server.RpcManager.AnnounceAll("internal/command", RpcInternalCommand.ExtremeMissionRewardReset, null);
    }
}

return new ExtremeMissionRewardResetTask(5, 0);

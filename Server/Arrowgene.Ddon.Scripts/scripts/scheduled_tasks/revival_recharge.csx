public class RevivalRechargeTask : SecondlyTask
{
    public RevivalRechargeTask() : base(TaskType.RevivalGreenGemstones, 30) { }

    public override void RunTask(DdonGameServer server)
    {
        server.RevivalManager.ProcessAllOnline();
    }
}

return new RevivalRechargeTask();

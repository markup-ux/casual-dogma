public class PawnLikabilityIncreaseResetTask : DailyTask
{
    public PawnLikabilityIncreaseResetTask(uint hour, uint minute)
        : base(TaskType.PawnAffectionIncreaseInteractionReset, hour, minute) { }

    public override void RunTask(DdonGameServer server)
    {
        server.Database.DeleteAllPartnerPawnLastAffectionIncreaseRecords();
    }
}

return new PawnLikabilityIncreaseResetTask(5, 0);

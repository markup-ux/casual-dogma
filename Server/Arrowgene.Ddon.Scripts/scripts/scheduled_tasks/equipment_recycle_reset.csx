public class EquipmentRecycleResetTask : DailyTask
{
    public EquipmentRecycleResetTask(uint hour, uint minute)
        : base(TaskType.EquipmentRecycleReset, hour, minute) { }

    public override void RunTask(DdonGameServer server)
    {
        server.Database.ResetRecyleEquipmentRecords();
    }
}

return new EquipmentRecycleResetTask(5, 0);

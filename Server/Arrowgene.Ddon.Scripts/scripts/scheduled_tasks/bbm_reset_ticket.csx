public class BBMResetTicketTask : WeeklyTask
{
    public BBMResetTicketTask(DayOfWeek day, uint hour, uint minute)
        : base(TaskType.AwardBitterblackMazeResetTickets, day, hour, minute) { }

    public override void RunTask(DdonGameServer server)
    {
        server.Database.ExecuteInTransaction(connection =>
        {
            server.Database.ResetBBMResetTicketStatus(connection);
            server.Database.ResetBBMGGReset(connection);
        });
    }
}

return new BBMResetTicketTask(DayOfWeek.Monday, 5, 0);

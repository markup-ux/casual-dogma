public class GroupChatPruningTask : DailyTask
{
    public GroupChatPruningTask(uint hour, uint minute)
        : base(TaskType.GroupChatPruning, hour, minute) { }

    public override void RunTask(DdonGameServer server)
    {
        server.Database.PruneGroupChatGroups();
    }
}

return new GroupChatPruningTask(0, 0);

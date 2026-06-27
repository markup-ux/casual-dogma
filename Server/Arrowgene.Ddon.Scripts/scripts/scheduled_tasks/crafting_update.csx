public class CraftingSchedulerTask : SecondlyTask
{
    public CraftingSchedulerTask() : base(TaskType.Crafting, 1) { }

    public override void RunTask(DdonGameServer server)
    {
        server.RpcManager.AnnounceAll("internal/command", RpcInternalCommand.UpdateCrafting, null);
    }
}

return new CraftingSchedulerTask();

using Arrowgene.Ddon.Shared.Model.Scheduler;
using System;

namespace Arrowgene.Ddon.GameServer.Tasks
{
    public abstract class HourlyTask : SchedulerTask
    {
        public HourlyTask(TaskType type) : base(ScheduleInterval.Hourly, type)
        {
        }

        public override long NextTimestamp()
        {
            var now = DateTimeOffset.UtcNow.ToOffset(Offset);
            var next = now.AddHours(1);
            var nextTime = new DateTimeOffset(next.Year, next.Month, next.Day, next.Hour, 0, 0, Offset);
            return nextTime.ToUnixTimeSeconds();
        }

        public override string TaskTypeName()
        {
            return "Hourly Task";
        }
    }
}

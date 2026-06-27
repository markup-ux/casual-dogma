using Arrowgene.Ddon.Shared.Model.Scheduler;
using System;

namespace Arrowgene.Ddon.GameServer.Tasks
{
    public abstract class DailyTask : SchedulerTask
    {
        public uint Hour { get; }
        public uint Minute { get; }

        public DailyTask(TaskType scheduleType, uint hour, uint minute) : base(ScheduleInterval.Daily, scheduleType)
        {
            Hour = hour;
            Minute = minute;
        }

        public override long NextTimestamp()
        {
            var now = DateTimeOffset.UtcNow.ToOffset(Offset);
            var today = now.Date;
            var todayReset = new DateTimeOffset(today.Year, today.Month, today.Day, (int)Hour, (int)Minute, 0, Offset);
            if (todayReset > now)
            {
                return todayReset.ToUnixTimeSeconds();
            }
            var tomorrow = now.AddDays(1).Date;
            return new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, (int)Hour, (int)Minute, 0, Offset).ToUnixTimeSeconds();
        }

        public override string TaskTypeName()
        {
            return "Daily Task";
        }
    }
}

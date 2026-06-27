using Arrowgene.Ddon.GameServer.Utils;
using Arrowgene.Ddon.Shared.Model.Scheduler;
using System;

namespace Arrowgene.Ddon.GameServer.Tasks
{
    public abstract class WeeklyTask : SchedulerTask
    {
        public DayOfWeek Day { get; }
        public uint Hour { get; }
        public uint Minute { get; }

        public WeeklyTask(TaskType scheduleType, DayOfWeek day, uint hour, uint minute) : base(ScheduleInterval.Weekly, scheduleType)
        {
            Day = day;
            Hour = hour;
            Minute = minute;
        }

        public override long NextTimestamp()
        {
            var now = DateTimeOffset.UtcNow.ToOffset(Offset);
            var today = now.Date;
            if (today.DayOfWeek == Day)
            {
                var todayReset = new DateTimeOffset(today.Year, today.Month, today.Day, (int)Hour, (int)Minute, 0, Offset);
                if (todayReset > now)
                {
                    return todayReset.ToUnixTimeSeconds();
                }
            }
            var nextDate = DateUtils.GetNextWeekday(now.AddDays(1).Date, Day);
            return new DateTimeOffset(nextDate.Year, nextDate.Month, nextDate.Day, (int)Hour, (int)Minute, 0, Offset).ToUnixTimeSeconds();
        }

        public override string TaskTypeName()
        {
            return "Weekly Task";
        }
    }
}

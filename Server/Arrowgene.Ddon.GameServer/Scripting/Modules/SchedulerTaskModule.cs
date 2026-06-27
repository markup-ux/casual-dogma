using Arrowgene.Ddon.GameServer.Tasks;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Model.Scheduler;
using Arrowgene.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class SchedulerTaskModule : GameServerScriptModule
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(SchedulerTaskModule));

        public override string ModuleRoot => "scheduled_tasks";
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => false;
        public override bool EnableHotLoad => false;

        public List<SchedulerTask> Tasks { get; private set; } = new();

        public T GetTask<T>(TaskType type) where T : SchedulerTask
        {
            return Tasks.OfType<T>().FirstOrDefault(t => t.Type == type);
        }

        public override void Initialize()
        {
            Tasks = new List<SchedulerTask>();
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result is SchedulerTask task)
            {
                Logger.Info($"Registered scheduled task '{task.Type}' from '{path}'");
                Tasks.Add(task);
                return true;
            }

            Logger.Error($"Script '{path}' did not return a SchedulerTask instance.");
            return false;
        }
    }
}

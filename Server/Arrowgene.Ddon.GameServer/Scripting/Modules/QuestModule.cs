using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using System.Collections.Generic;
using System.IO;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class QuestModule : GameServerScriptModule
    {
        public override string ModuleRoot => "quests";
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        public QuestModule()
        {
            IgnoredScripts.Add("EmblemTrial.csx");
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            if (IgnoredScripts.Contains(Path.GetFileName(path)))
            {
                return true;
            }

            IQuest quest = (IQuest)result;
            if (quest == null)
            {
                return false;
            }

            // Initialize any state required
            quest.Initialize(path);

            // TODO: Load quest through a different Mechanism
            LibDdon.LoadQuest(quest);

            return true;
        }
    }
}

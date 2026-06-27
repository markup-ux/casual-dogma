using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.Shared.Entity.Structure;
using System.Collections.Generic;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class JobOrbTreeSpecialConditionModule : GameServerScriptModule
    {
        public override string ModuleRoot => "job_orb_tree/special_conditions";
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        private Dictionary<uint, IJobOrbSpecialCondition> SpecialConditions { get; set; } = new();

        public List<CDataJobOrbDevoteElementSpecialCondition> GetSpecialConditions(GameClient client)
        {
            return SpecialConditions.Select(x => x.Value.ToCDataJobOrbDevoteElementSpecialCondition(client)).ToList();
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            var specialCondition = (IJobOrbSpecialCondition)result;
            if (specialCondition != null)
            {
                SpecialConditions[specialCondition.ConditionId] = specialCondition;
            }

            return true;
        }
    }
}

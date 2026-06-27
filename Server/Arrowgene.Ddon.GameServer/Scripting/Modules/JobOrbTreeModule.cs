using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.Shared.Model;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class JobOrbTreeModule : GameServerScriptModule
    {
        private string _ModuleRoot;
        public override string ModuleRoot
        {
            get { return _ModuleRoot; }
        }

        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        public Dictionary<JobId, List<JobOrbUpgrade>> SkillAugmentations { get; private set; }

        public JobOrbTreeModule(string moduleRoot)
        {
            _ModuleRoot = moduleRoot;
            SkillAugmentations = new Dictionary<JobId, List<JobOrbUpgrade>>();
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            var skillAugmentationInformation = (ISkillAugmentation) result;
            if (skillAugmentationInformation != null)
            {
                SkillAugmentations[skillAugmentationInformation.JobId] = skillAugmentationInformation.Upgrades;
            }

            return true;
        }
    }
}

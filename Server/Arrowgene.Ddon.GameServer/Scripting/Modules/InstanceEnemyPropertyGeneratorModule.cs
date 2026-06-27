using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class InstanceEnemyPropertyGeneratorModule : GameServerScriptModule
    {
        public override string ModuleRoot => Path.Combine("enemies", "instance_properties");
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        private Dictionary<string, IInstanceEnemyPropertyGenerator> InstanceEnemyProperties { get; }

        public InstanceEnemyPropertyGeneratorModule()
        {
            InstanceEnemyProperties = new Dictionary<string, IInstanceEnemyPropertyGenerator>();
        }

        public List<IInstanceEnemyPropertyGenerator> GetGenerators()
        {
            return InstanceEnemyProperties.Values.OrderBy(x => x.ScriptRank).ToList();
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            var generator = (IInstanceEnemyPropertyGenerator)result;
            if (generator == null)
            {
                return false;
            }

            string scriptName = Path.GetFileNameWithoutExtension(path);
            InstanceEnemyProperties[scriptName] = generator;

            return true;
        }
    }
}


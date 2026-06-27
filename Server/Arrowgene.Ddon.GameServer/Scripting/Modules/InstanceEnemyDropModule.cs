using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using Arrowgene.Ddon.Shared.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Scripting.Modules
{
    public class InstanceEnemyDropModule : GameServerScriptModule
    {
        public override string ModuleRoot => Path.Combine("enemies", "drop_generators");
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        public InstanceEnemyDropModule()
        {
        }

        private Dictionary<string, IInstanceEnemyDropGenerator> EnemyDropGenerators = new();

        public List<IInstanceEnemyDropGenerator> GetGenerators(GameMode gameMode)
        {
            return EnemyDropGenerators.Values.Where(x => x.GameMode == gameMode).ToList();
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            var generator = (IInstanceEnemyDropGenerator)result;
            if (generator == null)
            {
                return false;
            }

            string scriptName = Path.GetFileNameWithoutExtension(path);
            EnemyDropGenerators[scriptName] = generator;

            return true;
        }
    }
}

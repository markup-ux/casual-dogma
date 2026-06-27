using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class AddendumModule : GameServerScriptModule
    {
        public override string ModuleRoot => "addendums";
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        public AddendumModule()
        {
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            IAddendum addendum = (IAddendum)result;
            addendum.Amend();

            return true;
        }
    }
}

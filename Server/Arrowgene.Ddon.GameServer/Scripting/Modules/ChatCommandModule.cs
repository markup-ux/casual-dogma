using Arrowgene.Ddon.GameServer.Scripting.Interfaces;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Scripting
{
    public class ChatCommandModule : GameServerScriptModule
    {
        public override string ModuleRoot => "chat_commands";
        public override string Filter => "*.csx";
        public override bool ScanSubdirectories => true;
        public override bool EnableHotLoad => true;

        public Dictionary<string, IChatCommand> Commands { get; private set; } = new Dictionary<string, IChatCommand>();

        public ChatCommandModule()
        {
        }

        public override bool EvaluateResult(string path, object result, IDictionary<string, object> variables)
        {
            if (result == null)
            {
                return false;
            }

            IChatCommand command = (IChatCommand)result;
            if (command != null)
            {
                Commands[command.CommandName.ToLowerInvariant()] = command; 
            }

            return true;
        }
    }
}

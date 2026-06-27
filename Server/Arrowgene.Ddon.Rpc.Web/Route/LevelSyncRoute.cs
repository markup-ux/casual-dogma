using Arrowgene.Ddon.GameServer;
using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Rpc.Command;
using Arrowgene.WebServer;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.Rpc.Web.Route
{
    /// <summary>
    /// Public read-only endpoint the client-side level-sync applier polls to learn the current attack
    /// scale factors for a character (network alternative to the local signal file, for remote players).
    ///
    ///   GET /rpc/levelsync?name=First%20Last
    ///   -> { "Synced": bool, "PhysFactor": d, "MagFactor": d, "RecLevel": u, "TrueLevel": u, "Job": s }
    ///
    /// Only a scale factor is exposed (no sensitive data), so it is unauthenticated like /rpc/status.
    /// </summary>
    public class LevelSyncCommand : RpcQueryCommand
    {
        public LevelSyncCommand(WebCollection<string, string> queryParams) : base(queryParams)
        {
        }

        public override string Name => "LevelSyncCommand";

        public override RpcCommandResult Execute(DdonGameServer gameServer)
        {
            string name = _queryParams.Get("name") ?? string.Empty;
            LevelSyncManager.SyncSignalView view = gameServer.LevelSyncManager.GetSignalView(name);
            RecoverableHpManager.RecoverableHpSignalView hpView = gameServer.RecoverableHpManager.GetSignalView(name);
            view.PinRecoverableHp = hpView.PinRecoverableHp;
            view.RecoverableHpJobLevel = hpView.JobLevel;
            view.RecoverableHpJobLevelMax = hpView.JobLevelMax;
            ReturnValue = view;
            return new RpcCommandResult(this, true);
        }
    }

    public class LevelSyncRoute : RpcRouteTemplate
    {
        public override string Route => "/rpc/levelsync";

        public LevelSyncRoute(IRpcExecuter executer) : base(executer)
        {
        }

        public override async Task<WebResponse> Get(WebRequest request)
        {
            return await HandleQuery<LevelSyncCommand>(request);
        }
    }
}

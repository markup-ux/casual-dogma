using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class InstanceGetDropItemSetListHandler : GameRequestPacketHandler<C2SInstanceGetDropItemSetListReq, S2CInstanceGetDropItemSetListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(InstanceGetDropItemSetListHandler));

        public InstanceGetDropItemSetListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CInstanceGetDropItemSetListRes Handle(GameClient client, C2SInstanceGetDropItemSetListReq request)
        {
            StageLayoutId layout = request.LayoutId.AsStageLayoutId();
            Server.SupplyCacheManager.AdoptClientLayout(client, layout);

            return Server.SupplyCacheManager.BuildClientDropItemSetListResponse(client, layout);
        }
    }
}

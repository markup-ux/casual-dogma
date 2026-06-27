using Arrowgene.Ddon.GameServer.Context;
using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Server.Network;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;
using Arrowgene.Logging;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class InstanceEnemyGroupEntryHandler : GameStructurePacketHandler<C2SInstanceEnemyGroupEntryNtc>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(InstanceEnemyGroupEntryHandler));

        public InstanceEnemyGroupEntryHandler(DdonGameServer server) : base(server)
        {
        }

        public override void Handle(GameClient client, StructurePacket<C2SInstanceEnemyGroupEntryNtc> packet)
        {
            CDataStageLayoutId layout = packet.Structure.LayoutId;
            StageLayoutId stageLayoutId = layout.AsStageLayoutId();

            client.Character.Stage = stageLayoutId;
            if (stageLayoutId.Id != 0 && client.TryAdoptInstanceLayout(stageLayoutId))
            {
                Server.SupplyCacheManager.SyncCachesForLayout(client, stageLayoutId);
            }

            ContextManager.HandleEntry(client, layout);
        }
    }
}

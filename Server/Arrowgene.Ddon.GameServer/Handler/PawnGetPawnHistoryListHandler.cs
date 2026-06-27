using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class PawnGetPawnHistoryListHandler : GameRequestPacketHandler<C2SPawnGetPawnHistoryListReq, S2CPawnGetPawnHistoryListRes>
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(PawnGetPawnHistoryListHandler));

        public PawnGetPawnHistoryListHandler(DdonGameServer server) : base(server)
        {
        }

        public override S2CPawnGetPawnHistoryListRes Handle(GameClient client, C2SPawnGetPawnHistoryListReq request)
        {
            return new()
            {
                PawnHistoryList = Server.Database.SelectPawnHistory(request.PawnId)
            };
        }
    }
}

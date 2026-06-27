using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class ProfileSetPawnProfileCommentHandler(DdonGameServer server) : GameRequestPacketHandler<C2SProfileSetPawnProfileCommentReq, S2CProfileSetPawnProfileCommentRes>(server)
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ProfileSetPawnProfileCommentHandler));

        public override S2CProfileSetPawnProfileCommentRes Handle(GameClient client, C2SProfileSetPawnProfileCommentReq request)
        {
            var pawn = client.Character.Pawns.Where(x => x.PawnId == request.PawnId).FirstOrDefault()
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PAWN_NOT_FOUNDED);
            pawn.CharacterProfile.Comment = request.Comment;
            Server.Database.UpdateCharacterProfile(pawn);
            return new();
        }
    }
}

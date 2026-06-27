using Arrowgene.Ddon.Server;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Logging;
using System.Linq;

namespace Arrowgene.Ddon.GameServer.Handler
{
    public class ProfileSetPawnProfileHandler(DdonGameServer server) : GameRequestPacketHandler<C2SProfileSetPawnProfileReq, S2CProfileSetPawnProfileRes>(server)
    {
        private static readonly ServerLogger Logger = LogProvider.Logger<ServerLogger>(typeof(ProfileSetPawnProfileHandler));

        public override S2CProfileSetPawnProfileRes Handle(GameClient client, C2SProfileSetPawnProfileReq request)
        {
            var pawn = client.Character.Pawns.Where(x => x.PawnId == request.PawnId).FirstOrDefault()
                ?? throw new ResponseErrorException(ErrorCode.ERROR_CODE_PAWN_NOT_FOUNDED);
            pawn.CharacterProfile.CDataArisenProfile = request.ArisenProfile;
            pawn.CharacterProfile.Comment = request.Comment;
            Server.Database.UpdateCharacterProfile(pawn);
            return new();
        }
    }
}

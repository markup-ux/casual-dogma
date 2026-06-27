using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CProfileSetPawnProfileCommentRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_PROFILE_SET_PAWN_PROFILE_COMMENT_RES;

        public class Serializer : PacketEntitySerializer<S2CProfileSetPawnProfileCommentRes>
        {
            public override void Write(IBuffer buffer, S2CProfileSetPawnProfileCommentRes obj)
            {
                WriteServerResponse(buffer, obj);
            }

            public override S2CProfileSetPawnProfileCommentRes Read(IBuffer buffer)
            {
                S2CProfileSetPawnProfileCommentRes obj = new S2CProfileSetPawnProfileCommentRes();
                ReadServerResponse(buffer, obj);
                return obj;
            }
        }
    }
}

using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CProfileSetPawnProfileRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_PROFILE_SET_PAWN_PROFILE_RES;

        public class Serializer : PacketEntitySerializer<S2CProfileSetPawnProfileRes>
        {
            public override void Write(IBuffer buffer, S2CProfileSetPawnProfileRes obj)
            {
                WriteServerResponse(buffer, obj);
            }

            public override S2CProfileSetPawnProfileRes Read(IBuffer buffer)
            {
                S2CProfileSetPawnProfileRes obj = new S2CProfileSetPawnProfileRes();
                ReadServerResponse(buffer, obj);
                return obj;
            }
        }
    }
}

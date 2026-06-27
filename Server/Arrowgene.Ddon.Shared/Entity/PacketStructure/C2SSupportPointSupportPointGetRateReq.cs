using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SSupportPointSupportPointGetRateReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_SUPPORT_POINT_SUPPORT_POINT_GET_RATE_REQ;

        public class Serializer : PacketEntitySerializer<C2SSupportPointSupportPointGetRateReq>
        {
            public override void Write(IBuffer buffer, C2SSupportPointSupportPointGetRateReq obj)
            {
            }

            public override C2SSupportPointSupportPointGetRateReq Read(IBuffer buffer)
            {
                C2SSupportPointSupportPointGetRateReq obj = new C2SSupportPointSupportPointGetRateReq();
                return obj;
            }
        }
    }
}

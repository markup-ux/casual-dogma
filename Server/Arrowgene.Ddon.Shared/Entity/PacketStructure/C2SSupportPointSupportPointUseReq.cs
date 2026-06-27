using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SSupportPointSupportPointUseReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_SUPPORT_POINT_SUPPORT_POINT_USE_REQ;

        public List<CDataUseSupportPoint> UsePointList { get; set; } = [];

        public class Serializer : PacketEntitySerializer<C2SSupportPointSupportPointUseReq>
        {
            public override void Write(IBuffer buffer, C2SSupportPointSupportPointUseReq obj)
            {
                WriteEntityList(buffer, obj.UsePointList);
            }

            public override C2SSupportPointSupportPointUseReq Read(IBuffer buffer)
            {
                C2SSupportPointSupportPointUseReq obj = new C2SSupportPointSupportPointUseReq();
                obj.UsePointList = ReadEntityList<CDataUseSupportPoint>(buffer);
                return obj;
            }
        }
    }
}

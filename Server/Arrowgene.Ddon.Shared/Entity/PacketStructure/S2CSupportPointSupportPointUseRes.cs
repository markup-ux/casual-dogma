using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CSupportPointSupportPointUseRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_SUPPORT_POINT_SUPPORT_POINT_USE_RES;

        public List<CDataUseSupportPointRes> UsePoint { get; set; } = [];

        public class Serializer : PacketEntitySerializer<S2CSupportPointSupportPointUseRes>
        {
            public override void Write(IBuffer buffer, S2CSupportPointSupportPointUseRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteEntityList(buffer, obj.UsePoint);
            }

            public override S2CSupportPointSupportPointUseRes Read(IBuffer buffer)
            {
                S2CSupportPointSupportPointUseRes obj = new S2CSupportPointSupportPointUseRes();
                ReadServerResponse(buffer, obj);
                obj.UsePoint = ReadEntityList<CDataUseSupportPointRes>(buffer);
                return obj;
            }
        }
    }
}

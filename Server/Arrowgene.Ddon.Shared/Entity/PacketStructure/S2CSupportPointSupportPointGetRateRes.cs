using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CSupportPointSupportPointGetRateRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_SUPPORT_POINT_SUPPORT_POINT_GET_RATE_RES;

        public List<CDataSupportRate> SupportRate { get; set; } = [];

        public class Serializer : PacketEntitySerializer<S2CSupportPointSupportPointGetRateRes>
        {
            public override void Write(IBuffer buffer, S2CSupportPointSupportPointGetRateRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteEntityList(buffer, obj.SupportRate);
            }

            public override S2CSupportPointSupportPointGetRateRes Read(IBuffer buffer)
            {
                S2CSupportPointSupportPointGetRateRes obj = new S2CSupportPointSupportPointGetRateRes();
                ReadServerResponse(buffer, obj);
                obj.SupportRate = ReadEntityList<CDataSupportRate>(buffer);
                return obj;
            }
        }
    }
}

using System.Collections.Generic;
using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CInstanceGetDropItemSetListRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_INSTANCE_GET_DROP_ITEM_SET_LIST_RES;

        public S2CInstanceGetDropItemSetListRes()
        {
            LayoutId = new CDataStageLayoutId();
            DropItemSetList = new List<CDataDropItemSetInfo>();
        }

        public CDataStageLayoutId LayoutId { get; set; }
        public List<CDataDropItemSetInfo> DropItemSetList { get; set; }

        public class Serializer : PacketEntitySerializer<S2CInstanceGetDropItemSetListRes>
        {
            public override void Write(IBuffer buffer, S2CInstanceGetDropItemSetListRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteEntity(buffer, obj.LayoutId);
                WriteEntityList(buffer, obj.DropItemSetList);
            }

            public override S2CInstanceGetDropItemSetListRes Read(IBuffer buffer)
            {
                S2CInstanceGetDropItemSetListRes obj = new S2CInstanceGetDropItemSetListRes();
                ReadServerResponse(buffer, obj);
                obj.LayoutId = ReadEntity<CDataStageLayoutId>(buffer);
                obj.DropItemSetList = ReadEntityList<CDataDropItemSetInfo>(buffer);
                return obj;
            }
        }
    }
}

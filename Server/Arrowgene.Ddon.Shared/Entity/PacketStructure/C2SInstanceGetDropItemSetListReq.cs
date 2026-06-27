using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SInstanceGetDropItemSetListReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_INSTANCE_GET_DROP_ITEM_SET_LIST_REQ;

        public C2SInstanceGetDropItemSetListReq()
        {
            LayoutId = new CDataStageLayoutId();
        }

        public CDataStageLayoutId LayoutId { get; set; }

        public class Serializer : PacketEntitySerializer<C2SInstanceGetDropItemSetListReq>
        {
            public override void Write(IBuffer buffer, C2SInstanceGetDropItemSetListReq obj)
            {
                WriteEntity(buffer, obj.LayoutId);
            }

            public override C2SInstanceGetDropItemSetListReq Read(IBuffer buffer)
            {
                C2SInstanceGetDropItemSetListReq obj = new C2SInstanceGetDropItemSetListReq();
                obj.LayoutId = ReadEntity<CDataStageLayoutId>(buffer);
                return obj;
            }
        }
    }
}

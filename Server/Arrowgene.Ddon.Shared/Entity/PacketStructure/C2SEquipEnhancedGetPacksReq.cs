using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SEquipEnhancedGetPacksReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_EQUIP_ENHANCED_GET_PACKS_REQ;

        public EquipEnhanceType EnhanceType { get; set; }

        public class Serializer : PacketEntitySerializer<C2SEquipEnhancedGetPacksReq>
        {
            public override void Write(IBuffer buffer, C2SEquipEnhancedGetPacksReq obj)
            {
                WriteByte(buffer, (byte)obj.EnhanceType);
            }

            public override C2SEquipEnhancedGetPacksReq Read(IBuffer buffer)
            {
                C2SEquipEnhancedGetPacksReq obj = new C2SEquipEnhancedGetPacksReq();
                obj.EnhanceType = (EquipEnhanceType)ReadByte(buffer);
                return obj;
            }
        }

    }
}

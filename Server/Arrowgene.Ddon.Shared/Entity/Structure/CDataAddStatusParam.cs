using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataAddStatusParam
    {
        public CDataAddStatusParam()
        {
        }

        public CDataAddStatusParam(CDataAddStatusParam obj)
        {
            EnhanceType = obj.EnhanceType;
            Unk0 = obj.Unk0;
            EnhanceId = obj.EnhanceId;
            Unk1 = obj.Unk1;
        }

        // Not sure what two of these fields are, but this was not structured correctly.
        public EquipEnhanceType EnhanceType { get; set; }
        public byte Unk0 { get; set; }
        public ushort EnhanceId { get; set; }
        public ushort Unk1 { get; set; }

        public class Serializer : EntitySerializer<CDataAddStatusParam>
        {
            public override void Write(IBuffer buffer, CDataAddStatusParam obj)
            {
                WriteByte(buffer, (byte)obj.EnhanceType);
                WriteByte(buffer, obj.Unk0);
                WriteUInt16(buffer, obj.EnhanceId);
                WriteUInt16(buffer, obj.Unk1);
            }

            public override CDataAddStatusParam Read(IBuffer buffer)
            {
                CDataAddStatusParam obj = new CDataAddStatusParam();
                obj.EnhanceType = (EquipEnhanceType)ReadByte(buffer);
                obj.Unk0 = ReadByte(buffer);
                obj.EnhanceId = ReadUInt16(buffer);
                obj.Unk1 = ReadUInt16(buffer);
                return obj;
            }
        }
    }
}

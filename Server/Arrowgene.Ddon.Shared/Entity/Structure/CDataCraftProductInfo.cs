using Arrowgene.Buffers;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataCraftProductInfo
    {
        public uint ItemID { get; set; }

        public uint ItemNum { get; set; }

        public ushort AddStatusParam { get; set; }
        public byte PlusValue { get; set; }
        public uint Exp { get; set; }
        public uint ExtraBonus { get; set; }
        public bool IsGreatSuccess { get; set; }

        public class Serializer : EntitySerializer<CDataCraftProductInfo>
        {
            public override void Write(IBuffer buffer, CDataCraftProductInfo obj)
            {
                WriteUInt32(buffer, obj.ItemID);
                WriteUInt32(buffer, obj.ItemNum);
                WriteUInt16(buffer, obj.AddStatusParam);
                WriteByte(buffer, obj.PlusValue);
                WriteUInt32(buffer, obj.Exp);
                WriteUInt32(buffer, obj.ExtraBonus);
                WriteBool(buffer, obj.IsGreatSuccess);
            }

            public override CDataCraftProductInfo Read(IBuffer buffer)
            {
                CDataCraftProductInfo obj = new CDataCraftProductInfo();
                obj.ItemID = ReadUInt32(buffer);
                obj.ItemNum = ReadUInt32(buffer);
                obj.AddStatusParam = ReadUInt16(buffer);
                obj.PlusValue = ReadByte(buffer);
                obj.Exp = ReadUInt32(buffer);
                obj.ExtraBonus = ReadUInt32(buffer);
                obj.IsGreatSuccess = ReadBool(buffer);
                return obj;
            }
        }
    }
}

using Arrowgene.Buffers;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataSupportRate
    {
        public byte StatusType { get; set; }
        public uint Rate { get; set; }

        public class Serializer : EntitySerializer<CDataSupportRate>
        {
            public override void Write(IBuffer buffer, CDataSupportRate obj)
            {
                WriteByte(buffer, obj.StatusType);
                WriteUInt32(buffer, obj.Rate);
            }

            public override CDataSupportRate Read(IBuffer buffer)
            {
                CDataSupportRate obj = new CDataSupportRate();
                obj.StatusType = ReadByte(buffer);
                obj.Rate = ReadUInt32(buffer);
                return obj;
            }
        }
    }
}

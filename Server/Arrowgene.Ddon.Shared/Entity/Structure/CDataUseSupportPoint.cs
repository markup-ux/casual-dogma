using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataUseSupportPoint
    {
        public byte StatusType { get; set; }
        public byte CharType { get; set; } // 2 for Pawns?
        public JobId JobType { get; set; }
        public uint UsePoint { get; set; }
        public uint PawnId { get; set; }

        public class Serializer : EntitySerializer<CDataUseSupportPoint>
        {
            public override void Write(IBuffer buffer, CDataUseSupportPoint obj)
            {
                WriteByte(buffer, obj.StatusType);
                WriteByte(buffer, obj.CharType);
                WriteByte(buffer, (byte)obj.JobType);
                WriteUInt32(buffer, obj.UsePoint);
                WriteUInt32(buffer, obj.PawnId);
            }

            public override CDataUseSupportPoint Read(IBuffer buffer)
            {
                CDataUseSupportPoint obj = new CDataUseSupportPoint();
                obj.StatusType = ReadByte(buffer);
                obj.CharType = ReadByte(buffer);
                obj.JobType = (JobId)ReadByte(buffer);
                obj.UsePoint = ReadUInt32(buffer);
                obj.PawnId = ReadUInt32(buffer);
                return obj;
            }
        }
    }
}

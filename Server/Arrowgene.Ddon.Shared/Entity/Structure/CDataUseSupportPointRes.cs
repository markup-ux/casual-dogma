using Arrowgene.Buffers;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataUseSupportPointRes
    {
        public CDataUseSupportPoint UseSupportPoint { get; set; } = new();
        public uint AdjustPoint { get; set; }
        public uint TotalPoint { get; set; }

        public class Serializer : EntitySerializer<CDataUseSupportPointRes>
        {
            public override void Write(IBuffer buffer, CDataUseSupportPointRes obj)
            {
                WriteEntity(buffer, obj.UseSupportPoint);
                WriteUInt32(buffer, obj.AdjustPoint);
                WriteUInt32(buffer, obj.TotalPoint);
            }

            public override CDataUseSupportPointRes Read(IBuffer buffer)
            {
                CDataUseSupportPointRes obj = new CDataUseSupportPointRes();
                obj.UseSupportPoint = ReadEntity<CDataUseSupportPoint>(buffer);
                obj.AdjustPoint = ReadUInt32(buffer);
                obj.TotalPoint = ReadUInt32(buffer);
                return obj;
            }
        }
    }
}

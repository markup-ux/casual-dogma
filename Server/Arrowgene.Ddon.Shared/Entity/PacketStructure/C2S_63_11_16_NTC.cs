using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2S_63_11_16_NTC : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_63_11_16_NTC;

        public C2S_63_11_16_NTC()
        {
        }

        public uint StageNo {  get; set; }

        public class Serializer : PacketEntitySerializer<C2S_63_11_16_NTC>
        {
            public override void Write(IBuffer buffer, C2S_63_11_16_NTC obj)
            {
                WriteUInt32(buffer, obj.StageNo);
            }

            public override C2S_63_11_16_NTC Read(IBuffer buffer)
            {
                C2S_63_11_16_NTC obj = new C2S_63_11_16_NTC();
                obj.StageNo = ReadUInt32(buffer);
                return obj;
            }
        }
    }
}

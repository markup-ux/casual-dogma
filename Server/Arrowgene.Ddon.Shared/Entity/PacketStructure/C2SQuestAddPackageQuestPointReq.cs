using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SQuestAddPackageQuestPointReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_QUEST_ADD_PACKAGE_QUEST_POINT_REQ;

        public uint Unk0 { get; set; }
        public int Unk1 { get; set; }

        public class Serializer : PacketEntitySerializer<C2SQuestAddPackageQuestPointReq>
        {
            public override void Write(IBuffer buffer, C2SQuestAddPackageQuestPointReq obj)
            {
                WriteUInt32(buffer, obj.Unk0);
                WriteInt32(buffer, obj.Unk1);
            }

            public override C2SQuestAddPackageQuestPointReq Read(IBuffer buffer)
            {
                C2SQuestAddPackageQuestPointReq obj = new C2SQuestAddPackageQuestPointReq();
                obj.Unk0 = ReadUInt32(buffer);
                obj.Unk1 = ReadInt32(buffer);
                return obj;
            }
        }
    }
}

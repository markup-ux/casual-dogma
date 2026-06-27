using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SQuestGetPackageQuestInfoReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_QUEST_GET_PACKAGE_QUEST_INFO_REQ;

        public class Serializer : PacketEntitySerializer<C2SQuestGetPackageQuestInfoReq>
        {
            public override void Write(IBuffer buffer, C2SQuestGetPackageQuestInfoReq obj)
            {
            }

            public override C2SQuestGetPackageQuestInfoReq Read(IBuffer buffer)
            {
                C2SQuestGetPackageQuestInfoReq obj = new C2SQuestGetPackageQuestInfoReq();
                return obj;
            }
        }
    }
}

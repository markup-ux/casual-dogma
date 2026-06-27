using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class C2SQuestGetPackageQuestInfoDetailReq : IPacketStructure
    {
        public PacketId Id => PacketId.C2S_QUEST_GET_PACKAGE_QUEST_INFO_DETAIL_REQ;

        public QuestSubstoryGroupId SubstoryGroupId { get; set; }

        public class Serializer : PacketEntitySerializer<C2SQuestGetPackageQuestInfoDetailReq>
        {
            public override void Write(IBuffer buffer, C2SQuestGetPackageQuestInfoDetailReq obj)
            {
                WriteUInt32(buffer, (uint) obj.SubstoryGroupId);
            }

            public override C2SQuestGetPackageQuestInfoDetailReq Read(IBuffer buffer)
            {
                C2SQuestGetPackageQuestInfoDetailReq obj = new C2SQuestGetPackageQuestInfoDetailReq();
                obj.SubstoryGroupId = (QuestSubstoryGroupId) ReadUInt32(buffer);
                return obj;
            }
        }
    }
}

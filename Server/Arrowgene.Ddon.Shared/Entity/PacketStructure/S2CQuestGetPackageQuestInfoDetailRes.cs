using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model.Quest;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CQuestGetPackageQuestInfoDetailRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_QUEST_GET_PACKAGE_QUEST_INFO_DETAIL_RES;

        public S2CQuestGetPackageQuestInfoDetailRes()
        {
        }

        public QuestSubstoryGroupId SubstoryGroupId { get; set; }
        public bool IsComplete { get; set; }

        public List<CDataPackageQuestInfoDetail> PackageQuestInfoDetailList = new();

        public class Serializer : PacketEntitySerializer<S2CQuestGetPackageQuestInfoDetailRes>
        {
            public override void Write(IBuffer buffer, S2CQuestGetPackageQuestInfoDetailRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteUInt32(buffer, (uint) obj.SubstoryGroupId);
                WriteBool(buffer, obj.IsComplete);
                WriteEntityList(buffer, obj.PackageQuestInfoDetailList);
            }

            public override S2CQuestGetPackageQuestInfoDetailRes Read(IBuffer buffer)
            {
                S2CQuestGetPackageQuestInfoDetailRes obj = new S2CQuestGetPackageQuestInfoDetailRes();
                ReadServerResponse(buffer, obj);
                obj.SubstoryGroupId = (QuestSubstoryGroupId) ReadUInt32(buffer);
                obj.IsComplete = ReadBool(buffer);
                obj.PackageQuestInfoDetailList = ReadEntityList<CDataPackageQuestInfoDetail>(buffer);
                return obj;
            }
        }
    }
}


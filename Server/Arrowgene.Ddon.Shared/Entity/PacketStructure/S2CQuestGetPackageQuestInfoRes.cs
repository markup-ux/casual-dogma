using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Network;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CQuestGetPackageQuestInfoRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_QUEST_GET_PACKAGE_QUEST_INFO_RES;

        public S2CQuestGetPackageQuestInfoRes()
        {
        }

        public List<CDataQuestPackageQuestEntry> PackageQuestEntryList { get; set; } = new();

        public class Serializer : PacketEntitySerializer<S2CQuestGetPackageQuestInfoRes>
        {
            public override void Write(IBuffer buffer, S2CQuestGetPackageQuestInfoRes obj)
            {
                WriteServerResponse(buffer, obj);
                WriteEntityList(buffer, obj.PackageQuestEntryList);
            }

            public override S2CQuestGetPackageQuestInfoRes Read(IBuffer buffer)
            {
                S2CQuestGetPackageQuestInfoRes obj = new S2CQuestGetPackageQuestInfoRes();
                ReadServerResponse(buffer, obj);
                obj.PackageQuestEntryList = ReadEntityList<CDataQuestPackageQuestEntry>(buffer);
                return obj;
            }
        }
    }
}


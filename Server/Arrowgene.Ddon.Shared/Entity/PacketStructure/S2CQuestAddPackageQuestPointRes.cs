using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CQuestAddPackageQuestPointRes : ServerResponse
    {
        public override PacketId Id => PacketId.S2C_QUEST_ADD_PACKAGE_QUEST_POINT_RES;

        public S2CQuestAddPackageQuestPointRes()
        {
        }

        public class Serializer : PacketEntitySerializer<S2CQuestAddPackageQuestPointRes>
        {
            public override void Write(IBuffer buffer, S2CQuestAddPackageQuestPointRes obj)
            {
                WriteServerResponse(buffer, obj);
            }

            public override S2CQuestAddPackageQuestPointRes Read(IBuffer buffer)
            {
                S2CQuestAddPackageQuestPointRes obj = new S2CQuestAddPackageQuestPointRes();
                ReadServerResponse(buffer, obj);
                return obj;
            }
        }
    }
}


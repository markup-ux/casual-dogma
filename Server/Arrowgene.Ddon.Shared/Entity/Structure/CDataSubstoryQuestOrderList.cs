using System.Collections.Generic;
using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model;
using Arrowgene.Ddon.Shared.Model.Quest;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataSubstoryQuestOrderList
    {
        public CDataSubstoryQuestOrderList() {
            Details = new List<CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1>();
        }

        public QuestSubstoryGroupId SubstoryGroupId { get; set; }
        public List<CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1> Details { get; set; }

        public class Serializer : EntitySerializer<CDataSubstoryQuestOrderList>
        {
            public override void Write(IBuffer buffer, CDataSubstoryQuestOrderList obj)
            {
                WriteUInt32(buffer, (uint) obj.SubstoryGroupId);
                WriteEntityList<CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1>(buffer, obj.Details);
            }

            public override CDataSubstoryQuestOrderList Read(IBuffer buffer)
            {
                CDataSubstoryQuestOrderList obj = new CDataSubstoryQuestOrderList();
                obj.SubstoryGroupId = (QuestSubstoryGroupId) ReadUInt32(buffer);
                obj.Details = ReadEntityList<CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1>(buffer);
                return obj;
            }
        }
    }
}

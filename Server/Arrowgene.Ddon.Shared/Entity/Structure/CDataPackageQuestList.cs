using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model.Quest;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataPackageQuestList
    {
        public CDataPackageQuestList()
        {
        }

        public QuestSubstoryGroupId SubstoryGroupId { get; set; }
        public List<CDataPackageQuestDetail> Details { get; set; } = new();

        public class Serializer : EntitySerializer<CDataPackageQuestList>
        {
            public override void Write(IBuffer buffer, CDataPackageQuestList obj)
            {
                WriteUInt32(buffer, (uint) obj.SubstoryGroupId);
                WriteEntityList(buffer, obj.Details);
            }

            public override CDataPackageQuestList Read(IBuffer buffer)
            {
                CDataPackageQuestList obj = new CDataPackageQuestList();
                obj.SubstoryGroupId = (QuestSubstoryGroupId) ReadUInt32(buffer);
                obj.Details = ReadEntityList<CDataPackageQuestDetail>(buffer);
                return obj;
            }
        }
    }
}

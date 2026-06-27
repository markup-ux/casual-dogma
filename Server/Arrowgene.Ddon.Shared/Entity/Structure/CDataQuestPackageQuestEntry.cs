using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model.Quest;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataQuestPackageQuestEntry
    {
        public QuestSubstoryGroupId SubstoryGroupId { get; set; }
        public bool IsEnabled { get; set; } // Enable Cancel?

        public class Serializer : EntitySerializer<CDataQuestPackageQuestEntry>
        {
            public override void Write(IBuffer buffer, CDataQuestPackageQuestEntry obj)
            {
                WriteUInt32(buffer, (uint) obj.SubstoryGroupId);
                WriteBool(buffer, obj.IsEnabled);
            }

            public override CDataQuestPackageQuestEntry Read(IBuffer buffer)
            {
                CDataQuestPackageQuestEntry obj = new CDataQuestPackageQuestEntry();
                obj.SubstoryGroupId = (QuestSubstoryGroupId) ReadUInt32(buffer);
                obj.IsEnabled = ReadBool(buffer);
                return obj;
            }
        }
    }
}

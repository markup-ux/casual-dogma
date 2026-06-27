using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Model.Quest;
using System.Collections.Generic;

namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataPackageQuestInfoDetailList
    {
        public CDataPackageQuestInfoDetailList()
        {
        }

        public uint Unk0 { get; set; } // Suspect something like Key
        public QuestId QuestId { get; set; }
        public uint Level { get; set; }
        public List<CDataQuestOrderConditionParam> QuestOrderConditionParamList = new(); // Accept Requirements
        public bool ShowEntry { get; set; } // Shows as ????
        public bool DisplayQuestName { get; set; } // shows ! and the quest name
        public bool IsComplete { get; set; } // Puts a stamp on it when complete

        public class Serializer : EntitySerializer<CDataPackageQuestInfoDetailList>
        {
            public override void Write(IBuffer buffer, CDataPackageQuestInfoDetailList obj)
            {
                WriteUInt32(buffer, obj.Unk0);
                WriteUInt32(buffer, (uint) obj.QuestId);
                WriteUInt32(buffer, obj.Level);
                WriteEntityList(buffer, obj.QuestOrderConditionParamList);
                WriteBool(buffer, obj.ShowEntry);
                WriteBool(buffer, obj.DisplayQuestName);
                WriteBool(buffer, obj.IsComplete);
            }

            public override CDataPackageQuestInfoDetailList Read(IBuffer buffer)
            {
                CDataPackageQuestInfoDetailList obj = new CDataPackageQuestInfoDetailList();
                obj.Unk0 = ReadUInt32(buffer);
                obj.QuestId = (QuestId) ReadUInt32(buffer);
                obj.Level = ReadUInt32(buffer);
                obj.QuestOrderConditionParamList = ReadEntityList<CDataQuestOrderConditionParam>(buffer);
                obj.ShowEntry = ReadBool(buffer);
                obj.DisplayQuestName = ReadBool(buffer);
                obj.IsComplete = ReadBool(buffer);
                return obj;
            }
        }
    }
}


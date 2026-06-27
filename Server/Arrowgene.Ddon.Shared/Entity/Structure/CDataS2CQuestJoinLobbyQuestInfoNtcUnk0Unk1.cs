using System.Collections.Generic;
using Arrowgene.Buffers;
        
namespace Arrowgene.Ddon.Shared.Entity.Structure
{
    public class CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1
    {
        public CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1() {
            Unk3 = new List<CDataCommonU32>();
            QuestList = new List<CDataQuestOrderList>();
        }
    
        public uint SequenceNo { get; set; }
        public int Unk1 { get; set; }
        public int Unk2 { get; set; }
        public List<CDataCommonU32> Unk3 { get; set; }
        public bool Unk4 { get; set; }
        public List<CDataQuestOrderList> QuestList { get; set; }
    
        public class Serializer : EntitySerializer<CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1>
        {
            public override void Write(IBuffer buffer, CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1 obj)
            {
                WriteUInt32(buffer, obj.SequenceNo);
                WriteInt32(buffer, obj.Unk1);
                WriteInt32(buffer, obj.Unk2);
                WriteEntityList(buffer, obj.Unk3);
                WriteBool(buffer, obj.Unk4);
                WriteEntityList(buffer, obj.QuestList);
            }
        
            public override CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1 Read(IBuffer buffer)
            {
                CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1 obj = new CDataS2CQuestJoinLobbyQuestInfoNtcUnk0Unk1();
                obj.SequenceNo = ReadUInt32(buffer);
                obj.Unk1 = ReadInt32(buffer);
                obj.Unk2 = ReadInt32(buffer);
                obj.Unk3 = ReadEntityList<CDataCommonU32>(buffer);
                obj.Unk4 = ReadBool(buffer);
                obj.QuestList = ReadEntityList<CDataQuestOrderList>(buffer);
                return obj;
            }
        }
    }
}
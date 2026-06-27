using Arrowgene.Buffers;
using Arrowgene.Ddon.Shared.Network;

namespace Arrowgene.Ddon.Shared.Entity.PacketStructure
{
    public class S2CQuestTimerNtc : IPacketStructure
    {
        public S2CQuestTimerNtc()
        {
        }

        public PacketId Id => PacketId.S2C_QUEST_TIMER_NTC;
        public uint QuestScheduleId { get; set; }
        public byte TimerNo { get; set; }

        public class Serializer : PacketEntitySerializer<S2CQuestTimerNtc>
        {

            public override void Write(IBuffer buffer, S2CQuestTimerNtc obj)
            {
                WriteUInt32(buffer, obj.QuestScheduleId);
                WriteByte(buffer, obj.TimerNo);
            }

            public override S2CQuestTimerNtc Read(IBuffer buffer)
            {
                S2CQuestTimerNtc obj = new S2CQuestTimerNtc();
                obj.QuestScheduleId = ReadUInt32(buffer);
                obj.TimerNo = ReadByte(buffer);
                return obj;
            }
        }
    }
}

using Arrowgene.Ddon.Shared.Entity.PacketStructure;

namespace Arrowgene.Ddon.Shared.Model.Quest
{
    public abstract class QuestProgressWork
    {
        public readonly uint QuestScheduleId;
        public readonly QuestProgressWorkType WorkType;

        public QuestProgressWork(uint questScheduleId, QuestProgressWorkType workType)
        {
            WorkType = workType;
            QuestScheduleId = questScheduleId;
        }

        public abstract S2CQuestQuestProgressWorkSaveNtc GetWork();
    }
}

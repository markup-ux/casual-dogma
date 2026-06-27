using Arrowgene.Ddon.GameServer.Characters;
using Arrowgene.Ddon.Shared.Entity.PacketStructure;
using Arrowgene.Ddon.Shared.Entity.Structure;
using Arrowgene.Ddon.Shared.Model.Quest;
using System;
using System.Collections.Generic;

namespace Arrowgene.Ddon.GameServer.Quests.Work
{
    public class WorldQuestClearedProgressWork : QuestProgressWork
    {
        public readonly QuestAreaId AreaId;
        public readonly uint Amount;

        // The block is stored by reference so that BlockNo is read at NTC-generation time,
        // after AddBlock() has assigned the final block number.
        private readonly QuestBlock _block;

        public WorldQuestClearedProgressWork(QuestBlock block, QuestAreaId areaId, uint amount)
            : base(block.QuestScheduleId, QuestProgressWorkType.WorldQuestCleared)
        {
            _block = block;
            AreaId = areaId;
            Amount = amount;
        }

        public bool QuestIsMatch(Quest quest)
        {
            return quest.QuestAreaId == AreaId;
        }

        public override S2CQuestQuestProgressWorkSaveNtc GetWork()
        {
            return new S2CQuestQuestProgressWorkSaveNtc()
            {
                QuestScheduleId = QuestScheduleId,
                ProcessNo = _block.ProcessNo,
                SequenceNo = _block.SequenceNo,
                BlockNo = _block.BlockNo,
                WorkList = new List<CDataQuestProgressWork>
                {
                    QuestManager.NotifyCommand.WorldQuestClearNum(AreaId, Amount)
                }
            };
        }
    }
}

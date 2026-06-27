using Arrowgene.Ddon.Shared.Entity.Structure;

namespace Arrowgene.Ddon.Shared.Model
{
    public class Ability
    {
        public JobId Job { get => AbilityId.JobId(); }
        public AbilityId AbilityId { get; set; }
        public byte AbilityLv { get; set; }

        public CDataSetAcquirementParam AsCDataSetAcquirementParam(byte slotNo)
        {
            return new CDataSetAcquirementParam()
            {
                Job = Job,
                SlotNo = slotNo,
                AcquirementNo = (uint)AbilityId,
                AcquirementLv = AbilityLv
            };
        }

        public CDataContextAcquirementData AsCDataContextAcquirementData(byte slotNo)
        {
            return new CDataContextAcquirementData()
            {
                SlotNo = slotNo,
                AcquirementNo = (uint)AbilityId,
                AcquirementLv = AbilityLv
            };
        }

        public CDataLearnedSetAcquirementParam AsCDataLearnedSetAcquirementParam()
        {
            return new CDataLearnedSetAcquirementParam()
            {
                Job = Job,
                Type = ReleaseType.Augment,
                AcquirementNo = (uint)AbilityId,
                AcquirementLv = AbilityLv,
                AcquirementParamId = 0
            };
        }
    }
}
